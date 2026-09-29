using AwesomeAssertions;
using Bunit;

namespace PanoramicData.Blazor.Test.Components;

/// <summary>
/// Tests for <see cref="PDProgressBar"/>: the percentage it reports and the bar it draws from it.
/// </summary>
public class PDProgressBarTests : BunitContext
{
	/// <summary>
	/// Verifies that the percentage is the value as a share of the total, rounded to the requested decimal places.
	/// </summary>
	[Theory]
	[InlineData(25, 100, 0, 25)]
	[InlineData(1, 3, 0, 33)]
	[InlineData(1, 3, 2, 33.33)]
	[InlineData(50, 200, 1, 25)]
	public void GetPercentage_IsTheValueAsAShareOfTheTotal(double value, double total, ushort decimalPlaces, double expected)
	{
		var component = Render<PDProgressBar>(parameters => parameters
			.Add(p => p.Value, value)
			.Add(p => p.Total, total)
			.Add(p => p.DecimalPlaces, decimalPlaces));

		component.Instance.GetPercentage().Should().Be(expected);
	}

	/// <summary>
	/// Verifies that a zero total reports zero rather than dividing by zero.
	/// </summary>
	[Fact]
	public void GetPercentage_WithAZeroTotal_IsZero()
	{
		var component = Render<PDProgressBar>(parameters => parameters
			.Add(p => p.Value, 10)
			.Add(p => p.Total, 0));

		component.Instance.GetPercentage().Should().Be(0);
		component.Find(".bar").GetAttribute("style").Should().Contain("width:0%");
	}

	/// <summary>
	/// Verifies that the bar is drawn at the configured height and at the width of the percentage.
	/// </summary>
	[Fact]
	public void Bar_IsDrawnAtTheHeightAndPercentageWidth()
	{
		var component = Render<PDProgressBar>(parameters => parameters
			.Add(p => p.Value, 40)
			.Add(p => p.Height, "10px"));

		var style = component.Find(".pd-progressbar .bar").GetAttribute("style");
		style.Should().Contain("height: 10px").And.Contain("width:40%");
	}

	/// <summary>
	/// Verifies that bar content is rendered inside the bar and receives the progress bar itself.
	/// </summary>
	[Fact]
	public void BarContent_IsRenderedWithTheProgressBar()
	{
		var component = Render<PDProgressBar>(parameters => parameters
			.Add(p => p.Value, 75)
			.Add(p => p.BarContent, bar => builder => builder.AddContent(0, $"{bar.GetPercentage()} percent")));

		component.Find(".bar").TextContent.Should().Contain("75 percent");
	}

	/// <summary>
	/// Verifies that without bar content the bar is empty.
	/// </summary>
	[Fact]
	public void Bar_WithoutBarContent_IsEmpty()
	{
		var component = Render<PDProgressBar>(parameters => parameters.Add(p => p.Value, 75));

		component.Find(".bar").TextContent.Trim().Should().BeEmpty();
	}
}
