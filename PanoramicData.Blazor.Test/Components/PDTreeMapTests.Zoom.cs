using AwesomeAssertions;
using Bunit;
using Microsoft.AspNetCore.Components.Web;
using PanoramicData.Blazor.Arguments;

namespace PanoramicData.Blazor.Test;

/// <summary>
/// Selection, zoom and breadcrumb tests for <see cref="PDTreeMap{TItem}"/>.
/// </summary>
public partial class PDTreeMapTests
{
	/// <summary>
	/// Verifies that clicking a rectangle selects and focuses it and raises SelectionChanged and Click.
	/// </summary>
	[Fact]
	public async Task Clicking_a_rectangle_selects_it_and_raises_events()
	{
		var events = new List<string>();
		var component = RenderMap(p => p
			.Add(x => x.SelectionChanged, () => events.Add("selection"))
			.Add(x => x.Click, n => events.Add($"click:{n.Name}")));
		var index = IndexOf(component, _b);

		await component.InvokeAsync(() => component.FindAll("g.pdtm-node")[index].ClickAsync(new MouseEventArgs()));

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
	public async Task Double_clicking_a_branch_zooms_into_it()
	{
		var events = new List<string>();
		var component = RenderMap(p => p
			.Add(x => x.DoubleClick, n => events.Add($"double:{n.Name}"))
			.Add(x => x.ZoomRootChanged, n => events.Add($"zoom:{n?.Name}")));

		await component.InvokeAsync(() => component.FindAll("g.pdtm-node")[IndexOf(component, _a)].DoubleClickAsync(new MouseEventArgs()));

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
	public async Task The_root_breadcrumb_zooms_back_out()
	{
		var zooms = new List<Node?>();
		var component = RenderMap(p => p.Add(x => x.ZoomRootChanged, n => zooms.Add(n)));
		await component.InvokeAsync(() => component.FindAll("g.pdtm-node")[IndexOf(component, _a)].DoubleClickAsync(new MouseEventArgs()));

		await component.InvokeAsync(() => component.FindAll(".pdtm-crumb")[0].ClickAsync(new MouseEventArgs()));

		zooms.Should().Equal(_a, null);
		component.FindAll(".pdtm-breadcrumb").Should().BeEmpty();
		component.Instance.Rectangles.Should().HaveCount(5);
	}

	/// <summary>
	/// Verifies that double-clicking a leaf raises DoubleClick but does not zoom.
	/// </summary>
	[Fact]
	public async Task Double_clicking_a_leaf_does_not_zoom()
	{
		var zooms = new List<Node?>();
		Node? doubleClicked = null;
		var component = RenderMap(p => p
			.Add(x => x.DoubleClick, n => doubleClicked = n)
			.Add(x => x.ZoomRootChanged, n => zooms.Add(n)));

		await component.InvokeAsync(() => component.FindAll("g.pdtm-node")[IndexOf(component, _b)].DoubleClickAsync(new MouseEventArgs()));

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
	/// Verifies that the breadcrumb path is found through an item shared by two branches, which is visited
	/// once only.
	/// </summary>
	[Fact]
	public void The_breadcrumb_path_is_found_past_an_item_shared_by_two_branches()
	{
		var shared = new Node("Shared", 5);
		var target = new Node("Target", 0, new Node("T1", 5), new Node("T2", 5));
		var root = new Node("Top", 0, new Node("P", 0, shared), new Node("Q", 0, shared, target));

		var component = RenderMap(p => p.Add(x => x.ZoomRoot, target), root);

		component.FindAll(".pdtm-crumb").Select(b => b.TextContent.Trim()).Should().Equal("Top", "Q", "Target");
	}
}
