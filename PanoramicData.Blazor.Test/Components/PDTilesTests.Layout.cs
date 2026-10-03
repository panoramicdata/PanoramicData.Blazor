using AwesomeAssertions;
using Bunit;
using PanoramicData.Blazor.Models.Tiles;

namespace PanoramicData.Blazor.Test.Components;

/// <summary>
/// Alignment and view box tests for <see cref="PDTiles"/>.
/// </summary>
public partial class PDTilesTests
{
	/// <summary>
	/// Alignment decides both the SVG aspect-ratio anchor and where the grid sits inside a viewBox widened and
	/// heightened by the maximum-size percentages.
	/// </summary>
	[Theory]
	[InlineData(GridAlignment.TopLeft, "xMinYMin meet", "translate(-88, -96)")]
	[InlineData(GridAlignment.TopCenter, "xMidYMin meet", "translate(24, -96)")]
	[InlineData(GridAlignment.TopRight, "xMaxYMin meet", "translate(136, -96)")]
	[InlineData(GridAlignment.MiddleLeft, "xMinYMid meet", "translate(-88, -37)")]
	[InlineData(GridAlignment.MiddleCenter, "xMidYMid meet", "translate(24, -37)")]
	[InlineData(GridAlignment.MiddleRight, "xMaxYMid meet", "translate(136, -37)")]
	[InlineData(GridAlignment.BottomLeft, "xMinYMax meet", "translate(-88, 22)")]
	[InlineData(GridAlignment.BottomCenter, "xMidYMax meet", "translate(24, 22)")]
	[InlineData(GridAlignment.BottomRight, "xMaxYMax meet", "translate(136, 22)")]
	public void Alignment_PositionsGridWithinExpandedViewBox(GridAlignment alignment, string preserveAspectRatio, string transform)
	{
		var options = UnitGrid(alignment);
		options.MaxGridWidthPercent = 50;
		options.MaxGridHeightPercent = 50;

		var cut = RenderTiles(options);

		var svg = cut.Find("svg");
		svg.GetAttribute("viewBox").Should().Be("0 0 448 236");
		svg.GetAttribute("preserveAspectRatio").Should().Be(preserveAspectRatio);
		cut.Find(TileSelector).GetAttribute("transform").Should().Be(transform);
	}

	/// <summary>Without size limits the viewBox is exactly the grid, and a 100% limit is treated as no limit.</summary>
	[Fact]
	public void ViewBox_WithoutEffectiveLimits_IsTheGridSize()
	{
		var options = UnitGrid();
		options.MaxGridWidthPercent = 100;

		var cut = RenderTiles(options);

		cut.Find("svg").GetAttribute("viewBox").Should().Be("0 0 224 118");
		cut.Find(TileSelector).GetAttribute("transform").Should().Be("translate(-88, -96)");
	}

	/// <summary>Scale and padding shrink the grid within a larger viewBox.</summary>
	[Fact]
	public void ViewBox_ScaleAndPadding_EnlargeTheViewBox()
	{
		var options = UnitGrid();
		options.Scale = 50;
		options.Padding = 50;

		var cut = RenderTiles(options);

		cut.Find("svg").GetAttribute("viewBox").Should().Be("0 0 896 472");
	}

	/// <summary>Depth adds the tile's side height to the grid, and gap spreads tiles apart.</summary>
	[Fact]
	public void ViewBox_DepthAndGap_AreIncludedInTheGridSize()
	{
		var options = UnitGrid();
		options.Columns = 2;
		options.Depth = 25;
		options.Gap = 100;

		var cut = RenderTiles(options);

		// two tiles at one diagonal step of 224 x 118, plus a 56 pixel side
		cut.Find("svg").GetAttribute("viewBox").Should().Be("0 0 448 292");
	}
}
