using AwesomeAssertions;
using Bunit;
using Microsoft.AspNetCore.Components.Web;
using PanoramicData.Blazor.Extensions;
using PanoramicData.Blazor.Models;

namespace PanoramicData.Blazor.Test.Components;

/// <summary>
/// Tests the public behaviour of <see cref="PDTree{TItem}"/>: loading, selection, expansion, editing,
/// keyboard and mouse handling. Refresh is covered separately by <see cref="PDTreeRefreshTests"/>.
/// </summary>
public partial class PDTreeTests : BunitContext
{
	private readonly ItemProvider _provider = new();
	private readonly List<TreeNode<Item>> _selections = [];
	private readonly List<Exception> _exceptions = [];

	/// <summary>Sets up the rendering context.</summary>
	public PDTreeTests()
	{
		JSInterop.Mode = JSRuntimeMode.Loose;
		Services.AddPanoramicDataBlazor();
	}

	private IRenderedComponent<PDTree<Item>> RenderItemTree(Action<ComponentParameterCollectionBuilder<PDTree<Item>>>? configure = null, bool allowSelection = true)
	{
		var tree = Render<PDTree<Item>>(parameters =>
		{
			parameters
				.Add(p => p.DataProvider, _provider)
				.Add(p => p.KeyField, item => item.Id)
				.Add(p => p.ParentKeyField, item => item.ParentId ?? string.Empty)
				.Add(p => p.TextField, item => item.Name)
				.Add(p => p.AllowSelection, allowSelection)
				.Add(p => p.SelectionChange, node => _selections.Add(node))
				.Add(p => p.ExceptionHandler, ex => _exceptions.Add(ex));
			configure?.Invoke(parameters);
		});
		tree.WaitForAssertion(() => tree.Instance.RootNode.Nodes.Should().NotBeNull());
		return tree;
	}

	private static TreeNode<Item> Node(IRenderedComponent<PDTree<Item>> tree, string key)
		=> tree.Instance.RootNode.Find(key) ?? throw new InvalidOperationException($"No node '{key}'.");

	private static Task SelectAsync(IRenderedComponent<PDTree<Item>> tree, string key)
		=> tree.InvokeAsync(() => tree.Instance.SelectNode(Node(tree, key), false));

	private static void Key(IRenderedComponent<PDTree<Item>> tree, string code)
		=> tree.Find("div.pdtree").KeyDown(new KeyboardEventArgs { Code = code });
}
