using AwesomeAssertions;
using Bunit;
using Microsoft.AspNetCore.Components.Web;
using PanoramicData.Blazor.Models;

namespace PanoramicData.Blazor.Test.Components;

/// <summary>
/// Zoom, pan and resize tests for <see cref="PDTimeline"/>.
/// </summary>
public partial class PDTimelineTests
{
	/// <summary>
	/// Verifies when zooming in and out is possible: not past either end of the scale list, not when disabled,
	/// and not for a scale that is not in the list.
	/// </summary>
	[Fact]
	public void CanZoom_ReflectsScalePositionAndState()
	{
		var timeline = RenderTimeline();
		timeline.Instance.CanZoomIn().Should().BeTrue();
		timeline.Instance.CanZoomOut().Should().BeTrue();

		timeline.Render(p => p.Add(x => x.Scale, TimelineScale.Seconds));
		timeline.Instance.CanZoomIn().Should().BeFalse();

		timeline.Render(p => p.Add(x => x.Scale, TimelineScale.Years));
		timeline.Instance.CanZoomOut().Should().BeFalse();

		timeline.Render(p => p.Add(x => x.Scale, TimelineScale.Minutes5));
		timeline.Instance.CanZoomIn().Should().BeFalse();
		timeline.Instance.CanZoomOut().Should().BeFalse();

		timeline.Render(p => p.Add(x => x.Scale, TimelineScale.Days).Add(x => x.IsEnabled, false));
		timeline.Instance.CanZoomIn().Should().BeFalse();
		timeline.Instance.CanZoomOut().Should().BeFalse();
	}

	/// <summary>
	/// Verifies that with zoom-out restricted, zooming out is only possible while the wider scale still fills
	/// the viewport, and a refused zoom leaves the scale alone.
	/// </summary>
	[Fact]
	public async Task RestrictZoomOut_RefusesZoomPastTheData()
	{
		var restricted = new TimelineOptions { General = new TimelineGeneralOptions { RestrictZoomOut = true } };
		var timeline = RenderTimeline(p => p.Add(x => x.Options, restricted));

		timeline.Instance.CanZoomOut().Should().BeFalse();
		await timeline.InvokeAsync(() => timeline.Instance.SetScale(TimelineScale.Weeks));
		timeline.Instance.Scale.Name.Should().Be("Days");
		_scaleChanges.Should().BeEmpty();

		_initialized = false;
		_renderMin = new DateTime(2020, 1, 1);
		var longTimeline = RenderTimeline(p => p.Add(x => x.Options, new TimelineOptions { General = new TimelineGeneralOptions { RestrictZoomOut = true } }));
		longTimeline.Instance.CanZoomOut().Should().BeTrue();
	}

	/// <summary>
	/// Verifies that Ctrl with the mouse wheel zooms in and out one scale at a time, and the wheel alone does not.
	/// </summary>
	[Fact]
	public void CtrlWheel_Zooms()
	{
		var timeline = RenderTimeline();
		var root = timeline.Find("div.pd-timeline");

		root.Wheel(new WheelEventArgs { DeltaY = -1 });
		_scaleChanges.Should().BeEmpty();

		timeline.Find("div.pd-timeline").Wheel(new WheelEventArgs { DeltaY = -1, CtrlKey = true });
		timeline.Find("div.pd-timeline").Wheel(new WheelEventArgs { DeltaY = 1, CtrlKey = true });
		timeline.Find("div.pd-timeline").Wheel(new WheelEventArgs { DeltaY = 1, CtrlKey = true });

		_scaleChanges.Should().Equal("12 Hours", "Days", "Weeks");
	}

	/// <summary>
	/// Verifies that the wheel cannot zoom past either end of the scale list.
	/// </summary>
	[Fact]
	public void CtrlWheel_StopsAtTheEnds()
	{
		UseScale(TimelineScale.Seconds);
		_renderMin = new DateTime(2026, 1, 1, 12, 0, 0);
		_renderMax = new DateTime(2026, 1, 1, 12, 5, 0);
		var timeline = RenderTimeline();
		timeline.Find("div.pd-timeline").Wheel(new WheelEventArgs { DeltaY = -1, CtrlKey = true });

		_initialized = false;
		UseScale(TimelineScale.Years);
		_renderMin = new DateTime(2000, 1, 1);
		var years = RenderTimeline();
		years.Find("div.pd-timeline").Wheel(new WheelEventArgs { DeltaY = 1, CtrlKey = true });

		_scaleChanges.Should().BeEmpty();
	}

