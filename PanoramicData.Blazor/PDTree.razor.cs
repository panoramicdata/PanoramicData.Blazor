using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.JSInterop;
using PanoramicData.Blazor.Exceptions;
using PanoramicData.Blazor.Services;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace PanoramicData.Blazor
{
	public partial class PDTree<TItem> where TItem : class
	{
		private const string IdPrefix = "pd-tree-";
		private static int _idSequence;

		[Inject] public IJSRuntime? JSRuntime { get; set; }

		[Inject] protected IBlockOverlayService BlockOverlayService { get; set; } = null!;

		/// <summary>
		/// Provides access to the parent DragContext if it exists.
		/// </summary>
		[CascadingParameter] public PDDragContext? DragContext { get; set; }

		/// <summary>
		/// Gets the unique identifier of this tree.
		/// </summary>
		public string Id { get; private set; } = string.Empty;

		/// <summary>
		/// Gets or sets the root TreeNode instance.
		/// </summary>
		public TreeNode<TItem> RootNode { get; set; } = new TreeNode<TItem> { Text = "Root" };

		/// <summary>
		/// Gets or sets the IDataProviderService instance to use to fetch data.
		/// </summary>
		[Parameter] public IDataProviderService<TItem> DataProvider { get; set; } = null!;

		/// <summary>
		/// A Linq expression that selects the field that contains the key value.
		/// </summary>
		[Parameter] public Func<TItem, object>? KeyField { get; set; }

		/// <summary>
		/// A Linq expression that selects the field that contains the parent key value.
		/// </summary>
		[Parameter] public Func<TItem, object>? ParentKeyField { get; set; }

		/// <summary>
		/// A Linq expression that selects the field to display for the item.
		/// </summary>
		[Parameter] public Func<TItem, object>? TextField { get; set; }

		/// <summary>
		/// A Linq expression that determines whether the given item is a leaf in the tree.
		/// </summary>
		[Parameter] public Func<TItem, bool>? IsLeaf { get; set; }

		/// <summary>
		/// Gets or sets whether a non-leaf node will request data where necessary.
		/// </summary>
		[Parameter] public bool LoadOnDemand { get; set; }

		/// <summary>
		/// Gets or sets whether expanded nodes should show lines to help identify nested levels.
		/// </summary>
		[Parameter] public bool ShowLines { get; set; }

		/// <summary>
		/// Gets or sets whether the root node is displayed.
		/// </summary>
		[Parameter] public bool ShowRoot { get; set; } = true;

		/// <summary>
		/// Gets or sets whether selection is allowed.
		/// </summary>
		[Parameter] public bool AllowSelection { get; set; }

		/// <summary>
		/// Gets or sets whether node edit is allowed.
		/// </summary>
		[Parameter] public bool AllowEdit { get; set; }

		/// <summary>
		/// Gets or sets the template to render for each node.
		/// </summary>
		[Parameter] public RenderFragment<TreeNode<TItem>>? NodeTemplate { get; set; }

		/// <summary>
		/// Gets or sets an event callback raise whenever the selection changes.
		/// </summary>
		[Parameter] public EventCallback<TreeNode<TItem>> SelectionChange { get; set; }

		/// <summary>
		/// Callback fired whenever the user expands a node.
		/// </summary>
		[Parameter] public EventCallback<TreeNode<TItem>> NodeExpanded { get; set; }

		/// <summary>
		/// Callback fired whenever the user collapses a node.
		/// </summary>
		[Parameter] public EventCallback<TreeNode<TItem>> NodeCollapsed { get; set; }

		/// <summary>
		/// Callback fired whenever data items are loaded.
		/// </summary>
		/// <remarks>The callback allows the items to be modified by the calling application.</remarks>
		[Parameter] public EventCallback<List<TItem>> ItemsLoaded { get; set; }

		/// <summary>
		/// Callback fired whenever a tree node is updated.
		/// </summary>
		[Parameter] public EventCallback<TreeNode<TItem>> NodeUpdated { get; set; }

		/// <summary>
		/// Callback fired before a node edit begins.
		/// </summary>
		[Parameter] public EventCallback<TreeNodeBeforeEditEventArgs<TItem>> BeforeEdit { get; set; }

		/// <summary>
		/// Callback fired after a node edit ends.
		/// </summary>
		[Parameter] public EventCallback<TreeNodeAfterEditEventArgs<TItem>> AfterEdit { get; set; }

		/// <summary>
		/// Gets or sets whether nodes may be dragged.
		/// </summary>
		[Parameter] public bool AllowDrag { get; set; }

		/// <summary>
		/// Gets or sets whether items may be dropped onto nodes.
		/// </summary>
		[Parameter] public bool AllowDrop { get; set; }

		/// <summary>
		/// Callback fired whenever a drag operation ends on a node within the tree.
		/// </summary>
		[Parameter] public EventCallback<DropEventArgs> Drop { get; set; }

		/// <summary>
		/// Callback fired whenever the user presses a key down.
		/// </summary>
		[Parameter] public EventCallback<KeyboardEventArgs> KeyDown { get; set; }

		/// <summary>
		/// Gets the currently selected tree node.
		/// </summary>
		public TreeNode<TItem>? SelectedNode { get; private set; }

		/// <summary>
		/// Expands all the branch nodes in the tree.
		/// </summary>
		public void ExpandAll()
		{
			RootNode.Walk((n) => { n.IsExpanded = !n.Isleaf; return true; });
		}

		/// <summary>
		/// Collapses all the branch nodes in the tree.
		/// </summary>
		public void CollapseAll()
		{
			RootNode.Walk((n) => { n.IsExpanded = false; return true; });
		}

		/// <summary>
		/// Searches all nodes until the given criteria is first macthed.
		/// </summary>
		public TreeNode<TItem>? Search(Predicate<TreeNode<TItem>> predicate)
		{
			TreeNode<TItem>? found = null;
			RootNode.Walk((n) =>
			{
				if (predicate(n))
				{
					found = n;
					return false;
				}
				return true;
			});
			return found;
		}

		/// <summary>
		/// Selects the given node.
		/// </summary>
		/// <param name="node">The node to select.</param>
		public async Task SelectNode(TreeNode<TItem> node)
		{
			if (AllowSelection)
			{
				// end edit mode
				if (SelectedNode != null)
				{
					await CommitEdit().ConfigureAwait(true);
				}

				// select new node
				if (SelectedNode != node)
				{
					if (SelectedNode != null)
					{
						SelectedNode.IsSelected = false;
					}
					SelectedNode = node;
					SelectedNode.IsSelected = true;

					// ensure all parent nodes are expanded
					var parentNode = SelectedNode.ParentNode;
					while (parentNode != null)
					{
						parentNode.IsExpanded = true;
						parentNode = parentNode.ParentNode;
					}

					// notify of change
					await SelectionChange.InvokeAsync(SelectedNode).ConfigureAwait(true);
					StateHasChanged();
				}
			}
		}

		/// <summary>
		/// Toggles the given nodes expanded state.
		/// </summary>
		/// <param name="node">The node to be expanded or collapsed.</param>
		public async Task ToggleNodeIsExpandedAsync(TreeNode<TItem> node)
		{
			var wasExpanded = node.IsExpanded;

			// if expanding and Nodes is null then request data
			if (!node.Isleaf && !wasExpanded && node.Nodes == null)
			{
				// fetch direct child items
				var key = KeyField!(node.Data!).ToString();
				var items = await GetDataAsync(key).ConfigureAwait(true);
				// add new nodes to existing node
				node.Nodes = new List<TreeNode<TItem>>(); // indicates data fetched, even if no items returned
				BuildModel(items);
				// notify any listeners that new data fetched
				await NodeUpdated.InvokeAsync(node).ConfigureAwait(true);
			}

			// expand / collapse and notify
			node.IsExpanded = !wasExpanded;
			if (wasExpanded)
			{
				await NodeCollapsed.InvokeAsync(node).ConfigureAwait(true);
			}
			else
			{
				await NodeExpanded.InvokeAsync(node).ConfigureAwait(true);
			}
		}

		/// <summary>
		/// Refreshes the given node, re-loading sub-nodes if applicable.
		/// </summary>
		/// <param name="node">The node to refresh.</param>
		public async Task RefreshNodeAsync(TreeNode<TItem> node)
		{
			node.IsExpanded = false;
			node.Nodes = null;
			await ToggleNodeIsExpandedAsync(node).ConfigureAwait(true);
		}

		/// <summary>
		/// Request to remove the specified node.
		/// </summary>
		/// <param name="node">The node to be removed.</param>
		public async Task RemoveNodeAsync(TreeNode<TItem> node)
		{
			if (node?.ParentNode?.Nodes != null)
			{
				// remove node from parent and select parent
				node.ParentNode.Nodes.Remove(node);
				await SelectNode(node.ParentNode).ConfigureAwait(true);
			}
		}

		/// <summary>
		/// Places the currently selected node into edit mode.
		/// </summary>
		public async Task BeginEdit()
		{
			if (AllowEdit && SelectedNode != null)
			{
				// commit any other edits
				RootNode.Walk((x) => { x.CommitEdit(); return true; });

				// notify and allow cancel
				var args = new TreeNodeBeforeEditEventArgs<TItem>(SelectedNode);
				await BeforeEdit.InvokeAsync(args).ConfigureAwait(true);
				if (!args.Cancel)
				{
					SelectedNode.BeginEdit();
					StateHasChanged();
				}
			}
		}

		/// <summary>
		/// Saves the current edit.
		/// </summary>
		public async Task CommitEdit()
		{
			if (SelectedNode?.IsEditing == true)
			{
				if (string.IsNullOrWhiteSpace(SelectedNode.EditText) || SelectedNode.HasSiblingWithText(SelectedNode.EditText))
				{
					SelectedNode.CancelEdit();
				}
				else
				{
					// notify and allow cancel
					var afterEditArgs = new TreeNodeAfterEditEventArgs<TItem>(SelectedNode, SelectedNode.Text, SelectedNode.EditText);
					await AfterEdit.InvokeAsync(afterEditArgs).ConfigureAwait(true);
					if (afterEditArgs.Cancel)
					{
						SelectedNode.CancelEdit();
					}
					else
					{
						SelectedNode.EditText = afterEditArgs.NewValue; // application my of altered
						SelectedNode.CommitEdit();
					}
				}
			}
		}

		/// <summary>
		/// Cancels the current edit.
		/// </summary>
		public void CancelEdit()
		{
			if (SelectedNode?.IsEditing == true)
			{
				SelectedNode.CancelEdit();
			}
		}

		private async Task<IEnumerable<TItem>> GetDataAsync(string? key = null)
		{
			try
			{
				BlockOverlayService.Show();
				var request = new DataRequest<TItem>
				{
					Skip = 0,
					ForceUpdate = false,
					// if load on demand and item key is given then only fetch immediate child items
					SearchText = LoadOnDemand ? key ?? string.Empty : null
				};

				// perform query data
				var response = await DataProvider
					.GetDataAsync(request, CancellationToken.None)
					.ConfigureAwait(true);

				// allow calling application to filter/add items etc
				var items = new List<TItem>(response.Items);
				await ItemsLoaded.InvokeAsync(items).ConfigureAwait(true);

				return items;
			}
			finally
			{
				BlockOverlayService.Hide();
			}
		}

		private TreeNode<TItem> BuildModel(IEnumerable<TItem> items)
		{
			var root = new TreeNode<TItem>();
			var modifiedNodes = new List<TreeNode<TItem>>();

			foreach (var item in items)
			{
				// get key value and check is given and unique
				var key = KeyField!(item)?.ToString();
				if (key == null)
				{
					throw new PDTreeException($"Items must supply a key value.");
				}

				// create node
				var node = new TreeNode<TItem>
				{
					Key = key,
					Text = TextField?.Invoke(item)?.ToString() ?? item.ToString(),
					IsExpanded = false,
					Data = item,
					Nodes = LoadOnDemand ? null : new List<TreeNode<TItem>>()
				};
				if (LoadOnDemand && IsLeaf != null && IsLeaf(item))
				{
					node.Nodes = new List<TreeNode<TItem>>();
				}

				// get parent key and find parent if not root item
				var parentKey = ParentKeyField?.Invoke(item)?.ToString();
				if (parentKey == null || string.IsNullOrWhiteSpace(parentKey))
				{
					(root.Nodes ??= new List<TreeNode<TItem>>()).Add(node);
				}
				else
				{
					var parentNode = root.Find(parentKey) ?? RootNode.Find(parentKey);
					if (parentNode == null)
					{
						throw new PDTreeException($"A parent item with key '{parentKey}' could not be found");
					}
					else
					{
						node.ParentNode = parentNode;
						(parentNode.Nodes ??= new List<TreeNode<TItem>>()).Add(node);
						if (!modifiedNodes.Contains(parentNode))
							modifiedNodes.Add(parentNode);
					}
				}
			}

			// re-apply sorts where necessary
			foreach (var node in modifiedNodes)
			{
				node?.Nodes?.Sort();
			}

			return root.Nodes?.Count == 1
				? root.Nodes[0]
				: root;
		}

		protected override void OnInitialized()
		{
			Id = $"{IdPrefix}{++_idSequence}";
		}

		protected async override Task OnAfterRenderAsync(bool firstRender)
		{
			if (firstRender)
			{
				// build initial model and notify listeners
				var items = await GetDataAsync().ConfigureAwait(true);
				RootNode = BuildModel(items);
				// notify that node updated
				await NodeUpdated.InvokeAsync(RootNode);
				StateHasChanged();
			}
		}

		protected override void OnParametersSet()
		{
			if (KeyField == null)
				throw new PDTreeException("KeyField attribute is required.");
			if (ParentKeyField == null)
				throw new PDTreeException("ParentKeyField attribute is required.");
		}

		private async Task OnEndEdit()
		{
			// end any current edit
			await CommitEdit().ConfigureAwait(true);
			// re-focus the tree
			JSRuntime?.InvokeVoidAsync("focus", Id);
		}

		private async Task OnKeyDown(KeyboardEventArgs args)
		{
			switch (args.Code)
			{
				case "Escape":
					CancelEdit();
					break;

				case "Enter":
				case "Return":
					await CommitEdit().ConfigureAwait(true);
					break;
			}

			if (SelectedNode?.IsEditing == false)
			{
				switch (args.Code)
				{
					case "F2":
						await BeginEdit().ConfigureAwait(true);
						break;

					case "ArrowRight":
						if (!SelectedNode.IsExpanded)
						{
							await ToggleNodeIsExpandedAsync(SelectedNode).ConfigureAwait(true);
						}
						break;

					case "ArrowLeft":
						if (SelectedNode.IsExpanded)
						{
							await ToggleNodeIsExpandedAsync(SelectedNode).ConfigureAwait(true);
						}
						break;

					case "ArrowDown":
						var nextNode = SelectedNode.GetNext();
						if (nextNode != null)
						{
							await SelectNode(nextNode).ConfigureAwait(true);
						}
						break;

					case "ArrowUp":
						var prevNode = SelectedNode.GetPrevious();
						if (prevNode != null)
						{
							// do not select previous node if that is the hidden root node
							if (ShowRoot || prevNode.ParentNode != null)
							{
								await SelectNode(prevNode).ConfigureAwait(true);
							}
						}
						break;
				}
			}

			await KeyDown.InvokeAsync(args).ConfigureAwait(true);
		}

		private async Task OnDrop(DropEventArgs args)
		{
			if (args.Target is TreeNode<TItem> node)
			{
				await Drop.InvokeAsync(args).ConfigureAwait(true);
				await RefreshNodeAsync(node).ConfigureAwait(true);
			}
		}
	}
}