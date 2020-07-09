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
		private TreeNode<TItem>? _currentSelection;

		private Func<TItem, object>? CompiledKeyFunc => _compiledKeyFunc ??= KeyField?.Compile();
		private Func<TItem, object>? CompiledParentKeyFunc => _compiledParentKeyFunc ??= ParentKeyField?.Compile();
		private Func<TItem, object>? CompiledTextFunc => _compiledTextFunc ??= TextField?.Compile();

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
		/// Gets the currently selected item.
		/// </summary>
		public TItem? Selection { get => _currentSelection?.Data; }

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
		/// Function that walks the tree calling the given function at each node until no more nodes
		/// or the function returns false.
		/// </summary>
		/// <param name="fn">Function to be called for each node. Returns false to stop walking.</param>
		private void WalkTree(Func<TreeNode<TItem>, bool> fn)
		{
			if(_model != null)
			{
				walkTree(_model, fn);
			}

			// local recursive function to actually walk tree
			// returns true is walking should stop
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
						if(!walkTree(subNode, fn))
						{
							return false;
						}
					}
				}
				return true;
			}
		}

		protected async override Task OnInitializedAsync()
		{
			await GetDataAsync();
		}

		protected async Task GetDataAsync()
		{
			try
			{
				var request = new DataRequest<TItem>
				{
					Skip = 0,
					ForceUpdate = false
				};

				// perform query data
				var response = await DataProvider
					.GetDataAsync(request, CancellationToken.None)
					.ConfigureAwait(true);

				// build model
				_model = BuildModel(response.Items);
			}
			finally
			{
			}
		}

		public void SelectNode(TreeNode<TItem> node)
		{
			if (AllowSelection && node != _currentSelection)
			{
				if (_currentSelection != null)
				{
					_currentSelection.IsSelected = false;
				}
				_currentSelection = node;
				_currentSelection.IsSelected = true;
				StateHasChanged();
			}
		}

		private TreeNode<TItem> BuildModel(IEnumerable<TItem> items)
		{
			var root = new TreeNode<TItem>();
			var dict = new Dictionary<string, TreeNode<TItem>>();

			foreach (var item in items)
			{
				// get key value and check is given and unique
				var key = CompiledKeyFunc?.Invoke(item)?.ToString();
				if (key == null)
				{
					throw new PDTreeException($"Items must supply a key value.");
				}
				if (dict.ContainsKey(key))
				{
					throw new PDTreeException($"An item with the key value '{key}' has already been added. Key values must be unique.");
				}

				// create node
				var node = new TreeNode<TItem>
				{
					Key = key,
					Text = CompiledTextFunc?.Invoke(item)?.ToString() ?? string.Empty,
					IsExpanded = false,
					Data = item
				};

				// get parent key
				var parentKey = CompiledParentKeyFunc?.Invoke(item)?.ToString();
				if (string.IsNullOrWhiteSpace(parentKey))
				{
					// root item
					(root.Nodes ?? (root.Nodes = new List<TreeNode<TItem>>())).Add(node);
				}
				else if (dict.ContainsKey(parentKey))
				{
					// child node
					var parentNode = dict[parentKey];
					(parentNode.Nodes ?? (parentNode.Nodes = new List<TreeNode<TItem>>())).Add(node);
				}
				else
				{
					throw new PDTreeException($"An item with the parent key value '{parentKey}' was found, but no existing item contains the that key.");
				}

				dict.Add(key, node);
			}

			return root.Nodes?.Count == 1
				? root.Nodes[0]
				: root;
		}
	}
}
