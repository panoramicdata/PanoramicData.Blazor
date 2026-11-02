using AwesomeAssertions;
using Bunit;
using PanoramicData.Blazor.Models;

namespace PanoramicData.Blazor.Test.Components;

/// <summary>
/// Tests for forced refreshes replacing cached bars, and for the Y-axis maximum label and gridlines.
/// </summary>
public partial class PDTimelineTests
{
	private static TimelineOptions YAxis(TimelineYAxisOptions yAxis) => new() { YAxis = yAxis };

	/// <summary>
	/// Verifies that a forced refresh draws the provider's new values, including a period it no longer returns,
	/// rather than the cached ones: a consumer filtering its data must see the chart follow.
	/// </summary>
	[Fact]
	public async Task ForcedRefresh_ReplacesCachedBars_IncludingPeriodsNowEmpty()
	{
		_data.Add(new DataPoint { StartTime = Day(2), Count = 5, SeriesValues = [5] });
		_data.Add(new DataPoint { StartTime = Day(3), Count = 2, SeriesValues = [2] });
		var timeline = RenderTimeline();
		timeline.FindComponents<PDStackedBar>()[1].Instance.DataPoint.Count.Should().Be(5);

		_data.Clear();
		_data.Add(new DataPoint { StartTime = Day(3), Count = 1, SeriesValues = [1] });
		await timeline.InvokeAsync(() => timeline.Instance.RefreshAsync(true));

		var bars = timeline.FindComponents<PDStackedBar>();
		bars[1].Instance.DataPoint.Count.Should().Be(0, "the provider no longer returns day 2");
		bars[2].Instance.DataPoint.Count.Should().Be(1);
		bars[2].Instance.MaxValue.Should().Be(1);
	}

	/// <summary>
	/// Verifies that an unforced refresh of an unchanged range still keeps the cache, so it fetches nothing.
	/// </summary>
	[Fact]
	public async Task UnforcedRefresh_OfTheSameRange_KeepsTheCache()
	{
		_data.Add(new DataPoint { StartTime = Day(2), Count = 5, SeriesValues = [5] });
		var timeline = RenderTimeline();

		_data.Clear();
		await timeline.InvokeAsync(() => timeline.Instance.RefreshAsync());

		_queries.Should().BeEmpty();
		timeline.FindComponents<PDStackedBar>()[1].Instance.DataPoint.Count.Should().Be(5);
	}

	/// <summary>
	/// Verifies that the maximum label and gridlines are off by default, so existing timelines are unchanged.
	/// </summary>
	[Fact]
	public void YAxisExtras_AreOffByDefault()
	{
		_data.Add(new DataPoint { StartTime = Day(2), SeriesValues = [8] });

		var timeline = RenderTimeline();

		timeline.FindAll(".tl-max-value").Should().BeEmpty();
		timeline.FindAll("line.tl-grid-line").Should().BeEmpty();
	}

	/// <summary>
	/// Verifies that the maximum label shows the untransformed total of the tallest bar.
	/// </summary>
	[Fact]
	public void ShowMaxValue_LabelsTheTallestBarsTotal()
	{
		_data.Add(new DataPoint { StartTime = Day(2), SeriesValues = [3, 4] });
		_data.Add(new DataPoint { StartTime = Day(3), SeriesValues = [9] });

		var timeline = RenderTimeline(p => p
			.Add(x => x.Options, YAxis(new TimelineYAxisOptions { ShowMaxValue = true }))
			.Add(x => x.YValueTransform, Math.Sqrt));

		// sqrt(3) + sqrt(4) = 3.73 is taller than sqrt(9) = 3, so the label is that bar's total, 7
		timeline.Find(".tl-max-value text").TextContent.Should().Be("7");
	}

	/// <summary>
	/// Verifies that the maximum label shows a configured maximum, which is what the top of the plot then means.
	/// </summary>
	[Fact]
	public void ShowMaxValue_ShowsTheConfiguredMaximum()
	{
		_data.Add(new DataPoint { StartTime = Day(2), SeriesValues = [3] });

		var timeline = RenderTimeline(p => p.Add(x => x.Options, YAxis(new TimelineYAxisOptions { ShowMaxValue = true, MaxValue = 50 })));

		timeline.Find(".tl-max-value text").TextContent.Should().Be("50");
	}

	/// <summary>
	/// Verifies that an empty chart draws no maximum label or gridlines, rather than a zero or a division by zero.
	/// </summary>
	[Fact]
	public void EmptyChart_DrawsNoLabelOrGridLines()
	{
		var timeline = RenderTimeline(p => p.Add(x => x.Options, YAxis(new TimelineYAxisOptions { ShowMaxValue = true, ShowGridLines = true })));

		timeline.FindAll(".tl-max-value").Should().BeEmpty();
		timeline.FindAll("line.tl-grid-line").Should().BeEmpty();
	}

	/// <summary>
	/// Verifies that gridlines sit at round values, placed on the same scale as the bars, in the configured colour.
	/// </summary>
	[Fact]
	public void ShowGridLines_DrawsLinesAtRoundValuesOnTheBarScale()
	{
		_data.Add(new DataPoint { StartTime = Day(2), SeriesValues = [8] });

		var timeline = RenderTimeline(p => p.Add(x => x.Options, YAxis(new TimelineYAxisOptions { ShowGridLines = true, GridLineColour = "pink" })));

		// a maximum of 8 in 4 lines is a step of 2: 2, 4 and 6, on a 100-pixel plot
		var lines = timeline.FindAll("line.tl-grid-line");
		lines.Select(l => l.GetAttribute("y1")).Should().Equal("75", "50", "25");
		lines.Should().AllSatisfy(l => l.GetAttribute("stroke").Should().Be("pink"));
		lines[0].GetAttribute("x2").Should().Be("400");
	}

	/// <summary>
	/// Verifies the round gridline steps, and that nothing is returned for a maximum or count that is not positive.
	/// </summary>
	[Theory]
	[InlineData(8, 4, new double[] { 2, 4, 6 })]
	[InlineData(3, 4, new double[] { 1, 2 })]
	[InlineData(23, 4, new double[] { 10, 20 })]
	[InlineData(100, 4, new double[] { 50 })]
	[InlineData(100, 10, new double[] { 10, 20, 30, 40, 50, 60, 70, 80, 90 })]
	[InlineData(0.9, 3, new double[] { 0.5 })]
	[InlineData(0, 4, new double[0])]
	[InlineData(-5, 4, new double[0])]
	[InlineData(10, 0, new double[0])]
	public void GridLineValues_UseRoundSteps(double max, int maxLines, double[] expected)
		=> PDTimeline.Utilities.GridLineValues(max, maxLines).Should().Equal(expected);
}
