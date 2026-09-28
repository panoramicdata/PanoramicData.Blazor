using AngleSharp.Dom;
using AwesomeAssertions;
using Bunit;
using PanoramicData.Blazor.Models.Tiles;

namespace PanoramicData.Blazor.Test.Components;

/// <summary>
/// Tests that <see cref="PDTiles"/> lays out its isometric grid, applies grid and per-tile options, renders
/// connectors in every connection mode and fill pattern, raises click events, and generates random
/// connectors that respect the connector options.
/// </summary>
public class PDTilesTests : BunitContext
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

	#region Tiles

	/// <summary>Population decides how many tiles are shown; hidden tiles are transparent and ignore the pointer.</summary>
	[Theory]
	[InlineData(0, 0)]
	[InlineData(50, 2)]
	[InlineData(60, 3)]
	[InlineData(100, 4)]
	public void Population_ControlsHowManyTilesAreVisible(int population, int expectedVisible)
	{
		var options = new TileGridOptions { Columns = 2, Rows = 2, Population = population };

		var tiles = RenderTiles(options).FindAll(TileSelector);

		tiles.Should().HaveCount(4);
		tiles.Count(t => t.GetAttribute("style") == "opacity: 1; pointer-events: auto;").Should().Be(expectedVisible);
		tiles.Count(t => t.GetAttribute("style") == "opacity: 0; pointer-events: none;").Should().Be(4 - expectedVisible);
	}

	/// <summary>The tile name is the logo file name without the folder, the "Logo" suffix or the extension.</summary>
	[Theory]
	[InlineData("tiles/Alpha Logo.svg", "Alpha")]
	[InlineData("tiles/BetaLogo.svg", "Beta")]
	[InlineData("gamma.svg", "gamma")]
	[InlineData("", "Unknown")]
	public void TileName_IsDerivedFromTheLogoPath(string logo, string expectedName)
	{
		var tile = RenderTiles(UnitGrid(), logos: [logo]).Find(TileSelector);

		tile.GetAttribute("data-tile-name").Should().Be(expectedName);
		tile.QuerySelectorAll("image").Length.Should().Be(logo.Length == 0 ? 0 : 1);
	}

	/// <summary>Each tile shows one of the configured logos at the default size and rotation.</summary>
	[Fact]
	public void Logos_AreAssignedFromTheConfiguredList()
	{
		List<string> logos = ["tiles/A Logo.svg", "tiles/B Logo.svg", "tiles/C Logo.svg"];

		var cut = RenderTiles(new TileGridOptions { Columns = 3, Rows = 1 }, logos: logos);

		var hrefs = cut.FindAll("image").Select(i => i.GetAttribute("href")).ToList();
		hrefs.Should().BeEquivalentTo(logos);
		cut.Find("g[transform*='matrix']").GetAttribute("transform").Should().EndWith("scale(0.528) rotate(0)");
	}

	/// <summary>Reflections are drawn with a mask when enabled and omitted when the reflection is zero.</summary>
	[Theory]
	[InlineData(75, 1)]
	[InlineData(0, 0)]
	public void Reflection_IsDrawnOnlyWhenEnabled(int reflection, int expectedMasks)
	{
		var options = UnitGrid();
		options.Depth = 20;
		options.Reflection = reflection;

		var cut = RenderTiles(options);

		cut.FindAll("mask").Should().HaveCount(expectedMasks);
	}

	/// <summary>Per-tile overrides change that tile's colour, logo, size, rotation and reflection and leave other tiles alone.</summary>
	[Fact]
	public void TileOverrides_ApplyOnlyToTheirTile()
	{
		var overrides = new List<TileDefinition>
		{
			new() { Column = 0, Row = 0, Color = "#FF0000", Logo = "tiles/Custom Logo.svg", Depth = 30, LogoSize = 40, LogoRotation = 45, Reflection = 0 }
		};

		var cut = RenderTiles(new TileGridOptions { Columns = 2, Rows = 1 }, tiles: overrides);

		cut.FindAll($"linearGradient#{Id}-topGrad-FF0000").Should().ContainSingle();
		var custom = cut.Find($"g#{Id}-tile-0");
		custom.GetAttribute("data-tile-name").Should().Be("Custom");
		custom.QuerySelector("image")!.GetAttribute("href").Should().Be("tiles/Custom Logo.svg");
		custom.QuerySelector("g[transform*='matrix']")!.GetAttribute("transform").Should().EndWith("scale(0.264) rotate(45)");
		custom.QuerySelectorAll("mask").Should().BeEmpty();
		TopFaceFill(custom).Should().Be($"url(#{Id}-topGrad-FF0000)");

		var plain = cut.Find($"g#{Id}-tile-1");
		plain.QuerySelectorAll("mask").Should().ContainSingle();
		TopFaceFill(plain).Should().Be($"url(#{Id}-topGrad-373737)");
	}

	private static string? TopFaceFill(IElement tile)
		=> tile.QuerySelectorAll("path").Single(p => p.GetAttribute("filter") == $"url(#{Id}-glow)").GetAttribute("fill");

	/// <summary>A per-tile visibility override hides that tile whatever the population says.</summary>
	[Fact]
	public void TileOverride_Visible_False_HidesThatTile()
	{
		var overrides = new List<TileDefinition> { new() { Column = 1, Row = 0, Visible = false } };

		var tiles = RenderTiles(new TileGridOptions { Columns = 2, Rows = 1 }, tiles: overrides).FindAll(TileSelector);

		tiles[0].GetAttribute("style").Should().StartWith("opacity: 1");
		tiles[1].GetAttribute("style").Should().StartWith("opacity: 0");
	}

	/// <summary>Clicking a tile raises <see cref="PDTiles.TileClick"/> with its position, name and override definition.</summary>
	[Fact]
	public void TileClick_RaisesEventWithTileDetails()
	{
		var definition = new TileDefinition { Column = 1, Row = 0, Tag = "tag" };
		var clicks = new List<TileClickEventArgs>();
		var cut = Render<PDTiles>(p => p
			.Add(x => x.Id, Id)
			.Add(x => x.Options, new TileGridOptions { Columns = 2, Rows = 1 })
			.Add(x => x.Logos, ["tiles/Alpha Logo.svg"])
			.Add(x => x.Tiles, [definition])
			.Add(x => x.TileClick, args => clicks.Add(args)));

		cut.Find($"g#{Id}-tile-1").Click();
		cut.Find($"g#{Id}-tile-0").Click();

		clicks.Should().HaveCount(2);
		clicks[0].TileId.Should().Be(1);
		clicks[0].Column.Should().Be(1);
		clicks[0].Row.Should().Be(0);
		clicks[0].TileName.Should().Be("Alpha");
		clicks[0].Tile.Should().BeSameAs(definition);
		clicks[1].Tile.Should().BeNull();
	}

	/// <summary><see cref="PDTiles.Shuffle"/> reorders the logos across tiles without adding or losing any.</summary>
	[Fact]
	public async Task Shuffle_KeepsTheSameLogos()
	{
		List<string> logos = ["tiles/A Logo.svg", "tiles/B Logo.svg", "tiles/C Logo.svg", "tiles/D Logo.svg"];
		var cut = RenderTiles(new TileGridOptions { Columns = 4, Rows = 1 }, logos: logos);

		await cut.InvokeAsync(cut.Instance.Shuffle);

		cut.FindAll("image").Select(i => i.GetAttribute("href")).Should().BeEquivalentTo(logos);
	}

	/// <summary>Re-rendering with an unchanged grid keeps the logo assignment; changing the grid size lays it out again.</summary>
	[Fact]
	public void Rerender_KeepsAssignmentUntilTheGridChanges()
	{
		var logos = Enumerable.Range(0, 12).Select(i => $"tiles/L{i} Logo.svg").ToList();
		var options = new TileGridOptions { Columns = 3, Rows = 3 };
		var cut = RenderTiles(options, logos: logos);
		var before = TileNames(cut);

		cut.Render(p => p.Add(x => x.CssClass, "changed"));
		TileNames(cut).Should().Equal(before);

		cut.Render(p => p.Add(x => x.Options, new TileGridOptions { Columns = 4, Rows = 3 }));
		cut.FindAll(TileSelector).Should().HaveCount(12);
	}

	private static List<string?> TileNames(IRenderedComponent<PDTiles> cut)
		=> [.. cut.FindAll(TileSelector).Select(t => t.GetAttribute("data-tile-name"))];

	#endregion

	#region Child content

	/// <summary>Overlaid child content is rendered above a full-size SVG background layer.</summary>
	[Fact]
	public void ChildContent_Overlay_RendersAboveTheGrid()
	{
		var cut = Render<PDTiles>(p => p
			.Add(x => x.Options, UnitGrid())
			.AddChildContent("<p class=\"hello\">Hello</p>"));

		cut.Find(".pd-tiles-content p.hello").TextContent.Should().Be("Hello");
		cut.FindAll(".pd-tiles-content-wrap").Should().BeEmpty();
		cut.Find("svg").ParentElement!.GetAttribute("style").Should().StartWith("position: absolute;");
	}

	/// <summary>Without child content the SVG simply fills its container.</summary>
	[Fact]
	public void NoChildContent_SvgFillsContainer()
	{
		var cut = RenderTiles(UnitGrid());

		cut.Find("svg").ParentElement!.GetAttribute("style").Should().Be("width: 100%; height: 100%;");
		cut.FindAll(".pd-tiles-content, .pd-tiles-content-wrap").Should().BeEmpty();
	}

	/// <summary>
	/// Wrapped child content flows around a float spacer on the grid's side, and without an explicit width limit
	/// the grid is held to half the width.
	/// </summary>
	[Theory]
	[InlineData(GridAlignment.MiddleLeft, null, "float: left; width: 50%; height: 100%;", "0 0 448 118")]
	[InlineData(GridAlignment.MiddleRight, null, "float: right; width: 50%; height: 100%;", "0 0 448 118")]
	[InlineData(GridAlignment.TopCenter, 25, "float: right; width: 25%; height: 100%;", "0 0 896 118")]
	public void ChildContent_Wrapping_FloatsAroundTheGrid(GridAlignment alignment, int? maxWidth, string spacerStyle, string viewBox)
	{
		var options = UnitGrid(alignment);
		options.ContentWrapping = true;
		options.MaxGridWidthPercent = maxWidth;

		var cut = Render<PDTiles>(p => p
			.Add(x => x.Options, options)
			.AddChildContent("<p>Text</p>"));

		cut.Find(".pd-tiles-content-wrap .pd-tiles-float-spacer").GetAttribute("style").Should().Be(spacerStyle);
		cut.Find(".pd-tiles-content-wrap p").TextContent.Should().Be("Text");
		cut.Find("svg").GetAttribute("viewBox").Should().Be(viewBox);
	}

	#endregion


	#region Connectors

	private static IElement ConnectorElement(IRenderedComponent<PDTiles> cut) => cut.Find("g.connector");

	/// <summary>A straight connector is named after its end tiles and edge, and drawn with two edge lines.</summary>
	[Fact]
	public void StraightConnector_IsNamedAndDrawnWithEdgeLines()
	{
		var connector = Connector(0, 0, 1, 0, "right");
		connector.EdgeIndex = 2;
		connector.Color = "#112233";

		var element = ConnectorElement(RenderWithConnectors(StillConnectors(), connector));

		element.GetAttribute("data-connector-name").Should().Be("Alpha?Alpha#2");
		element.QuerySelectorAll("line").Should().HaveCount(2)
			.And.OnlyContain(l => l.GetAttribute("stroke") == "#112233" && l.GetAttribute("stroke-opacity") == "0.8");
	}

	/// <summary>Each fill pattern adds its own layer to a straight connector: nothing extra, a solid fill, bars or chevrons.</summary>
	[Theory]
	[InlineData(ConnectorFillPattern.Random, 2, 0, 0)]
	[InlineData(ConnectorFillPattern.Solid, 3, 0, 0)]
	[InlineData(ConnectorFillPattern.Bars, 2, 1, 4)]
	[InlineData(ConnectorFillPattern.Chevrons, 2, 1, 6)]
	public void StraightConnector_FillPattern_AddsItsLayer(ConnectorFillPattern pattern, int outerPolygons, int clippedGroups, int pointsPerShape)
	{
		var element = ConnectorElement(RenderWithConnectors(StillConnectors(), Connector(0, 0, 1, 0, "right", pattern)));

		element.Children.Count(c => c.LocalName == "polygon").Should().Be(outerPolygons - 1);
		element.QuerySelectorAll("clipPath polygon").Should().ContainSingle();
		var groups = element.QuerySelectorAll("g[clip-path]");
		groups.Should().HaveCount(clippedGroups);
		foreach (var shape in groups.SelectMany(g => g.QuerySelectorAll("polygon")))
		{
			shape.GetAttribute("points")!.Split(' ').Should().HaveCount(pointsPerShape);
		}
	}

	/// <summary>Reversing a patterned straight connector moves its pattern, so the shapes are drawn in different places.</summary>
	[Theory]
	[InlineData(ConnectorFillPattern.Bars)]
	[InlineData(ConnectorFillPattern.Chevrons)]
	public void StraightConnector_Reversed_MovesThePattern(ConnectorFillPattern pattern)
	{
		var forward = PatternPoints(RenderWithConnectors(StillConnectors(), Connector(0, 0, 1, 0, "right", pattern)));
		var reversedConnector = Connector(0, 0, 1, 0, "right", pattern);
		reversedConnector.Reversed = true;

		var reversed = PatternPoints(RenderWithConnectors(StillConnectors(), reversedConnector));

		reversed.Should().NotBeEmpty().And.NotEqual(forward);
	}

	private static List<string?> PatternPoints(IRenderedComponent<PDTiles> cut)
		=> [.. cut.FindAll("g.connector g[clip-path] polygon").Select(p => p.GetAttribute("points"))];

	/// <summary>Every direction, including unknown ones, produces a four-cornered straight ribbon between the tiles.</summary>
	[Theory]
	[InlineData("left")]
	[InlineData("right")]
	[InlineData("up")]
	[InlineData("down")]
	[InlineData("diag-front")]
	[InlineData("diag-back")]
	[InlineData("diag-right")]
	[InlineData("diag-left")]
	[InlineData("diag-sideways")]
	[InlineData("sideways")]
	public void StraightConnector_AnyDirection_DrawsAFourCornerRibbon(string direction)
	{
		var element = ConnectorElement(RenderWithConnectors(StillConnectors(), Connector(0, 0, 1, 1, direction)));

		element.QuerySelector("clipPath polygon")!.GetAttribute("points")!.Split(' ').Should().HaveCount(4);
	}

	/// <summary>A connector whose end tile is off the grid is not drawn, in straight or curved modes.</summary>
	[Theory]
	[InlineData(ConnectionMode.StraightLine)]
	[InlineData(ConnectionMode.RowCurves)]
	public void Connector_ToTileOffTheGrid_IsNotDrawn(ConnectionMode mode)
	{
		var cut = RenderWithConnectors(StillConnectors(mode), Connector(0, 0, 5, 5, "down"));

		cut.FindAll("g.connector").Should().BeEmpty();
		cut.FindAll(TileSelector).Should().HaveCount(4);
	}

	/// <summary>
	/// A half-height ribbon sits at the top, centre or bottom of the tile side as aligned, with the connector's own
	/// setting taking precedence over the options.
	/// </summary>
	[Fact]
	public void StraightConnector_VerticalAlign_PlacesTheRibbon()
	{
		var top = TopEdgeY(ConnectorVerticalAlign.Top, null);
		var centre = TopEdgeY(ConnectorVerticalAlign.Center, null);
		var bottom = TopEdgeY(ConnectorVerticalAlign.Bottom, null);
		var overridden = TopEdgeY(ConnectorVerticalAlign.Bottom, ConnectorVerticalAlign.Top);

		top.Should().BeLessThan(centre);
		centre.Should().BeLessThan(bottom);
		overridden.Should().Be(top);
	}

	private double TopEdgeY(ConnectorVerticalAlign optionsAlign, ConnectorVerticalAlign? connectorAlign)
	{
		var options = StillConnectors();
		options.VerticalAlign = optionsAlign;
		options.Height = 50;
		var connector = Connector(0, 0, 1, 0, "right");
		connector.VerticalAlign = connectorAlign;
		var line = ConnectorElement(RenderWithConnectors(options, connector)).QuerySelector("line")!;
		return double.Parse(line.GetAttribute("y1")!, System.Globalization.CultureInfo.InvariantCulture);
	}

	/// <summary>Curve modes draw connectors as bezier paths, with the pattern layer chosen by the fill pattern.</summary>
	[Theory]
	[InlineData(ConnectorFillPattern.Random, 4, 0)]
	[InlineData(ConnectorFillPattern.Solid, 5, 0)]
	[InlineData(ConnectorFillPattern.Bars, 4, 4)]
	[InlineData(ConnectorFillPattern.Chevrons, 4, 6)]
	public void CurvedConnector_FillPattern_AddsItsLayer(ConnectorFillPattern pattern, int paths, int pointsPerShape)
	{
		var cut = RenderWithConnectors(StillConnectors(ConnectionMode.RowCurves), Connector(0, 0, 1, 1, "down", pattern));

		var element = cut.Find("g.connector.connector-bezier");
		element.QuerySelectorAll("path").Should().HaveCount(paths);
		element.QuerySelector("path")!.GetAttribute("d").Should().StartWith("M ").And.Contain(" C ").And.EndWith("Z");
		var shapes = element.QuerySelectorAll("g[clip-path] polygon");
		(shapes.Length > 0).Should().Be(pointsPerShape > 0);
		shapes.Should().OnlyContain(s => s.GetAttribute("points")!.Split(' ', StringSplitOptions.None).Length == pointsPerShape);
	}

	/// <summary>A curved connector in a direction that is not an edge direction still draws as a closed bezier ribbon.</summary>
	[Theory]
	[InlineData("diag-front")]
	[InlineData("sideways")]
	public void CurvedConnector_NonEdgeDirection_IsStillDrawn(string direction)
	{
		var cut = RenderWithConnectors(StillConnectors(ConnectionMode.RowCurves), Connector(0, 0, 1, 1, direction));

		cut.Find("g.connector-bezier path").GetAttribute("d").Should().StartWith("M ").And.EndWith("Z");
	}

	/// <summary>A deeper tile override lengthens the connector's attachment on that tile, so the ribbon is taller.</summary>
	[Fact]
	public void Connector_TileDepthOverride_TallerRibbon()
	{
		var shallow = RibbonHeight(null);
		var deep = RibbonHeight(60);

		deep.Should().BeGreaterThan(shallow);
	}

	private double RibbonHeight(int? tileDepth)
	{
		var cut = Render<PDTiles>(p => p
			.Add(x => x.Options, new TileGridOptions { Columns = 2, Rows = 2, Depth = 20 })
			.Add(x => x.ConnectorOptions, StillConnectors())
			.Add(x => x.Tiles, [new TileDefinition { Column = 0, Row = 0, Depth = tileDepth }, new TileDefinition { Column = 1, Row = 0, Depth = tileDepth }])
			.Add(x => x.Connectors, [Connector(0, 0, 1, 0, "right")]));
		var lines = cut.Find("g.connector").QuerySelectorAll("line");
		return Y(lines[1], "y1") - Y(lines[0], "y1");
	}

	private static double Y(IElement line, string attribute)
		=> double.Parse(line.GetAttribute(attribute)!, System.Globalization.CultureInfo.InvariantCulture);

	/// <summary>Reversing a patterned curved connector moves its pattern along the curve.</summary>
	[Theory]
	[InlineData(ConnectorFillPattern.Bars)]
	[InlineData(ConnectorFillPattern.Chevrons)]
	public void CurvedConnector_Reversed_MovesThePattern(ConnectorFillPattern pattern)
	{
		var options = StillConnectors(ConnectionMode.ColumnCurves);
		var forward = PatternPoints(RenderWithConnectors(options, Connector(0, 0, 1, 1, "right", pattern)));
		var reversedConnector = Connector(0, 0, 1, 1, "right", pattern);
		reversedConnector.Reversed = true;

		var reversed = PatternPoints(RenderWithConnectors(StillConnectors(ConnectionMode.ColumnCurves), reversedConnector));

		reversed.Should().NotBeEmpty().And.NotEqual(forward);
	}

	/// <summary>A curved ribbon's start moves down the tile side from top, to centre, to bottom alignment.</summary>
	[Fact]
	public void CurvedConnector_VerticalAlign_PlacesTheRibbon()
	{
		var top = CurveStartY(ConnectorVerticalAlign.Top);
		var centre = CurveStartY(ConnectorVerticalAlign.Center);
		var bottom = CurveStartY(ConnectorVerticalAlign.Bottom);

		top.Should().BeLessThan(centre);
		centre.Should().BeLessThan(bottom);
	}

	private double CurveStartY(ConnectorVerticalAlign align)
	{
		var options = StillConnectors(ConnectionMode.RowCurves);
		options.VerticalAlign = align;
		options.Height = 50;
		var d = RenderWithConnectors(options, Connector(0, 0, 1, 1, "down")).Find("g.connector-bezier path").GetAttribute("d")!;
		var start = d.Split(' ')[1];
		return double.Parse(start.Split(',')[1], System.Globalization.CultureInfo.InvariantCulture);
	}

	/// <summary>
	/// Straight connectors and column curves are drawn behind the tiles at their depth; row curves are drawn over
	/// the row they start from.
	/// </summary>
	[Theory]
	[InlineData(ConnectionMode.StraightLine, 0, 1, "right", 1)]
	[InlineData(ConnectionMode.RowCurves, 1, 1, "down", 2)]
	[InlineData(ConnectionMode.ColumnCurves, 1, 1, "right", 0)]
	public void Connector_IsLayeredByConnectionMode(ConnectionMode mode, int endColumn, int endRow, string direction, int expectedIndex)
	{
		var cut = RenderWithConnectors(StillConnectors(mode), Connector(0, 0, endColumn, endRow, direction));

		var children = cut.Find("g.tiles-container").Children.ToList();
		children.FindIndex(c => c.ClassList.Contains("connector")).Should().Be(expectedIndex);
	}

	/// <summary>Clicking a straight or curved connector raises <see cref="PDTiles.ConnectorClick"/> with its name, ends and definition.</summary>
	[Theory]
	[InlineData(ConnectionMode.StraightLine)]
	[InlineData(ConnectionMode.RowCurves)]
	public void ConnectorClick_RaisesEventWithConnectorDetails(ConnectionMode mode)
	{
		var connector = Connector(0, 0, 1, 1, "down");
		ConnectorClickEventArgs? received = null;
		var cut = Render<PDTiles>(p => p
			.Add(x => x.Options, new TileGridOptions { Columns = 2, Rows = 2 })
			.Add(x => x.ConnectorOptions, StillConnectors(mode))
			.Add(x => x.Logos, ["tiles/Alpha Logo.svg"])
			.Add(x => x.Connectors, [connector])
			.Add(x => x.ConnectorClick, args => received = args));

		cut.Find("g.connector").Click();

		received.Should().NotBeNull();
		received!.ConnectorName.Should().Be("Alpha?Alpha#0");
		received.Connector.Should().BeSameAs(connector);
		received.StartTile.Should().BeSameAs(connector.StartTile);
		received.EndTile.Should().BeSameAs(connector.EndTile);
	}

	/// <summary>
	/// With connectors and a non-zero speed the pattern animates; removing the connectors stops it, and the
	/// component disposes cleanly.
	/// </summary>
	[Fact]
	public async Task Animation_RunsWithConnectors_AndStopsWithoutThem()
	{
		var options = new TileConnectorOptions { AnimationSpeed = 100 };
		var cut = RenderWithConnectors(options, Connector(0, 0, 1, 0, "right", ConnectorFillPattern.Bars));

		cut.WaitForAssertion(() => cut.Instance.AnimationOffset.Should().BeGreaterThan(0));

		cut.Render(p => p.Add(x => x.Connectors, null));
		cut.FindAll("g.connector").Should().BeEmpty();
		await cut.Instance.DisposeAsync();
	}

	/// <summary>Turning the animation speed to zero while connectors are shown also stops the animation.</summary>
	[Fact]
	public void Animation_SpeedSetToZero_Stops()
	{
		var cut = RenderWithConnectors(new TileConnectorOptions { AnimationSpeed = 100 }, Connector(0, 0, 1, 0, "right"));
		cut.WaitForAssertion(() => cut.Instance.AnimationOffset.Should().BeGreaterThan(0));

		cut.Render(p => p.Add(x => x.ConnectorOptions, StillConnectors()));

		cut.FindAll("g.connector").Should().ContainSingle();
	}

	#endregion

	#region Random connectors

	private List<TileConnector> Generate(TileConnectorOptions connectorOptions, int gridPopulation = 100)
	{
		var cut = Render<PDTiles>(p => p
			.Add(x => x.Options, new TileGridOptions { Columns = 2, Rows = 2, Population = gridPopulation })
			.Add(x => x.ConnectorOptions, connectorOptions)
			.Add(x => x.Logos, ["tiles/Alpha Logo.svg"]));
		return cut.Instance.GenerateRandomConnectors();
	}

	private static TileConnectorOptions EveryEdge(ConnectorDirection direction = ConnectorDirection.All, int? perEdge = 1) => new()
	{
		Direction = direction,
		PerEdge = perEdge,
		Population = 100,
		AnimationSpeed = 0
	};

	/// <summary>In straight-line mode the direction filter decides which neighbouring pairs get a connector.</summary>
	[Theory]
	[InlineData(ConnectorDirection.All, 6)]
	[InlineData(ConnectorDirection.Orthogonal, 4)]
	[InlineData(ConnectorDirection.Diagonal, 2)]
	[InlineData(ConnectorDirection.DiagonalLeftRight, 1)]
	[InlineData(ConnectorDirection.DiagonalFrontBack, 1)]
	public void Random_StraightLine_RespectsDirectionFilter(ConnectorDirection direction, int expected)
	{
		var connectors = Generate(EveryEdge(direction));

		connectors.Should().HaveCount(expected);
		var isDiagonal = connectors.Select(c => c.Direction.StartsWith("diag-", StringComparison.Ordinal)).ToList();
		switch (direction)
		{
			case ConnectorDirection.Orthogonal:
				isDiagonal.Should().OnlyContain(d => !d);
				break;
			case ConnectorDirection.Diagonal:
				isDiagonal.Should().OnlyContain(d => d);
				break;
		}
	}

	/// <summary>The specific diagonal filters pick the matching diagonal.</summary>
	[Fact]
	public void Random_DiagonalFilters_PickTheirDiagonal()
	{
		Generate(EveryEdge(ConnectorDirection.DiagonalFrontBack)).Single().Direction.Should().BeOneOf("diag-front", "diag-back");
		Generate(EveryEdge(ConnectorDirection.DiagonalLeftRight)).Single().Direction.Should().BeOneOf("diag-left", "diag-right");
	}

	/// <summary>PerEdge sets the number of parallel connectors on each orthogonal edge, numbered within the edge.</summary>
	[Fact]
	public void Random_PerEdge_SetsParallelConnectorsPerOrthogonalEdge()
	{
		var connectors = Generate(EveryEdge(ConnectorDirection.Orthogonal, perEdge: 3));

		connectors.Should().HaveCount(12);
		connectors.Should().OnlyContain(c => c.EdgeTotal == 3);
		connectors.GroupBy(c => (c.StartTile.Column, c.StartTile.Row, c.EndTile.Column, c.EndTile.Row))
			.Should().HaveCount(4)
			.And.OnlyContain(g => g.Select(c => c.EdgeIndex).SequenceEqual(new[] { 0, 1, 2 }));
	}

	/// <summary>Zero per edge suppresses orthogonal connectors, but diagonals always get exactly one.</summary>
	[Fact]
	public void Random_PerEdgeZero_LeavesOnlyDiagonals()
	{
		var connectors = Generate(EveryEdge(perEdge: 0));

		connectors.Should().HaveCount(2);
		connectors.Should().OnlyContain(c => c.Direction.StartsWith("diag-", StringComparison.Ordinal) && c.EdgeTotal == 1);
	}

	/// <summary>Without PerEdge each orthogonal edge gets up to four connectors, each correctly numbered.</summary>
	[Fact]
	public void Random_PerEdgeUnset_GivesBetweenOneAndFourPerEdge()
	{
		var connectors = Generate(EveryEdge(ConnectorDirection.Orthogonal, perEdge: null));

		connectors.Should().OnlyContain(c => c.EdgeTotal >= 1 && c.EdgeTotal <= 4 && c.EdgeIndex < c.EdgeTotal);
	}

	/// <summary>No connectors are generated at zero connector population, or when no tiles are visible.</summary>
	[Fact]
	public void Random_ZeroPopulationOrNoVisibleTiles_GivesNoConnectors()
	{
		var options = EveryEdge();
		options.Population = 0;

		Generate(options).Should().BeEmpty();
		Generate(EveryEdge(), gridPopulation: 0).Should().BeEmpty();
	}

	/// <summary>Generated connectors take their settings from the options and their colours from the palette in turn.</summary>
	[Fact]
	public void Random_CopiesOptions_AndCyclesThePalette()
	{
		var options = EveryEdge();
		options.FillPattern = ConnectorFillPattern.Bars;
		options.Opacity = 42;
		options.AnimationSpeed = 7;
		options.Height = 60;
		options.VerticalAlign = ConnectorVerticalAlign.Center;

		var connectors = Generate(options);

		connectors.Should().OnlyContain(c => c.FillPattern == ConnectorFillPattern.Bars && c.Opacity == 42
			&& c.AnimationSpeed == 7 && c.Height == 60 && c.VerticalAlign == ConnectorVerticalAlign.Center);
		connectors.Select(c => c.Color).Should().Equal("#00FFFF", "#FF00FF", "#00FF00", "#FF6600", "#FFFF00", "#FF0000");
	}

	/// <summary>A random fill pattern resolves to one of the concrete patterns for each connector.</summary>
	[Fact]
	public void Random_RandomFillPattern_ResolvesToConcretePatterns()
	{
		var options = EveryEdge(perEdge: 4);
		options.FillPattern = ConnectorFillPattern.Random;

		Generate(options).Should().NotBeEmpty()
			.And.OnlyContain(c => c.FillPattern != ConnectorFillPattern.Random);
	}

	/// <summary>
	/// In curve modes every tile connects to every tile of the neighbouring row or column, once per pair, and the
	/// direction filter does not apply.
	/// </summary>
	[Theory]
	[InlineData(ConnectionMode.RowCurves, "up", "down")]
	[InlineData(ConnectionMode.ColumnCurves, "left", "right")]
	public void Random_CurveModes_ConnectNeighbouringRowsOrColumns(ConnectionMode mode, string backward, string forward)
	{
		var options = EveryEdge(ConnectorDirection.Diagonal);
		options.ConnectionMode = mode;

		var connectors = Generate(options);

		connectors.Should().HaveCount(4).And.OnlyContain(c => c.Direction == backward || c.Direction == forward);
		connectors.Should().OnlyContain(c => mode == ConnectionMode.RowCurves
			? Math.Abs(c.EndTile.Row - c.StartTile.Row) == 1
			: Math.Abs(c.EndTile.Column - c.StartTile.Column) == 1);
		connectors.Select(c => (Math.Min(c.StartTile.Row * 2 + c.StartTile.Column, c.EndTile.Row * 2 + c.EndTile.Column),
			Math.Max(c.StartTile.Row * 2 + c.StartTile.Column, c.EndTile.Row * 2 + c.EndTile.Column)))
			.Should().OnlyHaveUniqueItems();
	}

	#endregion
}