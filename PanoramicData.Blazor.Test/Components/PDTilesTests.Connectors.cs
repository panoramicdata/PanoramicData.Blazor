using AngleSharp.Dom;
using AwesomeAssertions;
using Bunit;
using Microsoft.AspNetCore.Components.Web;
using PanoramicData.Blazor.Models.Tiles;

namespace PanoramicData.Blazor.Test.Components;

/// <summary>
/// Connector drawing, layering, click and animation tests for <see cref="PDTiles"/>.
/// </summary>
public partial class PDTilesTests
{
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
	public async Task ConnectorClick_RaisesEventWithConnectorDetails(ConnectionMode mode)
	{
		var connector = Connector(0, 0, 1, 1, "down");
		ConnectorClickEventArgs? received = null;
		var cut = Render<PDTiles>(p => p
			.Add(x => x.Options, new TileGridOptions { Columns = 2, Rows = 2 })
			.Add(x => x.ConnectorOptions, StillConnectors(mode))
			.Add(x => x.Logos, ["tiles/Alpha Logo.svg"])
			.Add(x => x.Connectors, [connector])
			.Add(x => x.ConnectorClick, args => received = args));

		await cut.Find("g.connector").ClickAsync(new MouseEventArgs());

		received.Should().NotBeNull();
		received!.ConnectorName.Should().Be("Alpha?Alpha#0");
		received.Connector.Should().BeSameAs(connector);
		received.StartTile.Should().BeSameAs(connector.StartTile);
		received.EndTile.Should().BeSameAs(connector.EndTile);
	}

	/// <summary>
	/// How long to wait for the animation timer. The wait returns as soon as the condition holds, so this is only
	/// reached when something is wrong; it is long because the timer's ticks share the thread pool and the
	/// renderer's dispatcher with the whole suite, and under full-suite load with coverage the first tick and
	/// the check that observes it were seen to take longer than bUnit's one-second default.
	/// </summary>
	private static readonly TimeSpan _timerWait = TimeSpan.FromSeconds(30);

	/// <summary>
	/// Renders the cheapest grid that can hold a connector, with animation on. Every tick of the 60 fps timer
	/// re-renders the whole component on the dispatcher, so a cheap render keeps those ticks from queuing up
	/// ahead of the test's own checks and parameter changes.
	/// </summary>
	private IRenderedComponent<PDTiles> RenderAnimated()
		=> Render<PDTiles>(p => p
			.Add(x => x.Options, new TileGridOptions { Columns = 2, Rows = 1, LineOpacity = 0, Glow = 0, Reflection = 0 })
			.Add(x => x.ConnectorOptions, new TileConnectorOptions { AnimationSpeed = 100 })
			.Add(x => x.Logos, [string.Empty])
			.Add(x => x.Connectors, [Connector(0, 0, 1, 0, "right")]));

	/// <summary>
	/// With connectors and a non-zero speed the pattern animates; removing the connectors stops it, and the
	/// component disposes cleanly.
	/// </summary>
	[Fact]
	public async Task Animation_RunsWithConnectors_AndStopsWithoutThem()
	{
		var cut = RenderAnimated();

		cut.WaitForAssertion(() => cut.Instance.AnimationOffset.Should().BeGreaterThan(0), _timerWait);

		cut.Render(p => p.Add(x => x.Connectors, null));
		cut.WaitForAssertion(() => cut.FindAll("g.connector").Should().BeEmpty(), _timerWait);
		await cut.InvokeAsync(async () => await cut.Instance.DisposeAsync());
	}

	/// <summary>Turning the animation speed to zero while connectors are shown stops the animation but keeps the connector.</summary>
	[Fact]
	public void Animation_SpeedSetToZero_KeepsTheConnector()
	{
		var cut = RenderAnimated();
		cut.WaitForAssertion(() => cut.Instance.AnimationOffset.Should().BeGreaterThan(0), _timerWait);

		cut.Render(p => p.Add(x => x.ConnectorOptions, StillConnectors()));

		cut.WaitForAssertion(() => cut.Instance.ConnectorOptions.AnimationSpeed.Should().Be(0), _timerWait);
		cut.FindAll("g.connector").Should().ContainSingle();
	}
}
