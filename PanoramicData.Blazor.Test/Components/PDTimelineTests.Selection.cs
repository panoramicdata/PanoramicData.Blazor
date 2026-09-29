using AwesomeAssertions;
using Bunit;
using Microsoft.AspNetCore.Components.Web;
using PanoramicData.Blazor.Models;

namespace PanoramicData.Blazor.Test.Components;

/// <summary>
/// Pointer and programmatic selection tests for <see cref="PDTimeline"/>.
/// </summary>
public partial class PDTimelineTests
{
	/// <summary>
	/// Verifies that a click selects its single column, draws the selection with both handles, and ends the
	/// change; a second click on the same column changes nothing.
	/// </summary>
	[Fact]
	public void Click_SelectsSingleColumn()
	{
		var timeline = RenderTimeline();

		ClickColumn(timeline, 3);

		_selections.Should().ContainSingle().Which.Should().BeEquivalentTo(Range(Day(4), Day(5)));
		_selectionChangeEnds.Should().Be(1);
		timeline.Instance.GetSelection().Should().BeEquivalentTo(Range(Day(4), Day(5)));
		var selection = timeline.Find("svg.tl-plot-area > rect");
		selection.GetAttribute("x").Should().Be("60");
		selection.GetAttribute("width").Should().Be("20");
		timeline.FindAll("rect.tl-selection-handle").Should().HaveCount(2);

		ClickColumn(timeline, 3);
		_selections.Should().ContainSingle();
		_selectionChangeEnds.Should().Be(1);
	}

	/// <summary>
	/// Verifies that a drag selects the columns it covers in either direction, and that a movement under the drag
	/// threshold is still a click.
	/// </summary>
	[Fact]
	public void Drag_SelectsColumnsInEitherDirection()
	{
		var timeline = RenderTimeline();

		DragColumns(timeline, 2, 6);
		_selections[^1].Should().BeEquivalentTo(Range(Day(3), Day(8)));

		DragColumns(timeline, 9, 5);
		_selections[^1].Should().BeEquivalentTo(Range(Day(6), Day(11)));

		Plot(timeline).PointerDown(new PointerEventArgs { ClientX = ColumnX(12), PointerId = 1 });
		Plot(timeline).PointerMove(new PointerEventArgs { ClientX = ColumnX(12) + 3, PointerId = 1 });
		Plot(timeline).PointerUp(new PointerEventArgs { ClientX = ColumnX(12) + 3, PointerId = 1 });
		_selections[^1].Should().BeEquivalentTo(Range(Day(13), Day(14)));
		_selectionChangeEnds.Should().Be(3);
	}

	/// <summary>
	/// Verifies that a shift-drag starting inside the selection moves the whole selection, keeping its width.
	/// </summary>
	[Fact]
	public void ShiftDrag_MovesSelection()
	{
		var timeline = RenderTimeline();
		DragColumns(timeline, 2, 4);

		DragColumns(timeline, 3, 5, shift: true);

		_selections[^1].Should().BeEquivalentTo(Range(Day(5), Day(8)));
		timeline.Instance.IsPointInSelection(ColumnX(4)).Should().BeTrue();
		timeline.Instance.IsPointInSelection(ColumnX(7)).Should().BeFalse();
	}

	/// <summary>
	/// Verifies that a shift-drag cannot move the selection before the first column or after the last.
	/// </summary>
	[Fact]
	public void ShiftDrag_IsClampedToTheTimeline()
	{
		var timeline = RenderTimeline();
		DragColumns(timeline, 2, 4);

		DragColumns(timeline, 3, 0, shift: true);
		DragColumns(timeline, 1, -5, shift: true);
		_selections[^1].Should().BeEquivalentTo(Range(Day(1), Day(4)));

		DragColumns(timeline, 1, 70, shift: true);
		_selections[^1].Should().BeEquivalentTo(Range(Day(58), new DateTime(2026, 3, 2)));
	}

