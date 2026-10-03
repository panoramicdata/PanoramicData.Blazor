using AwesomeAssertions;
using Bunit;
using Microsoft.AspNetCore.Components.Web;
using PanoramicData.Blazor.Models;

namespace PanoramicData.Blazor.Test.Components;

/// <summary>
/// Tests for how selection in <see cref="PDTimeline"/> respects disabled ranges and disabled selection.
/// </summary>
public partial class PDTimelineTests
{
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
	/// Verifies that a selection reaching into the disabled start of the timeline, when that is allowed, is
	/// pushed out of it by a shift-drag but still stops at the last column.
	/// </summary>
	[Fact]
	public async Task ShiftDrag_OutOfDisabledStart_StopsAtTheEnd()
	{
		var options = new TimelineOptions { General = new TimelineGeneralOptions { AllowDisableSelection = true } };
		var timeline = RenderTimeline(p => p
			.Add(x => x.Options, options)
			.Add(x => x.DisableBefore, Day(41)));
		await timeline.InvokeAsync(() => timeline.Instance.SetSelection(Day(21), Day(52)));

		DragColumns(timeline, 45, 44, shift: true);

		_selections[^1].Should().BeEquivalentTo(Range(Day(30), new DateTime(2026, 3, 2)));
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
	/// Verifies that with chart selection turned off a set selection can still be resized by its start handle,
	/// and its end is not then forced to the end of the timeline.
	/// </summary>
	[Fact]
	public async Task SelectionDisabled_StartHandle_KeepsTheEnd()
	{
		var options = new TimelineOptions { Selection = new TimelineSelectionOptions { Enabled = false, CanChangeEnd = false } };
		var timeline = RenderTimeline(p => p.Add(x => x.Options, options));
		await timeline.InvokeAsync(() => timeline.Instance.SetSelection(Day(3), Day(6)));
		timeline.Render();

		var handle = timeline.FindAll("rect.tl-selection-handle").Should().ContainSingle().Subject;
		handle.PointerDown(new PointerEventArgs { ClientX = ColumnX(2), PointerId = 2 });
		timeline.Find("rect.tl-selection-handle").PointerMove(new PointerEventArgs { ClientX = ColumnX(1) });
		timeline.Find("rect.tl-selection-handle").PointerUp(new PointerEventArgs { ClientX = ColumnX(1) });

		_selections[^1].Should().BeEquivalentTo(Range(Day(2), Day(6)));
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
}
