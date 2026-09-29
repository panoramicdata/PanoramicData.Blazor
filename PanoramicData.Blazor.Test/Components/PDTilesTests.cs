using AwesomeAssertions;
using Bunit;
using PanoramicData.Blazor.Models.Tiles;

namespace PanoramicData.Blazor.Test.Components;

/// <summary>
/// Tests that <see cref="PDTiles"/> lays out its isometric grid, applies grid and per-tile options, renders
/// connectors in every connection mode and fill pattern, raises click events, and generates random
/// connectors that respect the connector options.
/// </summary>
public partial class PDTilesTests : BunitContext
{
	private const string Id = "t";
	private const string TileSelector = "g[data-tile-id]";

	/// <summary>Sets up the rendering context.</summary>
	public PDTilesTests() => JSInterop.Mode = JSRuntimeMode.Loose;

	/// <summary>A one-by-one grid with no depth, gap or padding, so its geometry is easy to state exactly.</summary>
	private static TileGridOptions UnitGrid(GridAlignment alignment = GridAlignment.MiddleCenter) => new()
	{
		Columns = 1,
		Rows = 1,
		Depth = 0,
		Gap = 0,
		Padding = 0,
		Alignment = alignment
	};

	/// <summary>Connector options with animation off, so markup does not change between renders.</summary>
	private static TileConnectorOptions StillConnectors(ConnectionMode mode = ConnectionMode.StraightLine) => new()
	{
		ConnectionMode = mode,
		AnimationSpeed = 0
	};

	private IRenderedComponent<PDTiles> RenderTiles(
		TileGridOptions options,
		List<string>? logos = null,
		List<TileDefinition>? tiles = null)
		=> Render<PDTiles>(p => p
			.Add(x => x.Id, Id)
			.Add(x => x.Options, options)
			.Add(x => x.ConnectorOptions, StillConnectors())
			.Add(x => x.Logos, logos ?? ["tiles/Alpha Logo.svg"])
			.Add(x => x.Tiles, tiles));

	private IRenderedComponent<PDTiles> RenderWithConnectors(
		TileConnectorOptions connectorOptions,
		params TileConnector[] connectors)
		=> Render<PDTiles>(p => p
			.Add(x => x.Id, Id)
			.Add(x => x.Options, new TileGridOptions { Columns = 2, Rows = 2, Depth = 20 })
			.Add(x => x.ConnectorOptions, connectorOptions)
			.Add(x => x.Logos, ["tiles/Alpha Logo.svg"])
			.Add(x => x.Connectors, [.. connectors]));

	private static TileConnector Connector(int startColumn, int startRow, int endColumn, int endRow, string direction,
		ConnectorFillPattern pattern = ConnectorFillPattern.Solid)
		=> new()
		{
			StartTile = new TileCoordinate { Column = startColumn, Row = startRow },
			EndTile = new TileCoordinate { Column = endColumn, Row = endRow },
			Direction = direction,
			FillPattern = pattern
		};

	#region Container and layout

	/// <summary>The root element carries the id, CSS class and sizing parameters.</summary>
	[Fact]
	public void Root_ReflectsIdClassAndSize()
	{
		var cut = Render<PDTiles>(p => p
			.Add(x => x.Id, "my-tiles")
			.Add(x => x.CssClass, "extra")
			.Add(x => x.Width, "50%")
			.Add(x => x.Height, "300px")
			.Add(x => x.Style, "border: 1px"));

		var root = cut.Find("div.pd-tiles");
		root.Id.Should().Be("my-tiles");
		root.ClassList.Should().Contain("extra");
		root.GetAttribute("style").Should().Contain("width: 50%").And.Contain("height: 300px").And.Contain("border: 1px");
	}

	/// <summary>A component given no id generates a unique one.</summary>
	[Fact]
	public void Id_WhenNotSupplied_IsGeneratedAndUnique()
	{
		var first = Render<PDTiles>().Find("div.pd-tiles").Id;
		var second = Render<PDTiles>().Find("div.pd-tiles").Id;

		first.Should().StartWith("pd-tiles-");
		second.Should().StartWith("pd-tiles-").And.NotBe(first);
	}

	/// <summary>The default grid is three by three, with every tile shown.</summary>
	[Fact]
	public void Defaults_RenderNineVisibleTiles()
	{
		var cut = Render<PDTiles>();

		var tiles = cut.FindAll(TileSelector);
		tiles.Should().HaveCount(9);
		tiles.Should().OnlyContain(t => t.GetAttribute("style")!.Contains("opacity: 1", StringComparison.Ordinal));
	}

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

	#endregion

	#region Background layers and colours

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

	#endregion
}
