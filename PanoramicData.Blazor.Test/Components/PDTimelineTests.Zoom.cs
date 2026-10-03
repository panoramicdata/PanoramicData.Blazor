using AwesomeAssertions;
using Bunit;
using Microsoft.AspNetCore.Components.Web;
using PanoramicData.Blazor.Models;

namespace PanoramicData.Blazor.Test.Components;

/// <summary>
/// Zoom and scale tests for <see cref="PDTimeline"/>.
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
	/// Verifies that setting the scale with a focus date centres the viewport on that date.
	/// </summary>
	[Fact]
	public async Task SetScale_WithFocusDate_CentresOnIt()
	{
		var timeline = RenderTimeline();

		await timeline.InvokeAsync(() => timeline.Instance.SetScale(TimelineScale.Days, true, Day(30)));

		timeline.FindComponents<PDStackedBar>()[0].Instance.DataPoint.StartTime.Should().Be(Day(20));
		_queries[^1].Should().Be((Day(20), Day(41), "Days"));
	}

	/// <summary>
	/// Verifies that the scale chosen to fit from a date alone fits the range from that date to the end.
	/// </summary>
	[Fact]
	public void GetScaleToFit_FromDate_FitsToTheEnd()
	{
		var timeline = RenderTimeline();

		var scale = timeline.Instance.GetScaleToFit(Day(50));

		scale.Should().BeSameAs(timeline.Instance.GetScaleToFit(Day(50), timeline.Instance.RoundedMaxDateTime));
		scale!.Name.Should().NotBe(timeline.Instance.GetScaleToFit()!.Name);
	}

	/// <summary>
	/// Verifies that laying out at the given scale, or at the default one, does not raise
	/// <see cref="PDTimeline.ScaleChanged"/>, while a real change still does (#161).
	/// </summary>
	[Fact]
	public async Task FirstLayout_DoesNotRaiseScaleChanged()
	{
		var timeline = RenderTimeline();
		_scaleChanges.Should().BeEmpty();
		await timeline.InvokeAsync(timeline.Instance.ZoomOutAsync);
		_scaleChanges.Should().Equal("Weeks");

		_initialized = false;
		_scaleChanges.Clear();
		Render<PDTimeline>(parameters => parameters
			.Add(p => p.MinDateTime, new DateTime(2000, 1, 1))
			.Add(p => p.MaxDateTime, _max)
			.Add(p => p.Initialized, () => _initialized = true)
			.Add(p => p.ScaleChanged, (TimelineScale s) => _scaleChanges.Add(s.Name)))
			.WaitForState(() => _initialized, _wait);
		_scaleChanges.Should().BeEmpty();
	}

	/// <summary>
	/// Verifies that without auto-refresh, where the timeline never lays itself out, the first scale change
	/// asked for is still raised.
	/// </summary>
	[Fact]
	public async Task WithoutAutoRefresh_FirstZoomRaisesScaleChanged()
	{
		var manual = new TimelineOptions { General = new TimelineGeneralOptions { AutoRefresh = false } };
		var timeline = RenderTimeline(p => p.Add(x => x.Options, manual));

		await timeline.InvokeAsync(timeline.Instance.ZoomOutAsync);

		_scaleChanges.Should().Equal("Weeks");
	}
}