	/// <summary>
	/// Verifies that a shift-drag cannot move the selection into the disabled ranges.
	/// </summary>
	[Fact]
	public void ShiftDrag_StaysOutOfDisabledRanges()
	{
		var timeline = RenderTimeline(p => p
			.Add(x => x.DisableBefore, Day(3))
			.Add(x => x.DisableAfter, Day(20)));
		DragColumns(timeline, 4, 6);

		DragColumns(timeline, 5, 1, shift: true);
		_selections[^1].Should().BeEquivalentTo(Range(Day(3), Day(6)));

		DragColumns(timeline, 3, 19, shift: true);
		_selections[^1].Should().BeEquivalentTo(Range(Day(17), Day(20)));
	}

	/// <summary>
	/// Verifies that a shift-click outside the selection starts a new selection instead of moving it.
	/// </summary>
	[Fact]
	public void ShiftClick_OutsideSelection_StartsNewSelection()
	{
		var timeline = RenderTimeline();
		DragColumns(timeline, 2, 4);

		ClickColumn(timeline, 10, shift: true);

		_selections[^1].Should().BeEquivalentTo(Range(Day(11), Day(12)));
	}

	/// <summary>
	/// Verifies that the end handle extends or shrinks the selection but never past its start, and the start
	/// handle does the same from the other side.
	/// </summary>
	[Fact]
	public void Handles_ResizeSelection()
	{
		var timeline = RenderTimeline();
		DragColumns(timeline, 2, 4);
		var ends = _selectionChangeEnds;

		var endHandle = timeline.FindAll("rect.tl-selection-handle")[1];
		endHandle.PointerDown(new PointerEventArgs { ClientX = ColumnX(4), PointerId = 2 });
		timeline.FindAll("rect.tl-selection-handle")[1].PointerMove(new PointerEventArgs { ClientX = ColumnX(1) });
		timeline.FindAll("rect.tl-selection-handle")[1].PointerMove(new PointerEventArgs { ClientX = ColumnX(8) });
		timeline.FindAll("rect.tl-selection-handle")[1].PointerUp(new PointerEventArgs { ClientX = ColumnX(8) });
		_selections[^1].Should().BeEquivalentTo(Range(Day(3), Day(10)));

		var startHandle = timeline.FindAll("rect.tl-selection-handle")[0];
		startHandle.PointerDown(new PointerEventArgs { ClientX = ColumnX(2), PointerId = 2 });
		timeline.FindAll("rect.tl-selection-handle")[0].PointerMove(new PointerEventArgs { ClientX = ColumnX(9) });
		timeline.FindAll("rect.tl-selection-handle")[0].PointerMove(new PointerEventArgs { ClientX = ColumnX(0) });
		timeline.FindAll("rect.tl-selection-handle")[0].PointerUp(new PointerEventArgs { ClientX = ColumnX(0) });
		_selections[^1].Should().BeEquivalentTo(Range(Day(1), Day(10)));

		_selectionChangeEnds.Should().Be(ends + 2);
	}

	/// <summary>
	/// Verifies that a disabled selection option, or a disabled timeline, ignores clicks on the chart.
	/// </summary>
	[Fact]
	public void SelectionDisabled_IgnoresClicks()
	{
		var off = RenderTimeline(p => p.Add(x => x.Options, new TimelineOptions { Selection = new TimelineSelectionOptions { Enabled = false } }));
		ClickColumn(off, 3);

		_initialized = false;
		var disabled = RenderTimeline(p => p.Add(x => x.IsEnabled, false));
		ClickColumn(disabled, 3);

		_selections.Should().BeEmpty();
		_selectionChangeEnds.Should().Be(0);
	}

	/// <summary>
	/// Verifies that a click on a disabled column, before <see cref="PDTimeline.DisableBefore"/> or from
	/// <see cref="PDTimeline.DisableAfter"/>, selects nothing.
	/// </summary>
	[Fact]
	public void ClickOnDisabledColumn_SelectsNothing()
	{
		var timeline = RenderTimeline(p => p
			.Add(x => x.DisableBefore, Day(3))
			.Add(x => x.DisableAfter, Day(10)));

		ClickColumn(timeline, 1);
		ClickColumn(timeline, 9);

		_selections.Should().BeEmpty();
	}

