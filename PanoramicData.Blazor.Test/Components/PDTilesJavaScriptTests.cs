using AwesomeAssertions;
using Bunit;
using PanoramicData.Blazor.Models.Tiles;

namespace PanoramicData.Blazor.Test;

/// <summary>
/// Tests that <see cref="PDTilesJavaScript"/> renders its container, hands its options to the tile grid
/// module in the shape the module expects, forwards its public commands, and raises tile and connector
/// clicks reported from JavaScript.
/// </summary>
public class PDTilesJavaScriptTests : BunitContext
{
	private const string ModulePath = "./_content/PanoramicData.Blazor/PDTilesJavaScript.razor.js";

	private readonly BunitJSModuleInterop _module;

	/// <summary>Sets up the rendering context and the tile grid module.</summary>
	public PDTilesJavaScriptTests()
	{
		JSInterop.Mode = JSRuntimeMode.Loose;
		_module = JSInterop.SetupModule(ModulePath);
	}

	/// <summary>
	/// Verifies that the container carries the id, classes, size, background colour and extra style.
	/// </summary>
	[Fact]
	public void Renders_the_container_with_its_id_size_and_style()
	{
		var component = Render<PDTilesJavaScript>(parameters => parameters
			.Add(p => p.Id, "tiles")
			.Add(p => p.CssClass, "extra")
			.Add(p => p.Width, "300px")
			.Add(p => p.Height, "200px")
			.Add(p => p.Style, "border: 1px solid red;")
			.Add(p => p.Options, new TileGridOptions { BackgroundColor = "#101010" }));

		var container = component.Find("div");
		container.Id.Should().Be("tiles");
		container.ClassList.Should().Contain(["pd-tiles-js", "extra"]);
		var style = container.GetAttribute("style");
		style.Should().Contain("width: 300px").And.Contain("height: 200px")
			.And.Contain("background-color: #101010").And.Contain("border: 1px solid red;");
	}

	/// <summary>
	/// Verifies that a generated id is used when none is given.
	/// </summary>
	[Fact]
	public void Generates_an_id_when_none_is_given()
	{
		var component = Render<PDTilesJavaScript>();

		component.Find("div").Id.Should().MatchRegex("^pd-tiles-js-[0-9]+$");
	}

	/// <summary>
	/// Verifies that the grid is initialised with the container id, the grid options, the connector
	/// options and the logos.
	/// </summary>
	[Fact]
	public void First_render_initialises_the_grid_with_the_options()
	{
		var options = new TileGridOptions { Columns = 7, BackgroundColor = "#123456" };
		var connectors = new TileConnectorOptions
		{
			FillPattern = ConnectorFillPattern.Chevrons,
			PerEdge = 3,
			VerticalAlign = ConnectorVerticalAlign.Center
		};
		List<string> logos = ["a.svg", "b.svg"];

		Render<PDTilesJavaScript>(parameters => parameters
			.Add(p => p.Id, "tiles")
			.Add(p => p.Options, options)
			.Add(p => p.ConnectorOptions, connectors)
			.Add(p => p.Logos, logos));

		var initialize = _module.VerifyInvoke("initialize");
		initialize.Arguments.Should().HaveCount(3);
		initialize.Arguments[0].Should().Be("tiles");
		var config = initialize.Arguments[1];
		ReadProperty(config, "cols").Should().Be(7);
		ReadProperty(config, "bgColor").Should().Be("#123456");
		ReadProperty(config, "connFillPattern").Should().Be("Chevrons");
		ReadProperty(config, "connPerEdge").Should().Be("3");
		ReadProperty(config, "connVAlign").Should().Be("center");
		ReadProperty(config, "logos").Should().BeSameAs(logos);
		initialize.Arguments[2].Should().NotBeNull();
	}

	/// <summary>
	/// Verifies that an unset per-edge count is passed as "random".
	/// </summary>
	[Fact]
	public void An_unset_per_edge_count_is_passed_as_random()
	{
		Render<PDTilesJavaScript>();

		ReadProperty(_module.VerifyInvoke("initialize").Arguments[1], "connPerEdge").Should().Be("random");
	}

