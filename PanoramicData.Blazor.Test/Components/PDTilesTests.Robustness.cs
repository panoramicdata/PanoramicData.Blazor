using System.Globalization;
using System.Text.RegularExpressions;
using AwesomeAssertions;
using Bunit;
using PanoramicData.Blazor.Models.Tiles;

namespace PanoramicData.Blazor.Test.Components;

/// <summary>
/// Regression tests for <see cref="PDTiles"/> defects (#155): an empty logo list, culture-dependent SVG
/// coordinates, tile definitions changed after the first render, short hex colours, and options that were
/// never read.
/// </summary>
public partial class PDTilesTests
{
	[GeneratedRegex("blazor:[A-Za-z]+=\"[^\"]*\"")]
	private static partial Regex RendererIds();

	/// <summary>An empty logo list renders every tile, without a logo, instead of throwing.</summary>
	[Fact]
	public void EmptyLogos_RendersTilesWithoutLogos()
	{
		var cut = RenderTiles(new TileGridOptions { Columns = 2, Rows = 2 }, logos: []);

		cut.FindAll(TileSelector).Should().HaveCount(4);
		cut.FindAll("image").Should().BeEmpty();
		cut.FindAll(TileSelector).Should().OnlyContain(t => t.GetAttribute("data-tile-name") == "Unknown");
	}

	/// <summary>A per-tile logo still shows when the shared logo list is empty.</summary>
	[Fact]
	public void EmptyLogos_StillShowsATileLogoOverride()
	{
		var cut = RenderTiles(
			new TileGridOptions { Columns = 2, Rows = 1 },
			logos: [],
			tiles: [new TileDefinition { Column = 1, Row = 0, Logo = "tiles/Own Logo.svg" }]);

		cut.FindAll("image").Should().ContainSingle()
			.Which.GetAttribute("href").Should().Be("tiles/Own Logo.svg");
	}

	/// <summary>
	/// Under a culture whose decimal separator is a comma, every SVG coordinate is still written with a point:
	/// the markup is identical to the invariant-culture markup, and every straight-connector point is an
	/// "x,y" pair.
	/// </summary>
	[Fact]
	public void Markup_IsIndependentOfTheCurrentCulture()
	{
		var invariant = RenderUnderCulture(CultureInfo.InvariantCulture);
		var german = RenderUnderCulture(CultureInfo.GetCultureInfo("de-DE"));

		german.markup.Should().Be(invariant.markup);
		german.points.Should().NotBeEmpty();
		german.points.Should().OnlyContain(p => p.Split(' ').All(pair => pair.Split(',').Length == 2));
	}

	private (string markup, List<string> points) RenderUnderCulture(CultureInfo culture)
	{
		var original = CultureInfo.CurrentCulture;
		var originalUi = CultureInfo.CurrentUICulture;
		CultureInfo.CurrentCulture = culture;
		CultureInfo.CurrentUICulture = culture;
		try
		{
			var cut = Render<PDTiles>(p => p
				.Add(x => x.Id, Id)
				.Add(x => x.Options, new TileGridOptions { Columns = 2, Rows = 2, Depth = 17, Gap = 33, Perspective = 7, LineOpacity = 15 })
				.Add(x => x.ConnectorOptions, StillConnectors())
				.Add(x => x.Logos, ["tiles/Alpha Logo.svg"])
				.Add(x => x.Connectors, [Connector(0, 0, 1, 0, "right"), Connector(0, 0, 1, 1, "diag-front", ConnectorFillPattern.Bars)]));

			var points = cut.FindAll("g.connector > polygon")
				.Select(p => p.GetAttribute("points")!)
				.ToList();
			// Element reference and event handler ids differ per render, so they are not part of the comparison
			return (RendererIds().Replace(cut.Markup, string.Empty), points);
		}
		finally
		{
			CultureInfo.CurrentCulture = original;
			CultureInfo.CurrentUICulture = originalUi;
		}
	}

	/// <summary>Tile definitions supplied after the first render are applied.</summary>
	[Fact]
	public void Tiles_ChangedAfterFirstRender_AreApplied()
	{
		var cut = Render<PDTiles>(p => p
			.Add(x => x.Id, Id)
			.Add(x => x.Options, new TileGridOptions { Columns = 2, Rows = 1 })
			.Add(x => x.ConnectorOptions, StillConnectors())
			.Add(x => x.Logos, ["tiles/Alpha Logo.svg"]));
		cut.Find("g[data-tile-id='0']").GetAttribute("style").Should().StartWith("opacity: 1");

		cut.Render(p => p.Add(x => x.Tiles, [new TileDefinition { Column = 0, Row = 0, Visible = false, Logo = "tiles/New Logo.svg", Color = "#00FF00" }]));

		var tile = cut.Find("g[data-tile-id='0']");
		tile.GetAttribute("style").Should().StartWith("opacity: 0");
		tile.GetAttribute("data-tile-name").Should().Be("New");
		cut.FindAll($"linearGradient#{Id}-topGrad-00FF00").Should().ContainSingle();
	}

	/// <summary>
	/// Removing a tile definition after the first render restores that tile's generated state, and leaves the
	/// other tiles' generated logos where they were.
	/// </summary>
	[Fact]
	public void Tiles_RemovedAfterFirstRender_RestoreTheGeneratedState()
	{
		List<string> logos = ["tiles/A Logo.svg", "tiles/B Logo.svg", "tiles/C Logo.svg"];
		var cut = RenderTiles(new TileGridOptions { Columns = 3, Rows = 1 }, logos: logos);
		var before = cut.FindAll(TileSelector).Select(t => t.GetAttribute("data-tile-name")).ToList();

		cut.Render(p => p.Add(x => x.Tiles, [new TileDefinition { Column = 1, Row = 0, Visible = false, Logo = "tiles/X Logo.svg" }]));
		var during = cut.FindAll(TileSelector).Select(t => t.GetAttribute("data-tile-name")).ToList();
		cut.Render(p => p.Add(x => x.Tiles, null));

		during.Should().Equal(before[0], "X", before[2]);
		cut.FindAll(TileSelector).Select(t => t.GetAttribute("data-tile-name")).Should().Equal(before);
		cut.FindAll(TileSelector).Should().OnlyContain(t => t.GetAttribute("style")!.StartsWith("opacity: 1", StringComparison.Ordinal));
	}

