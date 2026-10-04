using AwesomeAssertions;
using Bunit;

namespace PanoramicData.Blazor.Test;

/// <summary>
/// Keyboard navigation tests for <see cref="PDTreeMap{TItem}"/>.
/// </summary>
public partial class PDTreeMapTests
{
	/// <summary>
	/// Verifies that when fewer rectangles are laid out the focus moves to the last one that remains.
	/// </summary>
	[Fact]
	public async Task The_focus_is_kept_within_fewer_rectangles()
	{
		var component = RenderMap();
		await PressKeyAsync(component, "End");
		component.FindAll("g.pdtm-node")[^1].ClassList.Should().Contain("pdtm-node-focused");

		component.Render(p => p.Add(x => x.Root, new Node("Small", 0, new Node("S1", 10), new Node("S2", 5))));

		var nodes = component.FindAll("g.pdtm-node");
		nodes.Should().HaveCount(2);
		nodes[1].ClassList.Should().Contain("pdtm-node-focused");
		nodes[0].ClassList.Should().NotContain("pdtm-node-focused");
	}

	/// <summary>
	/// Verifies that arrow, Home and End keys move the focus and selection through the rectangles.
	/// </summary>
	[Fact]
	public async Task Arrow_home_and_end_keys_move_the_selection()
	{
		var selections = 0;
		var component = RenderMap(p => p.Add(x => x.SelectionChanged, () => selections++));
		var rects = component.Instance.Rectangles;

		await PressKeyAsync(component, "ArrowRight");
		component.Instance.Selection.Should().BeSameAs(rects[0].Item);
		await PressKeyAsync(component, "ArrowDown");
		component.Instance.Selection.Should().BeSameAs(rects[1].Item);
		await PressKeyAsync(component, "ArrowUp");
		component.Instance.Selection.Should().BeSameAs(rects[0].Item);
		await PressKeyAsync(component, "End");
		component.Instance.Selection.Should().BeSameAs(rects[^1].Item);
		await PressKeyAsync(component, "ArrowLeft");
		component.Instance.Selection.Should().BeSameAs(rects[^2].Item);
		await PressKeyAsync(component, "Home");
		component.Instance.Selection.Should().BeSameAs(rects[0].Item);

		selections.Should().Be(6);
		component.FindAll("g.pdtm-node")[0].ClassList.Should().Contain("pdtm-node-focused");
	}

	/// <summary>
	/// Verifies that the arrow keys stop at the first and last rectangles.
	/// </summary>
	[Fact]
	public async Task Arrow_keys_stop_at_the_ends()
	{
		var component = RenderMap();
		var rects = component.Instance.Rectangles;

		await PressKeyAsync(component, "ArrowLeft");
		component.Instance.Selection.Should().BeSameAs(rects[0].Item);

		await PressKeyAsync(component, "End");
		await PressKeyAsync(component, "ArrowRight");
		component.Instance.Selection.Should().BeSameAs(rects[^1].Item);
	}

	/// <summary>
	/// Verifies that Enter clicks and zooms into the focused branch, and Backspace zooms back out.
	/// </summary>
	[Fact]
	public async Task Enter_zooms_into_the_focused_branch_and_backspace_zooms_out()
	{
		var zooms = new List<Node?>();
		var component = RenderMap(p => p.Add(x => x.ZoomRootChanged, n => zooms.Add(n)));
		var index = IndexOf(component, _a);
		for (var i = 0; i <= index; i++)
		{
			await PressKeyAsync(component, "ArrowRight");
		}

		await PressKeyAsync(component, "Enter");
		await PressKeyAsync(component, "Backspace");

		zooms.Should().Equal(_a, null);
	}

	/// <summary>
	/// Verifies that Enter with nothing focused, Escape at the root, and other keys do nothing.
	/// </summary>
	[Fact]
	public async Task Keys_with_nothing_to_act_on_do_nothing()
	{
		var events = new List<string>();
		var component = RenderMap(p => p
			.Add(x => x.SelectionChanged, () => events.Add("selection"))
			.Add(x => x.ZoomRootChanged, _ => events.Add("zoom")));

		await PressKeyAsync(component, "Enter");
		await PressKeyAsync(component, " ");
		await PressKeyAsync(component, "Escape");
		await PressKeyAsync(component, "x");

		events.Should().BeEmpty();
		component.Instance.Selection.Should().BeNull();
	}
}