	/// <summary>
	/// Verifies that each connector direction is passed as the short name the module expects.
	/// </summary>
	[Theory]
	[InlineData(ConnectorDirection.All, "all")]
	[InlineData(ConnectorDirection.Orthogonal, "ortho")]
	[InlineData(ConnectorDirection.Diagonal, "diag")]
	[InlineData(ConnectorDirection.DiagonalLeftRight, "diag-lr")]
	[InlineData(ConnectorDirection.DiagonalFrontBack, "diag-fb")]
	[InlineData((ConnectorDirection)99, "all")]
	public void Connector_directions_are_passed_as_module_names(ConnectorDirection direction, string expected)
	{
		Render<PDTilesJavaScript>(parameters => parameters
			.Add(p => p.ConnectorOptions, new TileConnectorOptions { Direction = direction }));

		ReadProperty(_module.VerifyInvoke("initialize").Arguments[1], "connDirection").Should().Be(expected);
	}

	/// <summary>
	/// Verifies that UpdateAsync sends the current grid options, without connector settings.
	/// </summary>
	[Fact]
	public async Task UpdateAsync_sends_the_current_grid_options()
	{
		var options = new TileGridOptions { Columns = 2 };
		var component = Render<PDTilesJavaScript>(parameters => parameters
			.Add(p => p.Id, "tiles")
			.Add(p => p.Options, options));

		options.Columns = 9;
		await component.InvokeAsync(component.Instance.UpdateAsync);

		var update = _module.VerifyInvoke("update");
		update.Arguments[0].Should().Be("tiles");
		ReadProperty(update.Arguments[1], "cols").Should().Be(9);
		update.Arguments[1]!.GetType().GetProperty("connDirection").Should().BeNull();
	}

	/// <summary>
	/// Verifies that the shuffle and connector commands are forwarded to the module with the grid id.
	/// </summary>
	[Fact]
	public async Task Commands_are_forwarded_with_the_grid_id()
	{
		var component = Render<PDTilesJavaScript>(parameters => parameters.Add(p => p.Id, "tiles"));

		await component.InvokeAsync(component.Instance.ShuffleAsync);
		await component.InvokeAsync(component.Instance.RandomizeConnectorsAsync);
		await component.InvokeAsync(component.Instance.ClearConnectorsAsync);

		_module.VerifyInvoke("shuffle").Arguments.Should().Equal("tiles");
		_module.VerifyInvoke("randomizeConnectors").Arguments.Should().Equal("tiles");
		_module.VerifyInvoke("clearConnectors").Arguments.Should().Equal("tiles");
	}

	/// <summary>
	/// Verifies that a tile click from JavaScript raises TileClick with the tile's details.
	/// </summary>
	[Fact]
	public async Task A_tile_click_from_javascript_raises_TileClick()
	{
		TileClickEventArgs? raised = null;
		var component = Render<PDTilesJavaScript>(parameters => parameters
			.Add(p => p.TileClick, args => raised = args));

		await component.InvokeAsync(() => component.Instance.OnTileClick(4, "ReportMagic", 1, 2));

		raised.Should().NotBeNull();
		raised!.TileId.Should().Be(4);
		raised.TileName.Should().Be("ReportMagic");
		raised.Column.Should().Be(1);
		raised.Row.Should().Be(2);
	}

	/// <summary>
	/// Verifies that a connector click from JavaScript raises ConnectorClick with both end tiles.
	/// </summary>
	[Fact]
	public async Task A_connector_click_from_javascript_raises_ConnectorClick()
	{
		ConnectorClickEventArgs? raised = null;
		var component = Render<PDTilesJavaScript>(parameters => parameters
			.Add(p => p.ConnectorClick, args => raised = args));

		await component.InvokeAsync(() => component.Instance.OnConnectorClick("link", 0, 1, 2, 3));

		raised.Should().NotBeNull();
		raised!.ConnectorName.Should().Be("link");
		raised.StartTile.Column.Should().Be(0);
		raised.StartTile.Row.Should().Be(1);
		raised.EndTile.Column.Should().Be(2);
		raised.EndTile.Row.Should().Be(3);
	}

	/// <summary>
	/// Verifies that disposing tells the module to dispose the grid.
	/// </summary>
	[Fact]
	public async Task Disposing_disposes_the_grid()
	{
		var component = Render<PDTilesJavaScript>(parameters => parameters.Add(p => p.Id, "tiles"));

		await component.Instance.DisposeAsync();

		_module.VerifyInvoke("dispose").Arguments.Should().Equal("tiles");
	}

	private static object? ReadProperty(object? source, string name)
		=> source!.GetType().GetProperty(name)!.GetValue(source);
}
