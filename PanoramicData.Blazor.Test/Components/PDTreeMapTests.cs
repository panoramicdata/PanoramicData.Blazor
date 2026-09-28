using System.Globalization;
using AwesomeAssertions;
using Bunit;
using Microsoft.AspNetCore.Components.Web;
using PanoramicData.Blazor.Arguments;
using PanoramicData.Blazor.Enums;
using PanoramicData.Blazor.Helpers;
using PanoramicData.Blazor.Models;

namespace PanoramicData.Blazor.Test;

/// <summary>
/// Tests that <see cref="PDTreeMap{TItem}"/> lays out and draws its hierarchy, colours and labels the
/// rectangles, selects and zooms by mouse and keyboard, keeps a breadcrumb of the zoom path, reports
/// errors from caller-supplied selectors, and measures its container through JavaScript only when it
/// has no fixed size.
/// </summary>
public class PDTreeMapTests : BunitContext
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

	/// <summary>
	/// Verifies that the text and tooltip selectors are used for labels and hover text.
	/// </summary>
	[Fact]
	public void Text_and_tooltip_selectors_are_used()
	{
		var component = RenderMap(
			p => p.Add(x => x.TooltipSelector, n => $"tip {n.Name}"),
			textSelector: n => $"[{n.Name}]");

		var node = component.FindAll("g.pdtm-node")[IndexOf(component, _b)];
		node.QuerySelector("title")!.TextContent.Should().Be("tip B");
		component.FindAll(".pdtm-label").Select(l => l.TextContent).Should().Contain("[B]");
	}

	/// <summary>
	/// Verifies that labels are positioned over their rectangles in percentages of the canvas.
	/// </summary>
	[Fact]
	public void Labels_are_positioned_in_percentages_of_the_canvas()
	{
		var component = RenderMap(p => p.Add(x => x.MaxRenderDepth, 1));
		var rect = component.Instance.Rectangles.Single(r => r.Item == _b);

		var label = component.FindAll(".pdtm-label").Single(l => l.TextContent == "B");

		var expectedLeft = (rect.X / 400 * 100).ToString("0.###", CultureInfo.InvariantCulture);
		label.GetAttribute("style").Should().StartWith($"left:{expectedLeft}%;")
			.And.EndWith("padding:4px;font-size:11px");
	}

	/// <summary>
	/// Verifies that no label is drawn on a rectangle narrower than the minimum label size.
	/// </summary>
	[Fact]
	public void Rectangles_below_the_minimum_label_size_have_no_label()
	{
		var component = RenderMap(minLabelPx: 1000);

		component.FindAll(".pdtm-label").Should().BeEmpty();
	}

	/// <summary>
	/// Verifies that clicking a rectangle selects and focuses it and raises SelectionChanged and Click.
	/// </summary>
	[Fact]
	public void Clicking_a_rectangle_selects_it_and_raises_events()
	{
		var events = new List<string>();
		var component = RenderMap(p => p
			.Add(x => x.SelectionChanged, () => events.Add("selection"))
			.Add(x => x.Click, n => events.Add($"click:{n.Name}")));
		var index = IndexOf(component, _b);

		component.FindAll("g.pdtm-node")[index].Click();

		events.Should().Equal("selection", "click:B");
		component.Instance.Selection.Should().BeSameAs(_b);
		var node = component.FindAll("g.pdtm-node")[index];
		node.GetAttribute("aria-selected").Should().Be("true");
		node.ClassList.Should().Contain("pdtm-node-focused");
	}

	/// <summary>
	/// Verifies that double-clicking a branch zooms into it and shows the breadcrumb.
	/// </summary>
	[Fact]
	public void Double_clicking_a_branch_zooms_into_it()
	{
		var events = new List<string>();
		var component = RenderMap(p => p
			.Add(x => x.DoubleClick, n => events.Add($"double:{n.Name}"))
			.Add(x => x.ZoomRootChanged, n => events.Add($"zoom:{n?.Name}")));

		component.FindAll("g.pdtm-node")[IndexOf(component, _a)].DoubleClick();

		events.Should().Equal("double:A", "zoom:A");
		component.Instance.Rectangles.Select(r => r.Item.Name).Should().BeEquivalentTo(["A1", "A2"]);
		var crumbs = component.FindAll(".pdtm-crumb");
		crumbs.Select(c => c.TextContent.Trim()).Should().Equal("Root", "A");
		crumbs[1].ClassList.Should().Contain("pdtm-crumb-current");
		crumbs[1].HasAttribute("disabled").Should().BeTrue();
		component.FindAll(".pdtm-crumb-separator").Should().ContainSingle();
	}

	/// <summary>
	/// Verifies that clicking the root breadcrumb zooms back out to the whole hierarchy.
	/// </summary>
	[Fact]
	public void The_root_breadcrumb_zooms_back_out()
	{
		var zooms = new List<Node?>();
		var component = RenderMap(p => p.Add(x => x.ZoomRootChanged, n => zooms.Add(n)));
		component.FindAll("g.pdtm-node")[IndexOf(component, _a)].DoubleClick();

		component.FindAll(".pdtm-crumb")[0].Click();

		zooms.Should().Equal(_a, null);
		component.FindAll(".pdtm-breadcrumb").Should().BeEmpty();
		component.Instance.Rectangles.Should().HaveCount(5);
	}

	/// <summary>
	/// Verifies that double-clicking a leaf raises DoubleClick but does not zoom.
	/// </summary>
	[Fact]
	public void Double_clicking_a_leaf_does_not_zoom()
	{
		var zooms = new List<Node?>();
		Node? doubleClicked = null;
		var component = RenderMap(p => p
			.Add(x => x.DoubleClick, n => doubleClicked = n)
			.Add(x => x.ZoomRootChanged, n => zooms.Add(n)));

		component.FindAll("g.pdtm-node")[IndexOf(component, _b)].DoubleClick();

		doubleClicked.Should().BeSameAs(_b);
		zooms.Should().BeEmpty();
	}

	/// <summary>
	/// Verifies that BeforeZoomChange receives the from and to items and can cancel the zoom.
	/// </summary>
	[Fact]
	public async Task BeforeZoomChange_can_cancel_the_zoom()
	{
		TreeMapBeforeZoomEventArgs<Node>? raised = null;
		var zooms = new List<Node?>();
		var component = RenderMap(p => p
			.Add(x => x.BeforeZoomChange, args =>
			{
				raised = args;
				args.Cancel = true;
			})
			.Add(x => x.ZoomRootChanged, n => zooms.Add(n)));

		await component.InvokeAsync(() => component.Instance.ZoomToAsync(_a));

		raised.Should().NotBeNull();
		raised!.From.Should().BeNull();
		raised.To.Should().BeSameAs(_a);
		zooms.Should().BeEmpty();
		component.Instance.Rectangles.Should().HaveCount(5);
	}

	/// <summary>
	/// Verifies that zooming to the current target, or to the root when not zoomed, does nothing.
	/// </summary>
	[Fact]
	public async Task Zooming_to_the_current_target_does_nothing()
	{
		var zooms = new List<Node?>();
		var component = RenderMap(p => p
			.Add(x => x.ZoomRoot, _a)
			.Add(x => x.ZoomRootChanged, n => zooms.Add(n)));

		await component.InvokeAsync(() => component.Instance.ZoomToAsync(_a));

		zooms.Should().BeEmpty();
		component.FindAll(".pdtm-crumb").Select(c => c.TextContent.Trim()).Should().Equal("Root", "A");
	}

	/// <summary>
	/// Verifies that the breadcrumb can be turned off.
	/// </summary>
	[Fact]
	public void The_breadcrumb_can_be_hidden()
	{
		var component = RenderMap(p => p.Add(x => x.ZoomRoot, _a).Add(x => x.ShowBreadcrumb, false));

		component.FindAll(".pdtm-breadcrumb").Should().BeEmpty();
	}

	/// <summary>
	/// Verifies that a zoom target outside the hierarchy leaves the breadcrumb at the root alone.
	/// </summary>
	[Fact]
	public void A_zoom_target_outside_the_hierarchy_has_no_breadcrumb_path()
	{
		var stranger = new Node("Stranger", 0, new Node("S1", 5));

		var component = RenderMap(p => p.Add(x => x.ZoomRoot, stranger));

		component.FindAll(".pdtm-breadcrumb").Should().BeEmpty();
		component.Instance.Rectangles.Should().ContainSingle().Which.Item.Name.Should().Be("S1");
	}

	/// <summary>
	/// Verifies that arrow, Home and End keys move the focus and selection through the rectangles.
	/// </summary>
	[Fact]
	public void Arrow_home_and_end_keys_move_the_selection()
	{
		var selections = 0;
		var component = RenderMap(p => p.Add(x => x.SelectionChanged, () => selections++));
		var rects = component.Instance.Rectangles;

		PressKey(component, "ArrowRight");
		component.Instance.Selection.Should().BeSameAs(rects[0].Item);
		PressKey(component, "ArrowDown");
		component.Instance.Selection.Should().BeSameAs(rects[1].Item);
		PressKey(component, "ArrowUp");
		component.Instance.Selection.Should().BeSameAs(rects[0].Item);
		PressKey(component, "End");
		component.Instance.Selection.Should().BeSameAs(rects[^1].Item);
		PressKey(component, "ArrowLeft");
		component.Instance.Selection.Should().BeSameAs(rects[^2].Item);
		PressKey(component, "Home");
		component.Instance.Selection.Should().BeSameAs(rects[0].Item);

		selections.Should().Be(6);
		component.FindAll("g.pdtm-node")[0].ClassList.Should().Contain("pdtm-node-focused");
	}

	/// <summary>
	/// Verifies that the arrow keys stop at the first and last rectangles.
	/// </summary>
	[Fact]
	public void Arrow_keys_stop_at_the_ends()
	{
		var component = RenderMap();
		var rects = component.Instance.Rectangles;

		PressKey(component, "ArrowLeft");
		component.Instance.Selection.Should().BeSameAs(rects[0].Item);

		PressKey(component, "End");
		PressKey(component, "ArrowRight");
		component.Instance.Selection.Should().BeSameAs(rects[^1].Item);
	}

	/// <summary>
	/// Verifies that Enter clicks and zooms into the focused branch, and Backspace zooms back out.
	/// </summary>
	[Fact]
	public void Enter_zooms_into_the_focused_branch_and_backspace_zooms_out()
	{
		var zooms = new List<Node?>();
		var component = RenderMap(p => p.Add(x => x.ZoomRootChanged, n => zooms.Add(n)));
		var index = IndexOf(component, _a);
		for (var i = 0; i <= index; i++)
		{
			PressKey(component, "ArrowRight");
		}

		PressKey(component, "Enter");
		PressKey(component, "Backspace");

		zooms.Should().Equal(_a, null);
	}

	/// <summary>
	/// Verifies that Enter with nothing focused, Escape at the root, and other keys do nothing.
	/// </summary>
	[Fact]
	public void Keys_with_nothing_to_act_on_do_nothing()
	{
		var events = new List<string>();
		var component = RenderMap(p => p
			.Add(x => x.SelectionChanged, () => events.Add("selection"))
			.Add(x => x.ZoomRootChanged, _ => events.Add("zoom")));

		PressKey(component, "Enter");
		PressKey(component, " ");
		PressKey(component, "Escape");
		PressKey(component, "x");

		events.Should().BeEmpty();
		component.Instance.Selection.Should().BeNull();
	}

	/// <summary>
	/// Verifies that an explicit colour selector overrides the colour mode, and a blank result falls back
	/// to it.
	/// </summary>
	[Fact]
	public void An_explicit_colour_overrides_the_colour_mode()
	{
		var component = RenderMap(p => p
			.Add(x => x.ColourSelector, n => n == _b ? "red" : " ")
			.Add(x => x.ColourMode, TreeMapColourMode.Custom));

		FillOf(component, _b).Should().Be("red");
		FillOf(component, _c).Should().Be(TreeMapPalette.Fallback());
	}

	/// <summary>
	/// Verifies that category colouring uses the palette colour for each item's category.
	/// </summary>
	[Fact]
	public void Category_colouring_uses_the_category_palette()
	{
		var component = RenderMap(p => p.Add(x => x.CategorySelector, n => n.Name[..1]));

		FillOf(component, _a1).Should().Be(TreeMapPalette.ForCategory("A"));
		FillOf(component, _b).Should().Be(TreeMapPalette.ForCategory("B"));
	}

	/// <summary>
	/// Verifies that depth colouring shades by each rectangle's depth.
	/// </summary>
	[Fact]
	public void Depth_colouring_shades_by_depth()
	{
		var component = RenderMap(p => p.Add(x => x.ColourMode, TreeMapColourMode.Depth));

		FillOf(component, _a).Should().Be(TreeMapPalette.ForDepth(0, 2));
		FillOf(component, _a1).Should().Be(TreeMapPalette.ForDepth(1, 2));
	}

	/// <summary>
	/// Verifies that heat colouring scales across the laid out values, ignoring values that are not
	/// numbers.
	/// </summary>
	[Fact]
	public void Heat_colouring_scales_across_the_values()
	{
		var heat = new Dictionary<string, double>
		{
			["A"] = double.NaN,
			["A1"] = 10,
			["A2"] = 20,
			["B"] = double.PositiveInfinity,
			["C"] = 50
		};

		var component = RenderMap(p => p
			.Add(x => x.ColourMode, TreeMapColourMode.Heat)
			.Add(x => x.HeatSelector, n => heat[n.Name]));

		FillOf(component, _a1).Should().Be(TreeMapPalette.ForHeat(10, 10, 50));
		FillOf(component, _c).Should().Be(TreeMapPalette.ForHeat(50, 10, 50));
	}

	/// <summary>
	/// Verifies that heat colouring with no heat selector, or with no usable values, still colours.
	/// </summary>
	[Fact]
	public void Heat_colouring_without_usable_values_falls_back()
	{
		var withoutSelector = RenderMap(p => p.Add(x => x.ColourMode, TreeMapColourMode.Heat));
		FillOf(withoutSelector, _b).Should().Be(TreeMapPalette.Fallback());

		var allNaN = RenderMap(p => p
			.Add(x => x.ColourMode, TreeMapColourMode.Heat)
			.Add(x => x.HeatSelector, _ => double.NaN));
		FillOf(allNaN, _b).Should().Be(TreeMapPalette.ForHeat(double.NaN, 0, 0));
	}

	/// <summary>
	/// Verifies that a throwing colour selector is reported and the fallback colour used.
	/// </summary>
	[Fact]
	public void A_throwing_colour_selector_is_reported_and_falls_back()
	{
		var component = RenderMap(p => p.Add(x => x.ColourSelector, _ => throw new InvalidOperationException("colour")));

		FillOf(component, _b).Should().Be(TreeMapPalette.Fallback());
		_errors.Should().NotBeEmpty().And.OnlyContain(e => e.Message == "colour");
	}

	/// <summary>
	/// Verifies that a throwing text selector is reported and an empty label used.
	/// </summary>
	[Fact]
	public void A_throwing_text_selector_is_reported()
	{
		var component = RenderMap(textSelector: _ => throw new InvalidOperationException("text"));

		component.FindAll(".pdtm-label").Should().OnlyContain(l => l.TextContent.Length == 0);
		_errors.Should().NotBeEmpty().And.OnlyContain(e => e.Message == "text");
	}

	/// <summary>
	/// Verifies that a layout failure is reported and the empty state shown.
	/// </summary>
	[Fact]
	public void A_layout_failure_is_reported_and_shows_the_empty_state()
	{
		var component = RenderMap(sizeSelector: _ => throw new InvalidOperationException("size"));

		component.Find(".pdtm-empty").Should().NotBeNull();
		_errors.Should().ContainSingle().Which.Message.Should().Be("size");
	}

	/// <summary>
	/// Verifies that fixed dimensions need no JavaScript.
	/// </summary>
	[Fact]
	public void Fixed_dimensions_do_not_load_the_module()
	{
		var module = JSInterop.SetupModule(ModulePath);

		RenderMap();

		module.Invocations["init"].Should().BeEmpty();
	}

	/// <summary>
	/// Verifies that without fixed dimensions the container is observed, and reported sizes lay it out.
	/// </summary>
	[Fact]
	public async Task Without_fixed_dimensions_the_container_size_drives_the_layout()
	{
		var module = JSInterop.SetupModule(ModulePath);
		var component = Render<PDTreeMap<Node>>(parameters => AddHierarchy(parameters));
		component.Find(".pdtm-empty").Should().NotBeNull("nothing can be laid out before the size is known");

		var init = module.VerifyInvoke("init");
		init.Arguments[0].Should().Be(component.Instance.Id);

		await component.InvokeAsync(() => component.Instance.OnContainerResized(300, 150));

		component.Find("svg").GetAttribute("viewBox").Should().Be("0 0 300 150");
		component.Instance.Rectangles.Should().HaveCount(5);
	}

	/// <summary>
	/// Verifies that sub-pixel resizes are ignored and a fixed dimension overrides the reported one.
	/// </summary>
	[Fact]
	public async Task Sub_pixel_resizes_are_ignored_and_a_fixed_width_wins()
	{
		JSInterop.SetupModule(ModulePath);
		var component = Render<PDTreeMap<Node>>(parameters => AddHierarchy(parameters).Add(p => p.Width, 250));

		await component.InvokeAsync(() => component.Instance.OnContainerResized(999, 100));
		component.Find("svg").GetAttribute("viewBox").Should().Be("0 0 250 100");

		await component.InvokeAsync(() => component.Instance.OnContainerResized(999, 100.5));
		component.Find("svg").GetAttribute("viewBox").Should().Be("0 0 250 100");
	}

	/// <summary>
	/// Verifies that disposing stops the container observation.
	/// </summary>
	[Fact]
	public async Task Disposing_stops_observing_the_container()
	{
		var module = JSInterop.SetupModule(ModulePath);
		var component = Render<PDTreeMap<Node>>(parameters => AddHierarchy(parameters));

		await component.Instance.DisposeAsync();

		module.VerifyInvoke("dispose").Arguments.Should().Equal(component.Instance.Id);
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

	private static void PressKey(IRenderedComponent<PDTreeMap<Node>> component, string key)
		=> component.Find("svg").KeyDown(new KeyboardEventArgs { Key = key });

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