	/// <summary>A definition changed in place is applied when the parent re-renders.</summary>
	[Fact]
	public void Tiles_ChangedInPlace_AreAppliedOnTheNextRender()
	{
		var definition = new TileDefinition { Column = 0, Row = 0 };
		var cut = RenderTiles(new TileGridOptions { Columns = 2, Rows = 1 }, tiles: [definition]);

		definition.Visible = false;
		cut.Render(p => p.Add(x => x.Tiles, [definition]));

		cut.Find("g[data-tile-id='0']").GetAttribute("style").Should().StartWith("opacity: 0");
	}

	/// <summary>The three-digit hex form is accepted, as the equivalent six-digit colour, for the grid and a tile.</summary>
	[Fact]
	public void ShortHexColour_IsAccepted()
	{
		var cut = RenderTiles(
			new TileGridOptions { Columns = 2, Rows = 1, TileColor = "#FFF", LineColor = "#abc" },
			tiles: [new TileDefinition { Column = 1, Row = 0, Color = "#f00" }]);

		cut.Find($"linearGradient#{Id}-topGrad stop").GetAttribute("style").Should().Be("stop-color:#FFFFFF");
		cut.FindAll($"linearGradient#{Id}-frontGrad-f00 stop")[0].GetAttribute("style").Should().Be("stop-color:#3F0000");
		cut.Find("g.grid-lines line").GetAttribute("stroke").Should().Be("rgba(170, 187, 204, 0.15)");
	}

	/// <summary>A colour that is not hex is rejected with a message naming the value and the accepted forms.</summary>
	[Theory]
	[InlineData("red")]
	[InlineData("#12")]
	[InlineData("#GGGGGG")]
	public void InvalidColour_IsRejectedWithAClearMessage(string colour)
	{
		var render = () => RenderTiles(new TileGridOptions { TileColor = colour });

		render.Should().Throw<ArgumentException>()
			.WithMessage($"*'{colour}'*#RGB*#RRGGBB*");
	}

	/// <summary>Turning <see cref="TileConnectorOptions.Animation"/> off stops the animation, whatever the speed.</summary>
	[Theory]
	[InlineData(true, true)]
	[InlineData(false, false)]
	public void AnimationOption_DecidesWhetherConnectorsAnimate(bool animation, bool expectAnimating)
	{
		var cut = Render<PDTiles>(p => p
			.Add(x => x.Options, new TileGridOptions { Columns = 2, Rows = 1, LineOpacity = 0, Glow = 0, Reflection = 0 })
			.Add(x => x.ConnectorOptions, new TileConnectorOptions { Animation = animation, AnimationSpeed = 100 })
			.Add(x => x.Logos, [string.Empty])
			.Add(x => x.Connectors, [Connector(0, 0, 1, 0, "right")]));

		cut.Instance.IsAnimating.Should().Be(expectAnimating);
		cut.FindAll("g.connector").Should().ContainSingle();
	}

	/// <summary>Turning <see cref="TileConnectorOptions.Animation"/> off after the first render stops a running animation.</summary>
	[Fact]
	public void AnimationOption_TurnedOff_StopsTheAnimation()
	{
		var cut = RenderAnimated();
		cut.Instance.IsAnimating.Should().BeTrue();

		cut.Render(p => p.Add(x => x.ConnectorOptions, new TileConnectorOptions { Animation = false, AnimationSpeed = 100 }));

		cut.Instance.IsAnimating.Should().BeFalse();
	}

	/// <summary>
	/// A per-tile <see cref="TileDefinition.Glow"/> gives that tile's top face its own glow filter at that
	/// intensity; zero removes the glow; tiles without an override keep the shared filter.
	/// </summary>
	[Fact]
	public void TileGlow_OverridesThatTilesGlow()
	{
		var cut = RenderTiles(
			new TileGridOptions { Columns = 3, Rows = 1 },
			tiles:
			[
				new TileDefinition { Column = 0, Row = 0, Glow = 50 },
				new TileDefinition { Column = 1, Row = 0, Glow = 0 }
			]);

		var bright = cut.Find($"g#{Id}-tile-0");
		var filter = bright.QuerySelector($"filter#{Id}-glow-0 feDropShadow")!;
		filter.GetAttribute("flood-opacity").Should().Be("0.5");
		filter.GetAttribute("stdDeviation").Should().Be("6");
		TopFace(bright).GetAttribute("filter").Should().Be($"url(#{Id}-glow-0)");

		var none = cut.Find($"g#{Id}-tile-1");
		none.QuerySelectorAll("filter").Should().BeEmpty();
		TopFace(none).HasAttribute("filter").Should().BeFalse();

		var plain = cut.Find($"g#{Id}-tile-2");
		plain.QuerySelectorAll("filter").Should().BeEmpty();
		TopFace(plain).GetAttribute("filter").Should().Be($"url(#{Id}-glow)");
	}

	private static AngleSharp.Dom.IElement TopFace(AngleSharp.Dom.IElement tile)
		=> tile.QuerySelectorAll("path").Single(p => p.GetAttribute("fill")?.Contains("topGrad", StringComparison.Ordinal) == true);
}
