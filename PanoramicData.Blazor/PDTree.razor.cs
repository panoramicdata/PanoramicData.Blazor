using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Collections.Generic;
using Microsoft.AspNetCore.Components;
using PanoramicData.Blazor.Services;
using PanoramicData.Blazor.Exceptions;

namespace PanoramicData.Blazor
{
    public partial class PDTree<TItem> where TItem : class
    {
		private TreeNode<TItem> _model = new TreeNode<TItem> { Text = "Root" };
		private readonly Dictionary<string, TreeNode<TItem>> _items = new Dictionary<string, TreeNode<TItem>>();

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
		[Parameter] public bool AllowSelection { get; set; } = false;

		/// <summary>
		/// Gets or sets the template to render for each node.
		/// </summary>
		[Parameter]
		public RenderFragment<TreeNode<TItem>>? NodeTemplate { get; set; }

		/// <summary>
		/// Gets or sets an event callback raise whenever the selection changes.
		/// </summary>
		[Parameter]
		public EventCallback<TreeNode<TItem>> SelectionChange { get; set; }

		/// <summary>
		/// Callback fired whenever the user expands a node.
		/// </summary>
		[Parameter]
		public EventCallback<TreeNode<TItem>> NodeExpanded { get; set; }

		/// <summary>
		/// Callback fired whenever the user collapses a node.
		/// </summary>
		[Parameter]
		public EventCallback<TreeNode<TItem>> NodeCollapsed { get; set; }

		/// <summary>
		/// Callback fired whenever data items are loaded.
		/// </summary>
		/// <remarks>The callback allows the items to be modified by the calling application.</remarks>
		[Parameter]
		public EventCallback<List<TItem>> ItemsLoaded { get; set; }

		/// <summary>
		/// Callback fired whenever a tree node is updated.
		/// </summary>
		[Parameter]
		public EventCallback<TreeNode<TItem>> NodeUpdated { get; set; }

		/// <summary>
		/// Gets the currently selected tree node.
		/// </summary>
		public TreeNode<TItem>? SelectedNode { get; private set; }

		/// <summary>
		/// Expands all the branch nodes in the tree.
		/// </summary>
		public void ExpandAll()
		{
			WalkTree((n) => { n.IsExpanded = !n.Isleaf; return true; });
		}

		/// <summary>
		/// Collapses all the branch nodes in the tree.
		/// </summary>
		public void CollapseAll()
		{
			WalkTree((n) => { n.IsExpanded = false; return true; });
		}

		/// <summary>
		/// Attempts to find and select the given item.
		/// </summary>
		/// <param name="item">The item to find and select.</param>
		/// <remarks>Items are searched for by their key values.</remarks>
		public TreeNode<TItem>? FindNode(TItem item)
		{
			if (KeyField != null)
			{
				var key = KeyField!(item).ToString();
				return FindNode(key);
			}
			return null;
		}

		/// <summary>
		/// Attempts to find the node that represents the item with the specified key.
		/// </summary>
		/// <param name="key">Key of the data item whose node to find.</param>
		/// <returns>If found the TreeNode instance otherwise null.</returns>
		public TreeNode<TItem>? FindNode(string key)
		{
			TreeNode<TItem>? node = null;
			if (string.IsNullOrEmpty(key))
			{
				node = _model;
			}
			else
			{
				WalkTree((n) =>
				{
					if (n.Key == key)
					{
						node = n;
						return false; // stop search
					}
					return true;
				});
			}
			return node;
		}

		/// <summary>
		/// Selects the given node.
		/// </summary>
		/// <param name="node">The node to select.</param>
		public async Task SelectNode(TreeNode<TItem> node)
		{
			if (AllowSelection && node != SelectedNode)
			{
				// clear current selection
				if (SelectedNode != null)
				{
					SelectedNode.IsSelected = false;
				}

				// select new node
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
			if (!node.Isleaf)
			{
				node.IsExpanded = false;
				node.Nodes = null;
				await ToggleNodeIsExpandedAsync(node).ConfigureAwait(true);
			}
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
		/// Function that walks the tree calling the given function at each node until no more nodes
		/// or the function returns false.
		/// </summary>
		/// <param name="fn">Function to be called for each node. Returns false to stop walking.</param>
		public void WalkTree(Func<TreeNode<TItem>, bool> fn)
		{
			if (_model != null)
			{
				walkTree(_model, fn);
			}

			// local recursive function to actually walk tree, returns false if walking should stop
			bool walkTree(TreeNode<TItem> node, Func<TreeNode<TItem>, bool> fn)
			{
				if (!fn(node))
				{
					return false;
				}
				else if (node.Nodes != null)
				{
					foreach (var subNode in node.Nodes)
					{
						if (!walkTree(subNode, fn))
						{
							return false;
						}
					}
				}
				return true;
			}
		}

		protected async override Task OnAfterRenderAsync(bool firstRender)
		{
			if (firstRender)
			{
				// build initial model and notify listeners
				var items = await GetDataAsync().ConfigureAwait(true);
				_model = BuildModel(items);
				// notify that node updated
				await NodeUpdated.InvokeAsync(_model);
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

		private async Task<IEnumerable<TItem>> GetDataAsync(string? key = null)
		{
			try
			{
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
			}
		}

		private TreeNode<TItem> BuildModel(IEnumerable<TItem> items)
		{
			var root = new TreeNode<TItem>();

			foreach (var item in items)
			{
				// get key value and check is given and unique
				var key = KeyField!(item)?.ToString();
				if (key == null)
				{
					throw new PDTreeException($"Items must supply a key value.");
				}
				//if (_items.ContainsKey(key))
				//{
				//	throw new PDTreeException($"An item with the key value '{key}' has already been added. Key values must be unique.");
				//}

				// create node
				var node = new TreeNode<TItem>
				{
					Key = key,
					Text = TextField?.Invoke(item)?.ToString() ?? item.ToString(),
					IsExpanded = false,
					Data = item,
					Nodes = LoadOnDemand ? null : new List<TreeNode<TItem>>()
				};
				if(LoadOnDemand && IsLeaf != null && IsLeaf(item))
				{
					node.Nodes = new List<TreeNode<TItem>>();
				}

				// get parent key
				var parentKey = ParentKeyField?.Invoke(item)?.ToString();
				if (parentKey == null || string.IsNullOrWhiteSpace(parentKey))
				{
					// root item
					(root.Nodes ?? (root.Nodes = new List<TreeNode<TItem>>())).Add(node);
				}
				else if (_items.ContainsKey(parentKey))
				{
					// child node
					var parentNode = _items[parentKey];
					node.ParentNode = parentNode;
					(parentNode.Nodes ?? (parentNode.Nodes = new List<TreeNode<TItem>>())).Add(node);
				}
				else
				{
					throw new PDTreeException($"An item with the parent key value '{parentKey}' was found, but no existing item contains that key.");
				}

				// store data item
				_items[key] = node;
			}

			return root.Nodes?.Count == 1
				? root.Nodes[0]
				: root;
		}
	}
}
