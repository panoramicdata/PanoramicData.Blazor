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
}
