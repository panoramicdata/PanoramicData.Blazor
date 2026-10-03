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
	/// Verifies that panning to a position that is not one of the defined positions pans to the start.
	/// </summary>
	[Fact]
	public async Task PanTo_UnknownPosition_PansToTheStart()
	{
		var timeline = RenderTimeline();
		await timeline.InvokeAsync(() => timeline.Instance.PanTo(Day(30)));

		await timeline.InvokeAsync(() => timeline.Instance.PanTo(Day(30), (TimelinePositions)99));
		timeline.Render();

		timeline.FindComponents<PDStackedBar>()[0].Instance.DataPoint.StartTime.Should().Be(Day(1));
	}

	/// <summary>
	/// Verifies that panning to a date at the start makes it the first visible column, and a date too near the
	/// end to be first pans as far as the timeline allows (#161).
	/// </summary>
	[Fact]
	public async Task PanTo_Start_MakesTheDateFirst()
	{
		var timeline = RenderTimeline();

		await timeline.InvokeAsync(() => timeline.Instance.PanTo(Day(30), TimelinePositions.Start));
		timeline.Render();
		timeline.FindComponents<PDStackedBar>()[0].Instance.DataPoint.StartTime.Should().Be(Day(30));

		await timeline.InvokeAsync(() => timeline.Instance.PanTo(Day(58), TimelinePositions.Start));
		timeline.Render();
		timeline.FindComponents<PDStackedBar>()[0].Instance.DataPoint.StartTime.Should().Be(Day(41));
	}

	/// <summary>
	/// Verifies that clicking the pan track of a disabled timeline neither pages the viewport nor fetches (#161).
	/// </summary>
	[Fact]
	public async Task PanClick_WhenDisabled_DoesNothing()
	{
		var timeline = RenderTimeline(p => p.Add(x => x.IsEnabled, false));

		await timeline.Find("svg.tl-pan").PointerUpAsync(new PointerEventArgs { ClientX = 310 });

		_queries.Should().BeEmpty();
		timeline.FindComponents<PDStackedBar>()[0].Instance.DataPoint.StartTime.Should().Be(Day(1));
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
