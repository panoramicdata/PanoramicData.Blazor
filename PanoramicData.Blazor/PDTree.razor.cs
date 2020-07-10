using System;
using System.Threading;
using System.Threading.Tasks;
using System.Linq.Expressions;
using System.Collections.Generic;
using Microsoft.AspNetCore.Components;
using PanoramicData.Blazor.Services;
using PanoramicData.Blazor.Exceptions;

namespace PanoramicData.Blazor
{
    public partial class PDTree<TItem> where TItem : class
    {
		private TreeNode<TItem> _model = new TreeNode<TItem> { Text = "Root" };
		private Func<TItem, object>? _compiledKeyFunc;
		private Func<TItem, object>? _compiledParentKeyFunc;
		private Func<TItem, object>? _compiledTextFunc;
		private Func<TItem, bool>? _compiledIsLeafFunc;
		private TreeNode<TItem>? _currentSelection;
		private readonly Dictionary<string, TreeNode<TItem>> _items = new Dictionary<string, TreeNode<TItem>>();

		private Func<TItem, object>? CompiledKeyFunc => _compiledKeyFunc ??= KeyField?.Compile();
		private Func<TItem, object>? CompiledParentKeyFunc => _compiledParentKeyFunc ??= ParentKeyField?.Compile();
		private Func<TItem, object>? CompiledTextFunc => _compiledTextFunc ??= TextField?.Compile();
		private Func<TItem, bool>? CompiledIsLeafFunc => _compiledIsLeafFunc ??= IsLeaf?.Compile();

		/// <summary>
		/// Gets or sets the IDataProviderService instance to use to fetch data.
		/// </summary>
		[Parameter] public IDataProviderService<TItem> DataProvider { get; set; } = null!;

		/// <summary>
		/// A Linq expression that selects the field that contains the key value.
		/// </summary>
		[Parameter] public Expression<Func<TItem, object>>? KeyField { get; set; }

		/// <summary>
		/// A Linq expression that selects the field that contains the parent key value.
		/// </summary>
		[Parameter] public Expression<Func<TItem, object>>? ParentKeyField { get; set; }

		/// <summary>
		/// A Linq expression that selects the field to display for the item.
		/// </summary>
		[Parameter] public Expression<Func<TItem, object>>? TextField { get; set; }

		/// <summary>
		/// A Linq expression that determines whether the given item is a leaf in the tree.
		/// </summary>
		[Parameter] public Expression<Func<TItem, bool>>? IsLeaf { get; set; }

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
		[Parameter] public bool AllowSelection { get; set; } = true;

		/// <summary>
		/// Gets or sets the template to render for each node.
		/// </summary>
		[Parameter]
		public RenderFragment<TreeNode<TItem>>? NodeTemplate { get; set; }

		/// <summary>
		/// Gets or sets an event callback raise whenever the selection changes.
		/// </summary>
		[Parameter]
		public EventCallback<TItem> SelectionChange { get; set; }

		/// <summary>
		/// Callback fired whenever the user expands a node.
		/// </summary>
		[Parameter]
		public EventCallback<TItem> NodeExpanded { get; set; }

		/// <summary>
		/// Callback fired whenever the user collapses a node.
		/// </summary>
		[Parameter]
		public EventCallback<TItem> NodeCollapsed { get; set; }


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
		public async Task SelectItemAsync(TItem item)
		{
			await SelectItemAsync(CompiledKeyFunc!.Invoke(item).ToString()).ConfigureAwait(true);
		}

		/// <summary>
		/// Attempts to find and select the given item.
		/// </summary>
		/// <param name="key">The key of the item to find and select.</param>
		public async Task SelectItemAsync(string key)
		{
			if (AllowSelection)
			{
				TreeNode<TItem>? node = null;
				WalkTree((n) =>
				{
					if (n.Key == key)
					{
						node = n;
						return false; // stop search
					}
					return true;
				});
				if (node != null)
				{
					await SelectNode(node).ConfigureAwait(true);
				}
			}
		}

		protected async override Task OnInitializedAsync()
		{
			// build model
			var items = await GetDataAsync();
			_model = BuildModel(items);
		}

		/// <summary>
		/// Function that walks the tree calling the given function at each node until no more nodes
		/// or the function returns false.
		/// </summary>
		/// <param name="fn">Function to be called for each node. Returns false to stop walking.</param>
		private void WalkTree(Func<TreeNode<TItem>, bool> fn)
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

				return response.Items;
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
				var key = CompiledKeyFunc?.Invoke(item)?.ToString();
				if (key == null)
				{
					throw new PDTreeException($"Items must supply a key value.");
				}
				if (_items.ContainsKey(key))
				{
					throw new PDTreeException($"An item with the key value '{key}' has already been added. Key values must be unique.");
				}

				// create node
				var node = new TreeNode<TItem>
				{
					Key = key,
					Text = CompiledTextFunc?.Invoke(item)?.ToString() ?? string.Empty,
					IsExpanded = false,
					Data = item,
					Nodes = LoadOnDemand ? null : new List<TreeNode<TItem>>()
				};
				if(LoadOnDemand && IsLeaf != null && CompiledIsLeafFunc!(item))
				{
					node.Nodes = new List<TreeNode<TItem>>();
				}

				// get parent key
				var parentKey = CompiledParentKeyFunc?.Invoke(item)?.ToString();
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
				_items.Add(key, node);
			}

			return root.Nodes?.Count == 1
				? root.Nodes[0]
				: root;
		}

		internal async Task SelectNode(TreeNode<TItem> node)
		{
			if (AllowSelection && node != _currentSelection)
			{
				// clear current selection
				if (_currentSelection != null)
				{
					_currentSelection.IsSelected = false;
				}

				// select new node
				_currentSelection = node;
				_currentSelection.IsSelected = true;

				// ensure all parent nodes are expanded
				var parentNode = _currentSelection.ParentNode;
				while(parentNode != null)
				{
					parentNode.IsExpanded = true;
					parentNode = parentNode.ParentNode;
				}

				// notify of change
				await SelectionChange.InvokeAsync(_currentSelection.Data!).ConfigureAwait(true);
				StateHasChanged();
			}
		}

		internal async Task ToggleNodeIsExpandedAsync(TreeNode<TItem> node)
		{
			var wasExpanded = node.IsExpanded;

			// if expanding and Nodes is null then request data
			if(!node.Isleaf && !wasExpanded && node.Nodes == null)
			{
				// fetch direct child items
				var key = CompiledKeyFunc!(node.Data!).ToString();
				var items = await GetDataAsync(key);

				// add new nodes to existing node
				node.Nodes = new List<TreeNode<TItem>>(); // indicates data fetched, even if no items returned
				BuildModel(items);
			}

			// expand / collapse and notify
			node.IsExpanded = !wasExpanded;
			if(wasExpanded)
			{
				await NodeCollapsed.InvokeAsync(node.Data!).ConfigureAwait(true);
			}
			else
			{
				await NodeExpanded.InvokeAsync(node.Data!).ConfigureAwait(true);
			}
		}
	}
}
