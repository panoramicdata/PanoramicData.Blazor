using AwesomeAssertions;
using Bunit;

namespace PanoramicData.Blazor.Test.Components;

/// <summary>
/// Background layer and colour tests for <see cref="PDTiles"/>.
/// </summary>
public partial class PDTilesTests
{
	/// <summary>The background colour is applied to the SVG only when the background is shown.</summary>
	[Theory]
	[InlineData(true, true)]
	[InlineData(false, false)]
	public void ShowBackground_ControlsSvgBackground(bool showBackground, bool expectBackground)
	{
		var options = UnitGrid();
		options.ShowBackground = showBackground;
		options.BackgroundColor = "#123456";

		var cut = RenderTiles(options);

		cut.Find("svg").GetAttribute("style")!.Contains("background-color: #123456", StringComparison.Ordinal)
			.Should().Be(expectBackground);
	}

	/// <summary>A perspective tilts the tile container by a fifth of a degree per unit; zero adds no transform.</summary>
	[Theory]
	[InlineData(0, "")]
	[InlineData(50, "transform: perspective(1000px) rotateX(10deg);")]
	public void Perspective_TiltsTheTileContainer(int perspective, string expectedStyle)
	{
		var options = UnitGrid();
		options.Perspective = perspective;

		var cut = RenderTiles(options);

		(cut.Find("g.tiles-container").GetAttribute("style") ?? string.Empty).Should().Be(expectedStyle);
	}

	/// <summary>Grid lines are drawn in the configured colour and opacity, and omitted at zero opacity.</summary>
	[Fact]
	public void GridLines_UseConfiguredColour_AndAreOmittedAtZeroOpacity()
	{
		var options = UnitGrid();
		options.LineColor = "#FF0000";
		options.LineOpacity = 50;
		var lines = RenderTiles(options).FindAll("g.grid-lines line");

		lines.Should().NotBeEmpty();
		lines.Should().OnlyContain(l => l.GetAttribute("stroke") == "rgba(255, 0, 0, 0.5)");

		var hidden = UnitGrid();
		hidden.LineOpacity = 0;
		RenderTiles(hidden).FindAll("g.grid-lines").Should().BeEmpty();
	}

	/// <summary>The background glow is drawn at the configured strength, and omitted when it is zero.</summary>
	[Fact]
	public void Glow_IsDrawnAtConfiguredStrength_AndOmittedAtZero()
	{
		var options = UnitGrid();
		options.Glow = 40;
		var glow = RenderTiles(options).Find($"rect[fill='url(#{Id}-bgGlow-abs)']");

		glow.GetAttribute("opacity").Should().Be("0.4");

		var none = UnitGrid();
		none.Glow = 0;
		RenderTiles(none).FindAll($"rect[fill='url(#{Id}-bgGlow-abs)']").Should().BeEmpty();
	}

	/// <summary>The face gradients are derived from the tile colour: a quarter, a half, the colour, and 1.4 times it.</summary>
	[Fact]
	public void TileColour_DrivesTheFaceGradients()
	{
		var options = UnitGrid();
		options.TileColor = "#808080";

		var cut = RenderTiles(options);

		var top = cut.Find($"linearGradient#{Id}-topGrad").QuerySelectorAll("stop").Select(s => s.GetAttribute("style"));
		top.Should().Equal("stop-color:#B3B3B3", "stop-color:#808080");
		var front = cut.Find($"linearGradient#{Id}-frontGrad").QuerySelectorAll("stop").Select(s => s.GetAttribute("style"));
		front.Should().Equal("stop-color:#202020", "stop-color:#202020", "stop-color:#404040", "stop-color:#404040", "stop-color:#808080");
	}

	/// <summary>A light tile colour is clamped at white rather than overflowing when lightened.</summary>
	[Fact]
	public void TileColour_Light_IsClampedWhenLightened()
	{
		var options = UnitGrid();
		options.TileColor = "#FFFFFF";

		var cut = RenderTiles(options);

		cut.Find($"linearGradient#{Id}-topGrad stop").GetAttribute("style").Should().Be("stop-color:#FFFFFF");
	}
}
