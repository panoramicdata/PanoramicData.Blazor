using AwesomeAssertions;
using Bunit;
using PanoramicData.Blazor.Models;

namespace PanoramicData.Blazor.Test.Components;

/// <summary>
/// Layout, data and state rendering tests for <see cref="PDTimeline"/>.
/// </summary>
public partial class PDTimelineTests
{
	/// <summary>
	/// Verifies that after first render the timeline initialises its module, measures the canvas, fetches just
	/// the visible columns and draws one bar per column.
	/// </summary>
	[Fact]
	public void FirstRender_InitialisesAndFetchesViewport()
	{
		_data.Add(new DataPoint { StartTime = Day(2), Count = 4, SeriesValues = [3] });
		var timeline = Render<PDTimeline>(parameters => parameters
			.Add(p => p.Id, "tl-1")
			.Add(p => p.Scale, TimelineScale.Days)
			.Add(p => p.MinDateTime, _min)
			.Add(p => p.MaxDateTime, _max)
			.Add(p => p.DataProvider, Provide)
			.Add(p => p.Refreshed, () => _refreshes++)
			.Add(p => p.Initialized, () => _initialized = true));

		timeline.WaitForState(() => _initialized, _wait);

		// initialize(id, options, ref): the object reference is the third argument, and there is no fourth
		var initialize = _module.VerifyInvoke("initialize");
		initialize.Arguments.Should().HaveCount(3);
		initialize.Arguments[0].Should().Be("tl-1");
		initialize.Arguments[2].Should().BeOfType<Microsoft.JSInterop.DotNetObjectReference<PDTimeline>>();
		_queries[^1].Should().Be((Day(1), Day(22), "Days"));
		_refreshes.Should().BePositive();
		var bars = timeline.FindComponents<PDStackedBar>();
		bars.Should().HaveCount(20);
		bars[1].Instance.DataPoint.Count.Should().Be(4);
		bars[1].Instance.X.Should().Be(20);
		bars[1].Instance.MaxValue.Should().Be(3);
		bars[0].Instance.DataPoint.StartTime.Should().Be(Day(1));
		timeline.Find("div.pd-timeline").Id.Should().Be("tl-1");
	}

	/// <summary>
	/// Verifies that a configured Y-axis maximum overrides the maximum found in the data, and that the Y-value
	/// transform is used when finding it.
	/// </summary>
	[Fact]
	public void MaxValue_ComesFromOptionsOrTransformedData()
	{
		_data.Add(new DataPoint { StartTime = Day(1), SeriesValues = [2, 3] });

		var transformed = RenderTimeline(p => p.Add(x => x.YValueTransform, v => v * 10));
		transformed.FindComponents<PDStackedBar>()[0].Instance.MaxValue.Should().Be(50);

		_initialized = false;
		var fixedMax = RenderTimeline(p => p.Add(x => x.Options, new TimelineOptions { YAxis = new TimelineYAxisOptions { MaxValue = 99 } }));
		fixedMax.FindComponents<PDStackedBar>()[0].Instance.MaxValue.Should().Be(99);
	}

	/// <summary>
	/// Verifies that columns before <see cref="PDTimeline.DisableBefore"/> or ending after
	/// <see cref="PDTimeline.DisableAfter"/> are drawn disabled.
	/// </summary>
	[Fact]
	public void DisabledRanges_DisableTheirBars()
	{
		var timeline = RenderTimeline(p => p
			.Add(x => x.DisableBefore, Day(3))
			.Add(x => x.DisableAfter, Day(18)));

		var enabled = timeline.FindComponents<PDStackedBar>().Select(b => b.Instance.IsEnabled).ToList();
		enabled.Take(2).Should().AllBeEquivalentTo(false);
		enabled.Skip(2).Take(15).Should().AllBeEquivalentTo(true);
		enabled.Skip(17).Should().AllBeEquivalentTo(false);
	}