	/// <summary>
	/// Verifies that the zoom methods step through the scale list and stop at its ends.
	/// </summary>
	[Fact]
	public async Task ZoomInAndOut_StepThroughScales()
	{
		var timeline = RenderTimeline();

		await timeline.InvokeAsync(timeline.Instance.ZoomOutAsync);
		await timeline.InvokeAsync(timeline.Instance.ZoomInAsync);
		await timeline.InvokeAsync(timeline.Instance.ZoomInAsync);
		_scaleChanges.Should().Equal("Weeks", "Days", "12 Hours");

		timeline.Render(p => p.Add(x => x.Scale, TimelineScale.Years));
		_scaleChanges.Clear();
		await timeline.InvokeAsync(timeline.Instance.ZoomOutAsync);
		timeline.Render(p => p.Add(x => x.Scale, TimelineScale.Seconds));
		_scaleChanges.Clear();
		await timeline.InvokeAsync(timeline.Instance.ZoomInAsync);
		_scaleChanges.Should().BeEmpty();
	}

	/// <summary>
	/// Verifies that the scale chosen to fit a range is the finest one that fits in the viewport, falling back
	/// to the first scale when none does.
	/// </summary>
	[Fact]
	public void GetScaleToFit_ChoosesFinestFittingScale()
	{
		var timeline = RenderTimeline();

		timeline.Instance.GetScaleToFit(Day(1), Day(2))!.Name.Should().Be("4 Hours");
		timeline.Instance.GetScaleToFit()!.Name.Should().Be("Weeks");
		timeline.Instance.GetScaleToFit(new DateTime(1900, 1, 1), Day(1))!.Name.Should().Be("Seconds");
	}

	/// <summary>
	/// Verifies that the zoom-to methods switch to the scale that fits their range.
	/// </summary>
	[Fact]
	public async Task ZoomTo_SwitchesToFittingScale()
	{
		var timeline = RenderTimeline();

		await timeline.InvokeAsync(() => timeline.Instance.ZoomToAsync(Day(1), Day(2)));
		await timeline.InvokeAsync(timeline.Instance.ZoomToEndAsync);
		await timeline.InvokeAsync(() => timeline.Instance.ZoomToSelectionAsync());
		await timeline.InvokeAsync(() => timeline.Instance.SetSelection(Day(4), Day(5)));
		await timeline.InvokeAsync(() => timeline.Instance.ZoomToSelectionAsync(true));
		await timeline.InvokeAsync(timeline.Instance.ZoomToStartAsync);

		_scaleChanges.Should().Equal("4 Hours", "Weeks", "4 Hours", "Weeks");
	}

	/// <summary>
	/// Verifies that changing scale keeps the selection in place, snapped to whole periods of the new scale.
	/// </summary>
	[Fact]
	public async Task ScaleChange_SnapsSelectionToNewPeriods()
	{
		var timeline = RenderTimeline();
		await timeline.InvokeAsync(() => timeline.Instance.SetSelection(Day(4), Day(6)));

		await timeline.InvokeAsync(() => timeline.Instance.SetScale(TimelineScale.Weeks));

		timeline.Instance.GetSelection().Should().BeEquivalentTo(Range(Day(4), Day(6)));
		var selection = timeline.Find("svg.tl-plot-area > rect");
		selection.GetAttribute("x").Should().Be("20");
		selection.GetAttribute("width").Should().Be("20");
	}

	/// <summary>
	/// Verifies that dragging the pan handle moves the viewport and fetches the newly visible range.
	/// </summary>
	[Fact]
	public void PanDrag_MovesViewportAndFetches()
	{
		var timeline = RenderTimeline();
		var pan = timeline.Find("svg.tl-pan");

		pan.PointerDown(new PointerEventArgs { ClientX = 50, PointerId = 3 });
		timeline.Find("svg.tl-pan").PointerMove(new PointerEventArgs { ClientX = 110, Buttons = 1 });
		timeline.Find("svg.tl-pan").PointerUp(new PointerEventArgs { ClientX = 110 });

		timeline.FindAll("svg.tl-pan rect")[1].GetAttribute("x").Should().Be("60");
		_queries[^1].Should().Be((Day(10), Day(31), "Days"));
		timeline.FindComponents<PDStackedBar>()[0].Instance.DataPoint.StartTime.Should().Be(Day(10));
	}

	/// <summary>
	/// Verifies that the pan handle cannot be dragged past either end of the track.
	/// </summary>
	[Fact]
	public void PanDrag_IsClampedToTrack()
	{
		var timeline = RenderTimeline();

		timeline.Find("svg.tl-pan").PointerMove(new PointerEventArgs { ClientX = 50, Buttons = 1 });
		timeline.Find("svg.tl-pan").PointerMove(new PointerEventArgs { ClientX = 900, Buttons = 1 });
		var x = double.Parse(timeline.FindAll("svg.tl-pan rect")[1].GetAttribute("x")!, System.Globalization.CultureInfo.InvariantCulture);
		x.Should().BeApproximately(400 - (20.0 / 61 * 400), 0.001);

		timeline.Find("svg.tl-pan").PointerMove(new PointerEventArgs { ClientX = -900, Buttons = 1 });
		timeline.FindAll("svg.tl-pan rect")[1].GetAttribute("x").Should().Be("0");
	}

