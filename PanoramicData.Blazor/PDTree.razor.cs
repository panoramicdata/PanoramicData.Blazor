using System;
using System.Threading;
using System.Threading.Tasks;
using System.Linq.Expressions;
using System.Collections.Generic;
using Microsoft.AspNetCore.Components;
using PanoramicData.Blazor.Services;
using PanoramicData.Blazor.Exceptions;
using System.Text;

namespace PanoramicData.Blazor
{
    public partial class PDTree<TItem>
    {
		private TreeNode? _model;
		private Func<TItem, object>? _compiledKeyFunc;
		private Func<TItem, object>? _compiledParentKeyFunc;
		private Func<TItem, object>? _compiledTextFunc;

		private Func<TItem, object>? CompiledKeyFunc => _compiledKeyFunc ??= KeyField?.Compile();
		private Func<TItem, object>? CompiledParentKeyFunc => _compiledParentKeyFunc ??= ParentKeyField?.Compile();
		private Func<TItem, object>? CompiledTextFunc => _compiledTextFunc ??= TextField?.Compile();

		/// <summary>
		/// Gets or sets the IDataProviderService instance to use to fetch data.
		/// </summary>
		[Parameter] public IDataProviderService<TItem> DataProvider { get; set; } = null!;

		/// <summary>
		/// A Linq expression that selects the field that contains th ekey value.
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

		private TreeNode BuildModel(IEnumerable<TItem> items)
		{
			var root = new TreeNode();
			var dict = new Dictionary<string, TreeNode>();

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
				var node = new TreeNode
				{
					Key = key,
					Text = CompiledTextFunc?.Invoke(item)?.ToString() ?? string.Empty
				};

				// get parent key
				var parentKey = CompiledParentKeyFunc?.Invoke(item)?.ToString();
				if (string.IsNullOrWhiteSpace(parentKey))
				{
					// root item
					(root.Nodes ?? (root.Nodes = new List<TreeNode>())).Add(node);
				}
				else if (dict.ContainsKey(parentKey))
				{
					// child node
					var parentNode = dict[parentKey];
					(parentNode.Nodes ?? (parentNode.Nodes = new List<TreeNode>())).Add(node);
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

		private MarkupString RenderTree()
		{
			if (_model == null) return new MarkupString();
			var sb = new StringBuilder();
			RenderNode(_model, sb);
			return new MarkupString(sb.ToString());
		}

		private void RenderNode(TreeNode node, StringBuilder builder)
		{
			builder.Append("<div class=\"pl-3\">");
			builder.Append("<div>");
			builder.Append(node.Text);
			builder.Append("</div>");
			if(!node.Isleaf && node.Nodes != null)
			{
				foreach(var subNode in node.Nodes)
				{
					RenderNode(subNode, builder);
				}
			}
			builder.AppendLine("</div>");
		}
	}
}