	/// <summary>
	/// Verifies that when only the start can change a click selects from there to the end of the timeline, with
	/// only the start handle drawn.
	/// </summary>
	[Fact]
	public void StartOnlySelection_RunsToTheEnd()
	{
		var timeline = RenderTimeline(p => p.Add(x => x.Options, new TimelineOptions { Selection = new TimelineSelectionOptions { CanChangeEnd = false } }));

		ClickColumn(timeline, 3);

		_selections[^1].Should().BeEquivalentTo(Range(Day(4), new DateTime(2026, 3, 2)));
		timeline.FindAll("rect.tl-selection-handle").Should().ContainSingle()
			.Which.GetAttribute("x").Should().Be("60");
	}

	/// <summary>
	/// Verifies that when only the end can change a click selects from the start of the timeline, with only the
	/// end handle drawn.
	/// </summary>
	[Fact]
	public void EndOnlySelection_RunsFromTheStart()
	{
		var timeline = RenderTimeline(p => p.Add(x => x.Options, new TimelineOptions { Selection = new TimelineSelectionOptions { CanChangeStart = false } }));

		ClickColumn(timeline, 3);

		_selections[^1].Should().BeEquivalentTo(Range(Day(1), Day(5)));
		timeline.FindAll("rect.tl-selection-handle").Should().ContainSingle()
			.Which.GetAttribute("x").Should().Be("76");
	}

	/// <summary>
	/// Verifies that a right-aligned timeline shorter than the viewport is padded on the left, and a start-only
	/// selection then runs to the right-hand edge.
	/// </summary>
	[Fact]
	public void RightAligned_ShortTimeline_PadsLeftAndSelectsToEdge()
	{
		_renderMax = Day(10);
		var options = new TimelineOptions
		{
			General = new TimelineGeneralOptions { RightAlign = true },
			Selection = new TimelineSelectionOptions { CanChangeEnd = false }
		};
		var timeline = RenderTimeline(p => p.Add(x => x.Options, options));

		timeline.Instance.RoundedMinDateTime.Should().Be(new DateTime(2025, 12, 22));
		ClickColumn(timeline, 15);

		_selections[^1].Should().BeEquivalentTo(Range(Day(6), Day(11)));
	}

	/// <summary>
	/// Verifies that when only the start can change on a short timeline that is not right-aligned, the selection
	/// runs to the last real column.
	/// </summary>
	[Fact]
	public void StartOnlySelection_OnShortTimeline_RunsToLastColumn()
	{
		_renderMax = Day(10);
		var timeline = RenderTimeline(p => p.Add(x => x.Options, new TimelineOptions { Selection = new TimelineSelectionOptions { CanChangeEnd = false } }));

		ClickColumn(timeline, 5);

		_selections[^1].Should().BeEquivalentTo(Range(Day(6), Day(11)));
	}

	/// <summary>
	/// Verifies that a drag into a disabled range is cut short at its boundary, from either direction and from
	/// the start handle.
	/// </summary>
	[Fact]
	public void Drag_IsClippedToEnabledRange()
	{
		var timeline = RenderTimeline(p => p
			.Add(x => x.DisableBefore, Day(5))
			.Add(x => x.DisableAfter, Day(12)));

		DragColumns(timeline, 6, 15);
		_selections[^1].Should().BeEquivalentTo(Range(Day(7), Day(12)));

		DragColumns(timeline, 8, 1);
		_selections[^1].Should().BeEquivalentTo(Range(Day(5), Day(10)));

		DragColumns(timeline, 6, 8);
		timeline.FindAll("rect.tl-selection-handle")[0].PointerDown(new PointerEventArgs { ClientX = ColumnX(6) });
		timeline.FindAll("rect.tl-selection-handle")[0].PointerMove(new PointerEventArgs { ClientX = ColumnX(2) });
		_selections[^1].Should().BeEquivalentTo(Range(Day(5), Day(10)));
	}