	/// <summary>
	/// Verifies the X axis: a major tick labelled with the month on the first of the month, and minor labels with
	/// the day of the month for the other columns.
	/// </summary>
	[Fact]
	public void XAxis_LabelsMajorAndMinorTicks()
	{
		var timeline = RenderTimeline();

		var axis = timeline.Find("svg.tl-x-axis");
		axis.QuerySelectorAll("text.minor-tick").Select(t => t.TextContent.Trim()).Take(3).Should().Equal("01", "02", "03");
		axis.QuerySelectorAll("text:not(.minor-tick)").Select(t => t.TextContent.Trim()).Should().Equal("2026-01");
		axis.QuerySelectorAll("line[y2='18']").Should().ContainSingle();
		axis.QuerySelectorAll("rect title").Should().HaveCount(20);
	}

	/// <summary>
	/// Verifies that the pan handle is sized to the share of the timeline in view.
	/// </summary>
	[Fact]
	public void PanHandle_IsSizedToViewportShare()
	{
		var timeline = RenderTimeline();

		var handle = timeline.FindAll("svg.tl-pan rect")[1];
		double.Parse(handle.GetAttribute("width")!, System.Globalization.CultureInfo.InvariantCulture)
			.Should().BeApproximately(20.0 / 61 * 400, 0.001);
		handle.GetAttribute("x").Should().Be("0");
	}

	/// <summary>
	/// Verifies that a disabled timeline is styled disabled and draws no pan handle, and the enable methods
	/// restore it.
	/// </summary>
	[Fact]
	public async Task Disabled_IsStyledAndEnableRestores()
	{
		var timeline = RenderTimeline(p => p.Add(x => x.IsEnabled, false));

		timeline.Find("div.pd-timeline").ClassList.Should().Contain("disabled");
		timeline.Find("svg.tl-x-axis line").GetAttribute("stroke").Should().Be("DarkGray");
		timeline.Find("svg.tl-pan").Children.Should().BeEmpty();

		await timeline.InvokeAsync(timeline.Instance.Enable);
		timeline.Find("div.pd-timeline").ClassList.Should().NotContain("disabled");
		timeline.FindAll("svg.tl-pan rect").Should().HaveCount(2);

		await timeline.InvokeAsync(timeline.Instance.Disable);
		timeline.Instance.IsEnabled.Should().BeFalse();
		await timeline.InvokeAsync(() => timeline.Instance.SetEnabled(true));
		timeline.Instance.IsEnabled.Should().BeTrue();
	}

	/// <summary>
	/// Verifies that the new-data indicators are drawn when announced and raise their callbacks when pressed.
	/// </summary>
	[Fact]
	public void Indicators_RaiseUpdateCallbacks()
	{
		var minUpdates = 0;
		var maxUpdates = 0;
		var timeline = RenderTimeline(p => p
			.Add(x => x.NewMinDateTimeAvailable, true)
			.Add(x => x.NewMaxDateTimeAvailable, true)
			.Add(x => x.UpdateMinDate, () => minUpdates++)
			.Add(x => x.UpdateMaxDate, () => maxUpdates++));

		timeline.Find("svg.tl-ind-l").PointerDown();
		timeline.Find("svg.tl-ind-r").PointerDown();

		minUpdates.Should().Be(1);
		maxUpdates.Should().Be(1);
		timeline.Find("svg.tl-ind-r rect").GetAttribute("x").Should().Be("380");
	}

	/// <summary>
	/// Verifies that a spinner is shown while a fetch is outstanding, and removed when it completes.
	/// </summary>
	[Fact]
	public async Task Spinner_ShownWhileLoading()
	{
		var pending = new TaskCompletionSource<DataPoint[]>();
		var timeline = RenderTimeline();
		timeline.Render(p => p.Add(x => x.DataProvider, (_, _, _, ct) =>
		{
			ct.ThrowIfCancellationRequested();
			return new ValueTask<DataPoint[]>(pending.Task);
		}));

		var refresh = timeline.InvokeAsync(() => timeline.Instance.RefreshAsync(true));
		timeline.Render();
		timeline.FindAll("path.rotate").Should().ContainSingle();

		pending.SetResult([]);
		await refresh;
		timeline.Render();
		timeline.FindAll("path.rotate").Should().BeEmpty();
	}
}
