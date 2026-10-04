using AwesomeAssertions;
using Bunit;
using Microsoft.AspNetCore.Components.Web;

namespace PanoramicData.Blazor.Test;

/// <summary>
/// Tests that <see cref="PDTreeMap{TItem}"/> lays out and draws its hierarchy, colours and labels the
/// rectangles, selects and zooms by mouse and keyboard, keeps a breadcrumb of the zoom path, reports
/// errors from caller-supplied selectors, and measures its container through JavaScript only when it
/// has no fixed size.
/// </summary>
public partial class PDTreeMapTests : BunitContext
{
	private const string ModulePath = "./_content/PanoramicData.Blazor/PDTreeMap.razor.js";

	private readonly Node _a1 = new("A1", 40);
	private readonly Node _a2 = new("A2", 20);
	private readonly Node _a;
	private readonly Node _b = new("B", 30);
	private readonly Node _c = new("C", 10);
	private readonly Node _root;
	private readonly List<Exception> _errors = [];

	/// <summary>Sets up the rendering context and a small hierarchy.</summary>
	public PDTreeMapTests()
	{
		JSInterop.Mode = JSRuntimeMode.Loose;
		_a = new Node("A", 0, _a1, _a2);
		_root = new Node("Root", 0, _a, _b, _c);
	}

	/// <summary>
	/// Verifies that an empty hierarchy shows the empty text and no SVG.
	/// </summary>
	[Fact]
	public void An_empty_hierarchy_shows_the_empty_text()
	{
		var component = RenderMap(p => p.Add(x => x.EmptyText, "Nothing here"), root: null);

		component.Find(".pdtm-empty").TextContent.Should().Be("Nothing here");
		component.FindAll("svg").Should().BeEmpty();
	}

	/// <summary>
	/// Verifies that every laid out rectangle is drawn as a tree item with its level, name and geometry.
	/// </summary>
	[Fact]
	public void Each_rectangle_is_drawn_as_an_accessible_tree_item()
	{
		var component = RenderMap();

		var rects = component.Instance.Rectangles;
		rects.Select(r => r.Item.Name).Should().BeEquivalentTo(["A", "A1", "A2", "B", "C"]);
		var nodes = component.FindAll("g.pdtm-node");
		nodes.Should().HaveCount(rects.Count);
		var svg = component.Find("svg");
		svg.GetAttribute("viewBox").Should().Be("0 0 400 200");
		svg.GetAttribute("aria-label").Should().Be("Tree map");

		var index = IndexOf(component, _a1);
		nodes[index].GetAttribute("aria-level").Should().Be("2");
		nodes[index].GetAttribute("aria-label").Should().Be("A1, 40");
		nodes[index].GetAttribute("aria-selected").Should().Be("false");
		nodes[index].QuerySelector("title")!.TextContent.Should().Be("A1, 40");
		nodes[index].QuerySelector("rect")!.GetAttribute("rx").Should().Be("2");
	}

	/// <summary>
	/// Verifies that a branch cut off by the render depth is described as containing further items.
	/// </summary>
	[Fact]
	public void A_branch_at_the_depth_cut_is_marked_aggregated()
	{
		var component = RenderMap(p => p.Add(x => x.MaxRenderDepth, 1));

		var node = component.FindAll("g.pdtm-node")[IndexOf(component, _a)];
		node.ClassList.Should().Contain("pdtm-node-aggregated");
		node.GetAttribute("aria-label").Should().Be("A, 60, contains further items");
	}

	private IRenderedComponent<PDTreeMap<Node>> RenderMap(
		Action<ComponentParameterCollectionBuilder<PDTreeMap<Node>>>? configure = null,
		double minLabelPx = 1,
		Func<Node, string>? textSelector = null,
		Func<Node, double>? sizeSelector = null)
		=> RenderMap(configure, _root, minLabelPx, textSelector, sizeSelector);

	private IRenderedComponent<PDTreeMap<Node>> RenderMap(
		Action<ComponentParameterCollectionBuilder<PDTreeMap<Node>>>? configure,
		Node? root,
		double minLabelPx = 1,
		Func<Node, string>? textSelector = null,
		Func<Node, double>? sizeSelector = null)
		=> Render<PDTreeMap<Node>>(parameters =>
		{
			AddHierarchy(parameters, root, textSelector, sizeSelector)
				.Add(p => p.Width, 400)
				.Add(p => p.Height, 200)
				.Add(p => p.MinLabelPx, minLabelPx)
				.Add(p => p.ExceptionHandler, e => _errors.Add(e));
			configure?.Invoke(parameters);
		});

	private ComponentParameterCollectionBuilder<PDTreeMap<Node>> AddHierarchy(ComponentParameterCollectionBuilder<PDTreeMap<Node>> parameters)
		=> AddHierarchy(parameters, _root, null, null);

	private static ComponentParameterCollectionBuilder<PDTreeMap<Node>> AddHierarchy(
		ComponentParameterCollectionBuilder<PDTreeMap<Node>> parameters,
		Node? root,
		Func<Node, string>? textSelector,
		Func<Node, double>? sizeSelector)
		=> parameters
			.Add(p => p.Root, root)
			.Add(p => p.ChildrenSelector, n => n.Children)
			.Add(p => p.SizeSelector, sizeSelector ?? (n => n.Size))
			.Add(p => p.TextSelector, textSelector ?? (n => n.Name));

	private static int IndexOf(IRenderedComponent<PDTreeMap<Node>> component, Node node)
		=> component.Instance.Rectangles.ToList().FindIndex(r => r.Item == node);

	private static string? FillOf(IRenderedComponent<PDTreeMap<Node>> component, Node node)
		=> component.FindAll("g.pdtm-node")[IndexOf(component, node)].QuerySelector("rect")!.GetAttribute("fill");

	private static Task PressKeyAsync(IRenderedComponent<PDTreeMap<Node>> component, string key)
		=> component.InvokeAsync(() => component.Find("svg").KeyDownAsync(new KeyboardEventArgs { Key = key }));

	/// <summary>
	/// A node in the test hierarchy.
	/// </summary>
	/// <param name="name">The node name.</param>
	/// <param name="size">The node's own size.</param>
	/// <param name="children">The child nodes.</param>
	private sealed class Node(string name, double size, params Node[] children)
	{
		/// <summary>Gets the node name.</summary>
		public string Name { get; } = name;

		/// <summary>Gets the node's own size.</summary>
		public double Size { get; } = size;

		/// <summary>Gets the child nodes, or null for a leaf.</summary>
		public Node[]? Children { get; } = children.Length == 0 ? null : children;

		/// <inheritdoc />
		public override string ToString() => Name;
	}
}
