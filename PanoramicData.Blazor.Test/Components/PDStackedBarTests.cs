using AwesomeAssertions;
using Bunit;
using PanoramicData.Blazor.Models;

namespace PanoramicData.Blazor.Test.Components;

/// <summary>
/// Tests that <see cref="PDStackedBar"/> stacks one rectangle per positive series value, colours them from
/// the timeline options, and describes the point in its tooltip.
/// </summary>
public class PDStackedBarTests : BunitContext
{
	private static readonly DateTime _start = new(2026, 3, 4, 5, 6, 0);

	/// <summary>Sets up the rendering context.</summary>
	public PDStackedBarTests() => JSInterop.Mode = JSRuntimeMode.Loose;

	private static TimelineOptions TwoSeriesOptions() => new()
	{
		Bar = new TimelineBarOptions { Width = 20, Padding = 3 },
		Series =
		[
			new TimelineSeries { Label = "Alpha", Colour = "red", Format = "0" },
			new TimelineSeries { Label = "Beta", Colour = "blue", Format = "0.0" }
		]
	};

	/// <summary>
	/// Verifies that values are scaled against the maximum, stacked bottom-up, and that a zero value draws nothing.
	/// </summary>
	[Fact]
	public void Bars_AreScaledAndStacked_SkippingZeroValues()
	{
		var component = Render<PDStackedBar>(parameters => parameters
			.Add(p => p.Options, TwoSeriesOptions())
			.Add(p => p.Height, 50)
			.Add(p => p.MaxValue, 100)
			.Add(p => p.X, 40)
			.Add(p => p.DataPoint, new DataPoint { StartTime = _start, Count = 7, SeriesValues = [10, 0, 30] }));

		var svg = component.Find("svg.tl-stacked-bar");
		svg.GetAttribute("x").Should().Be("40");
		svg.GetAttribute("width").Should().Be("20");
		svg.GetAttribute("height").Should().Be("50");
		svg.ClassList.Should().NotContain("disabled");

		var rects = component.FindAll("rect");
		rects.Should().HaveCount(2);
		rects[0].GetAttribute("height").Should().Be("5");
		rects[0].GetAttribute("y").Should().Be("45");
		rects[0].GetAttribute("x").Should().Be("3");
		rects[0].GetAttribute("width").Should().Be("14");
		rects[0].GetAttribute("fill").Should().Be("red");
		rects[1].GetAttribute("height").Should().Be("15");
		rects[1].GetAttribute("y").Should().Be("30");

		// The third value has no configured series, so it falls back to black.
		rects[1].GetAttribute("fill").Should().Be("black");
	}

	/// <summary>
	/// Verifies that the Y-value transform is applied before scaling.
	/// </summary>
	[Fact]
	public void YValueTransform_IsApplied()
	{
		var component = Render<PDStackedBar>(parameters => parameters
			.Add(p => p.Height, 100)
			.Add(p => p.MaxValue, 100)
			.Add(p => p.YValueTransform, v => v * 2)
			.Add(p => p.DataPoint, new DataPoint { StartTime = _start, SeriesValues = [10] }));

		component.Find("rect").GetAttribute("height").Should().Be("20");
	}

	/// <summary>
	/// Verifies that the tooltip lists the start time, each series value in its own format, and the count.
	/// </summary>
	[Fact]
	public void Title_DescribesThePoint()
	{
		var component = Render<PDStackedBar>(parameters => parameters
			.Add(p => p.Options, TwoSeriesOptions())
			.Add(p => p.DateFormat, "yyyy-MM-dd HH:mm")
			.Add(p => p.DataPoint, new DataPoint { StartTime = _start, Count = 7, SeriesValues = [10, 2.5] }));

		var lines = component.Find("title").TextContent
			.Split('\n', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);

		lines.Should().Equal("2026-03-04 05:06", "Alpha: 10", "Beta: 2.5", $"{DataPoint.CountLabel}: 7");
	}

	/// <summary>
	/// Verifies that a point with no series values draws no bars and its tooltip has just the time and count.
	/// </summary>
	[Fact]
	public void NoSeriesValues_DrawsNothing_AndTitleOmitsSeries()
	{
		var component = Render<PDStackedBar>(parameters => parameters
			.Add(p => p.Options, TwoSeriesOptions())
			.Add(p => p.DataPoint, new DataPoint { StartTime = _start, Count = 3 }));

		component.FindAll("rect").Should().BeEmpty();
		var lines = component.Find("title").TextContent
			.Split('\n', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
		lines.Should().Equal("04/03/26 05:06", $"{DataPoint.CountLabel}: 3");
	}

	/// <summary>
	/// Verifies that a null data point draws nothing and leaves the tooltip empty.
	/// </summary>
	[Fact]
	public void NullDataPoint_DrawsNothing()
	{
		var component = Render<PDStackedBar>(parameters => parameters
			.Add(p => p.DataPoint, null!));

		component.FindAll("rect").Should().BeEmpty();
		component.Find("title").TextContent.Should().BeEmpty();
	}

	/// <summary>
	/// Verifies that the enable/disable methods toggle the disabled class.
	/// </summary>
	[Fact]
	public async Task EnableDisable_ToggleDisabledClass()
	{
		var component = Render<PDStackedBar>(parameters => parameters
			.Add(p => p.IsEnabled, false));
		component.Find("svg").ClassList.Should().Contain("disabled");

		await component.InvokeAsync(component.Instance.Enable);
		component.Find("svg").ClassList.Should().NotContain("disabled");
		component.Instance.IsEnabled.Should().BeTrue();

		await component.InvokeAsync(component.Instance.Disable);
		component.Find("svg").ClassList.Should().Contain("disabled");

		await component.InvokeAsync(() => component.Instance.SetEnabled(true));
		component.Find("svg").ClassList.Should().NotContain("disabled");
	}
}
