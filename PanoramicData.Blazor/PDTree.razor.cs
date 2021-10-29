using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.JSInterop;
using PanoramicData.Blazor.Arguments;
using PanoramicData.Blazor.Exceptions;
using PanoramicData.Blazor.Interfaces;
using PanoramicData.Blazor.Models;
using PanoramicData.Blazor.Services;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace PanoramicData.Blazor
{
	public partial class PDTree<TItem> where TItem : class
	{
		private const string _idPrefix = "pd-tree-";
		private static int _idSequence;
		private int _clickCount;
		private Timer? _clickTimer;
		private TreeNode<TItem>? _clickedNode;

		#region Injected Parameters

		[Inject] public IJSRuntime? JSRuntime { get; set; }

		[Inject] protected IBlockOverlayService BlockOverlayService { get; set; } = null!;

		#endregion

		#region Cascading Parameters

		/// <summary>
		/// Provides access to the parent DragContext if it exists.
		/// </summary>
		[CascadingParameter] public PDDragContext? DragContext { get; set; }

		#endregion

		#region Parameters

		/// <summary>
		/// Should a node clear its child content on collapse? Doing so will force a re-load of child nodes
		/// if it is re-expanded. Only applicable when LoadOnDemand = true.
		/// </summary>
		[Parameter] public bool ClearOnCollapse { get; set; }

		/// <summary>
		/// Gets or sets the IDataProviderService instance to use to fetch data.
		/// </summary>
		[Parameter] public IDataProviderService<TItem> DataProvider { get; set; } = null!;

		/// <summary>
		/// Gets or sets a delegate to be called if an exception occurs.
		/// </summary>
		[Parameter] public EventCallback<Exception> ExceptionHandler { get; set; }

		/// <summary>
		/// A function that selects the field that contains the key value.
		/// </summary>
		[Parameter] public Func<TItem, object>? KeyField { get; set; }

		/// <summary>
		/// A function that selects the field that contains the parent key value.
		/// </summary>
		[Parameter] public Func<TItem, object>? ParentKeyField { get; set; }

		/// <summary>
		/// A function that selects the field to display for the item.
		/// </summary>
		[Parameter] public Func<TItem, object>? TextField { get; set; }

		/// <summary>
		/// A function that determines whether the given item is a leaf in the tree.
		/// </summary>
		[Parameter] public Func<TItem, bool>? IsLeaf { get; set; }

		/// <summary>
		/// A function that calculates the CSS classes used to show an icon for the given node.
		/// </summary>
		[Parameter] public Func<TItem, int, string>? IconCssClass { get; set; }

		/// <summary>
		/// A function used to determine sort order of child nodes.
		/// </summary>
		[Parameter] public Comparison<TItem>? Sort { get; set; }

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
		/// Predicate used to determine whether a node should be expanded when ExpandAll is called.
		/// </summary>
		[Parameter] public Predicate<TreeNode<TItem>>? ExpandOnExpandAll { get; set; }

		/// <summary>
		/// Gets or sets the template to render for each node.
		/// </summary>
		[Parameter] public RenderFragment<TreeNode<TItem>>? NodeTemplate { get; set; }

		/// <summary>
		/// Gets or sets an event callback raised when the component has perform all it initialization.
		/// </summary>
		[Parameter] public EventCallback Ready { get; set; }

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
		/// Gets or sets whether nodes can be dropped before or after other nodes.
		/// </summary>
		[Parameter] public bool AllowDropInBetween { get; set; }

		/// <summary>
		/// Callback fired whenever a drag operation ends on a node within the tree.
		/// </summary>
		[Parameter] public EventCallback<DropEventArgs> Drop { get; set; }

		/// <summary>
		/// Callback fired whenever the user presses a key down.
		/// </summary>
		[Parameter] public EventCallback<KeyboardEventArgs> KeyDown { get; set; }

		#endregion

		/// <summary>
		/// Gets the unique identifier of this tree.
		/// </summary>
		public string Id { get; private set; } = string.Empty;

		/// <summary>
		/// Gets or sets the root TreeNode instance.
		/// </summary>
		public TreeNode<TItem> RootNode { get; set; } = new TreeNode<TItem> { Text = "Root" };

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
		/// Expands all the branch nodes in the tree.
		/// </summary>
		public async Task ExpandAllAsync()
		{
			await RootNode.WalkAsync(async (n) =>
			{
				if (!n.IsExpanded && !n.Isleaf)
				{
					if (ExpandOnExpandAll == null || ExpandOnExpandAll(n))
					{
						await ToggleNodeIsExpandedAsync(n).ConfigureAwait(true);
					}
				}
				return true;
			}).ConfigureAwait(true);
		}

		/// <summary>
		/// Collapses all the branch nodes in the tree.
		/// </summary>
		public void CollapseAll()
		{
			RootNode.Walk((n) => { n.IsExpanded = false; return true; });
		}

		/// <summary>
		/// Searches all nodes until the given criteria is first matched.
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

		private void ClickTimerCallback(object state)
		{
			_clickTimer?.Dispose();
			_clickTimer = null;

			InvokeAsync(async () =>
			{
				if (_clickedNode != null)
				{
					if (_clickCount > 1)
					{
						await ToggleNodeIsExpandedAsync(_clickedNode).ConfigureAwait(true);
					}
					await SelectNode(_clickedNode).ConfigureAwait(true);
					StateHasChanged();
				}
			});

			_clickCount = 0;
		}

		public void NodeMouseUp(TreeNode<TItem> node)
		{
			if (_clickTimer == null || node != _clickedNode)
			{
				_clickTimer?.Dispose();
				_clickedNode = node;
				_clickCount = 1;
				_clickTimer = new Timer(ClickTimerCallback, null, 250, Timeout.Infinite);
			}
			else
			{
				_clickCount++;
			}
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
				if (SelectedNode == node)
				{
					// if the same node
					await BeginEdit().ConfigureAwait(true);
				}
				else
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

			if (node.Isleaf)
			{
				return;
			}

			// if expanding and Nodes is null then request data
			if (!node.Isleaf && !wasExpanded && node.Nodes == null)
			{
				// fetch direct child items
				var key = node.Data is null ? null : KeyField!(node.Data!).ToString();
				var items = await GetDataAsync(key).ConfigureAwait(true);
				// add new nodes to existing node
				node.Nodes = new List<TreeNode<TItem>>(); // indicates data fetched, even if no items returned
				UpdateModel(items);

				// notify any listeners that new data fetched
				await NodeUpdated.InvokeAsync(node).ConfigureAwait(true);
			}

			// expand / collapse and notify
			node.IsExpanded = !wasExpanded;
			if (wasExpanded)
			{
				if (LoadOnDemand && ClearOnCollapse)
				{
					node.Nodes = null;
				}

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
				if (string.IsNullOrWhiteSpace(SelectedNode.EditText))
				{
					SelectedNode.CancelEdit();
					await ExceptionHandler.InvokeAsync(new PDTreeException("A value is required")).ConfigureAwait(true);
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
						SelectedNode.EditText = afterEditArgs.NewValue; // application might of altered
						SelectedNode.CommitEdit();
						SelectedNode?.ParentNode?.Nodes?.Sort(NodeSort); // re-sort parent
						await JSRuntime.InvokeVoidAsync("panoramicData.focus", Id).ConfigureAwait(true);
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
			if (DataProvider is null)
			{
				return new TItem[0];
			}
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

		private void UpdateModel(IEnumerable<TItem> items)
		{
			var modifiedNodes = new List<TreeNode<TItem>>();
			var dict = new Dictionary<string, TreeNode<TItem>>();

			foreach (var item in items)
			{
				// get key value and check is given and unique
				var key = KeyField!(item)?.ToString();
				if (key is null || string.IsNullOrEmpty(key))
				{
					throw new PDTreeException("Items must supply a key value.");
				}

				// get parent key and find parent node
				var parentKey = ParentKeyField?.Invoke(item)?.ToString();
				TreeNode<TItem>? parentNode = null;
				if (parentKey == null || string.IsNullOrWhiteSpace(parentKey))
				{
					parentNode = RootNode;
				}
				else
				{
					parentNode = (dict.ContainsKey(parentKey)) ? parentNode = dict[parentKey] : RootNode.Find(parentKey);
				}
				if (parentNode == null)
				{
					throw new PDTreeException($"A parent item with key '{parentKey}' could not be found");
				}

				// does the node already exist?
				var node = parentNode.Find(key);
				if (node is null)
				{
					node = new TreeNode<TItem>();
					// add to parent node and mark parent node for re-sort
					(parentNode.Nodes ??= new List<TreeNode<TItem>>()).Add(node);
					if (!modifiedNodes.Contains(parentNode))
					{
						modifiedNodes.Add(parentNode);
					}
				}
				node.Key = key;
				node.Text = TextField?.Invoke(item)?.ToString() ?? item.ToString();
				node.IsExpanded = false;
				node.Data = item;
				node.ParentNode = parentNode;
				node.Level = parentNode.Level + 1;
				node.IconCssClass = IconCssClass?.Invoke(item, parentNode.Level + 1) ?? string.Empty;
				node.Nodes = LoadOnDemand ? null : new List<TreeNode<TItem>>();
				if (LoadOnDemand && IsLeaf != null && IsLeaf(item))
				{
					node.Nodes = new List<TreeNode<TItem>>();
				}

				// cache node for performance
				dict.Add(key, node);
			}

			// re-apply sorts where necessary
			foreach (var node in modifiedNodes)
			{
				node?.Nodes?.Sort(NodeSort);
			}
		}

		private int NodeSort(TreeNode<TItem> a, TreeNode<TItem> b)
		{
			if (Sort is null || a.Data is null || b.Data is null)
			{
				return a.Text.CompareTo(b.Text);
			}
			else
			{
				return Sort(a.Data, b.Data);
			}
		}

		protected override void OnInitialized()
		{
			Id = $"{_idPrefix}{++_idSequence}";
		}

		protected async override Task OnAfterRenderAsync(bool firstRender)
		{
			if (firstRender)
			{
				// build initial model and notify listeners
				var items = await GetDataAsync().ConfigureAwait(true);
				UpdateModel(items);

				// notify that node updated
				await NodeUpdated.InvokeAsync(RootNode).ConfigureAwait(true);

				// notify that initialization completed
				await Ready.InvokeAsync(null).ConfigureAwait(true);
				StateHasChanged();
			}
		}

		protected override void OnParametersSet()
		{
			if (KeyField == null)
			{
				throw new PDTreeException("KeyField attribute is required.");
			}
			if (ParentKeyField == null)
			{
				throw new PDTreeException("ParentKeyField attribute is required.");
			}
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
			if (args.Target is TreeNode<TItem>)
			{
				await Drop.InvokeAsync(args).ConfigureAwait(true);
			}
		}
	}
}