	/// <summary>
	/// Verifies that when selecting disabled ranges is allowed a drag is not clipped.
	/// </summary>
	[Fact]
	public void Drag_WithDisabledSelectionAllowed_IsNotClipped()
	{
		var options = new TimelineOptions { General = new TimelineGeneralOptions { AllowDisableSelection = true } };
		var timeline = RenderTimeline(p => p
			.Add(x => x.Options, options)
			.Add(x => x.DisableAfter, Day(12)));

		DragColumns(timeline, 6, 15);

		_selections[^1].Should().BeEquivalentTo(Range(Day(7), Day(17)));
	}

	/// <summary>
	/// Verifies that a single-column selection that straddles <see cref="PDTimeline.DisableAfter"/> is cut at it.
	/// </summary>
	[Fact]
	public void Click_OnPartlyDisabledColumn_IsClipped()
	{
		var timeline = RenderTimeline(p => p.Add(x => x.DisableAfter, Day(5).AddHours(12)));

		ClickColumn(timeline, 4);

		_selections[^1].Should().BeEquivalentTo(Range(Day(5), Day(5).AddHours(12)));
	}

	/// <summary>
	/// Verifies that setting a selection raises the change once, draws it, and clamps it to the timeline's end.
	/// </summary>
	[Fact]
	public async Task SetSelection_RaisesOnceAndClampsToEnd()
	{
		var timeline = RenderTimeline();

		await timeline.InvokeAsync(() => timeline.Instance.SetSelection(Day(3), Day(6)));
		await timeline.InvokeAsync(() => timeline.Instance.SetSelection(Day(3), Day(6)));
		_selections.Should().ContainSingle().Which.Should().BeEquivalentTo(Range(Day(3), Day(6)));
		timeline.Render();
		timeline.Find("svg.tl-plot-area > rect").GetAttribute("x").Should().Be("40");
		timeline.Find("svg.tl-plot-area > rect").GetAttribute("width").Should().Be("60");

		await timeline.InvokeAsync(() => timeline.Instance.SetSelection(Day(3), new DateTime(2026, 4, 1)));
		_selections[^1].Should().BeEquivalentTo(Range(Day(3), new DateTime(2026, 3, 2)));
	}

	/// <summary>
	/// Verifies that a set selection starting before the timeline is clamped to the timeline's start, not moved
	/// past its own end (#161).
	/// </summary>
	[Fact]
	public async Task SetSelection_BeforeTheStart_ClampsToTheStart()
	{
		var timeline = RenderTimeline();

		await timeline.InvokeAsync(() => timeline.Instance.SetSelection(new DateTime(2025, 12, 1), Day(5)));

		_selections[^1].Should().BeEquivalentTo(Range(Day(1), Day(5)));
		timeline.Instance.GetSelection().Should().BeEquivalentTo(Range(Day(1), Day(5)));
	}

	/// <summary>
	/// Verifies that a set selection is clipped to the enabled range unless selecting disabled ranges is allowed.
	/// </summary>
	[Fact]
	public async Task SetSelection_IsClippedToEnabledRange()
	{
		var timeline = RenderTimeline(p => p
			.Add(x => x.DisableBefore, Day(5))
			.Add(x => x.DisableAfter, Day(20)));

		await timeline.InvokeAsync(() => timeline.Instance.SetSelection(Day(2), Day(30)));
		_selections[^1].Should().BeEquivalentTo(Range(Day(5), Day(20)));

		_initialized = false;
		var options = new TimelineOptions { General = new TimelineGeneralOptions { AllowDisableSelection = true } };
		var unclipped = RenderTimeline(p => p
			.Add(x => x.Options, options)
			.Add(x => x.DisableAfter, Day(20)));
		await unclipped.InvokeAsync(() => unclipped.Instance.SetSelection(Day(2), Day(30)));
		_selections[^1].Should().BeEquivalentTo(Range(Day(2), Day(30)));
	}

	/// <summary>
	/// Verifies that <see cref="PDTimeline.IsPointInSelection"/> is false with no selection.
	/// </summary>
	[Fact]
	public void IsPointInSelection_WithoutSelection_IsFalse()
	{
		var timeline = RenderTimeline();

		timeline.Instance.IsPointInSelection(ColumnX(3)).Should().BeFalse();
	}
}