	/// <summary>
	/// Verifies that clicking the pan track beside the handle pages the viewport that way.
	/// </summary>
	[Fact]
	public void PanClick_PagesViewport()
	{
		var timeline = RenderTimeline();

		timeline.Find("svg.tl-pan").PointerUp(new PointerEventArgs { ClientX = 310 });
		_queries[^1].Start.Should().Be(Day(20));

		timeline.Find("svg.tl-pan").PointerUp(new PointerEventArgs { ClientX = 20 });
		_queries[^1].Start.Should().Be(Day(1));

		var count = _queries.Count;
		timeline.Find("svg.tl-pan").PointerUp(new PointerEventArgs { ClientX = 50 });
		_queries.Should().HaveCount(count);
	}

	/// <summary>
	/// Verifies that when all data is fetched up front, panning does not fetch again.
	/// </summary>
	[Fact]
	public void Pan_WithFetchAll_DoesNotFetch()
	{
		var timeline = RenderTimeline(p => p.Add(x => x.Options, new TimelineOptions { General = new TimelineGeneralOptions { FetchAll = true } }));

		timeline.Find("svg.tl-pan").PointerUp(new PointerEventArgs { ClientX = 310 });

		_queries.Should().BeEmpty();
		timeline.FindComponents<PDStackedBar>()[0].Instance.DataPoint.StartTime.Should().Be(Day(20));
	}

	/// <summary>
	/// Verifies that panning to a date centres it or ends the viewport on it, and a date outside the timeline
	/// pans to the end.
	/// </summary>
	[Fact]
	public void PanTo_PositionsTheDate()
	{
		var timeline = RenderTimeline();

		timeline.InvokeAsync(() => timeline.Instance.PanTo(Day(30)));
		timeline.Render();
		timeline.FindComponents<PDStackedBar>()[0].Instance.DataPoint.StartTime.Should().Be(Day(20));

		timeline.InvokeAsync(() => timeline.Instance.PanTo(Day(30), TimelinePositions.End));
		timeline.Render();
		timeline.FindComponents<PDStackedBar>()[0].Instance.DataPoint.StartTime.Should().Be(Day(11));

		timeline.InvokeAsync(() => timeline.Instance.PanTo(new DateTime(2026, 6, 1), TimelinePositions.End));
		timeline.Render();
		timeline.FindComponents<PDStackedBar>()[0].Instance.DataPoint.StartTime.Should().Be(Day(41));
	}

	/// <summary>
	/// Verifies that a timeline shorter than the viewport always pans to its start.
	/// </summary>
	[Fact]
	public void PanTo_OnShortTimeline_StaysAtStart()
	{
		_renderMax = Day(10);
		var timeline = RenderTimeline();

		timeline.InvokeAsync(() => timeline.Instance.PanTo(Day(8), TimelinePositions.End));
		timeline.Render();

		timeline.FindComponents<PDStackedBar>()[0].Instance.DataPoint.StartTime.Should().Be(Day(1));
	}

	/// <summary>
	/// Verifies that a resize re-measures the canvas and re-lays-out the viewport, re-fetching by default.
	/// </summary>
	[Fact]
	public async Task Resize_RelaysOutAndRefetches()
	{
		var timeline = RenderTimeline(p => p.Add(x => x.Options, new TimelineOptions { General = new TimelineGeneralOptions { FetchAll = true } }));

		SetCanvas(200);
		await timeline.InvokeAsync(timeline.Instance.OnResize);

		timeline.FindComponents<PDStackedBar>().Should().HaveCount(10);
		_queries.Should().ContainSingle();
	}

	/// <summary>
	/// Verifies that with re-fetching on resize turned off an unchanged range is not fetched again.
	/// </summary>
	[Fact]
	public async Task Resize_WithoutRefetch_KeepsData()
	{
		var timeline = RenderTimeline(p => p
			.Add(x => x.RefetchDataOnResize, false)
			.Add(x => x.Options, new TimelineOptions { General = new TimelineGeneralOptions { FetchAll = true } }));

		SetCanvas(200);
		await timeline.InvokeAsync(timeline.Instance.OnResize);

		timeline.FindComponents<PDStackedBar>().Should().HaveCount(10);
		_queries.Should().BeEmpty();
	}

	/// <summary>
	/// Verifies that setting the dates moves the rounded bounds.
	/// </summary>
	[Fact]
	public void SetDates_MovesBounds()
	{
		var timeline = RenderTimeline();

		timeline.Instance.SetDates(Day(5), Day(9));

		timeline.Instance.MinDateTime.Should().Be(Day(5));
		timeline.Instance.RoundedMaxDateTime.Should().Be(Day(10));
	}
}
