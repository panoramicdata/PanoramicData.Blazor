using AngleSharp.Dom;
using AwesomeAssertions;
using Bunit;
using Microsoft.AspNetCore.Components.Web;
using PanoramicData.Blazor.Models.Tiles;

namespace PanoramicData.Blazor.Test.Components;

/// <summary>
/// Tile population, naming, overrides, click and child-content tests for <see cref="PDTiles"/>.
/// </summary>
public partial class PDTilesTests
{
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
	public async Task TileClick_RaisesEventWithTileDetails()
	{
		var definition = new TileDefinition { Column = 1, Row = 0, Tag = "tag" };
		var clicks = new List<TileClickEventArgs>();
		var cut = Render<PDTiles>(p => p
			.Add(x => x.Id, Id)
			.Add(x => x.Options, new TileGridOptions { Columns = 2, Rows = 1 })
			.Add(x => x.Logos, ["tiles/Alpha Logo.svg"])
			.Add(x => x.Tiles, [definition])
			.Add(x => x.TileClick, args => clicks.Add(args)));

		await cut.Find($"g#{Id}-tile-1").ClickAsync(new MouseEventArgs());
		await cut.Find($"g#{Id}-tile-0").ClickAsync(new MouseEventArgs());

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
}
