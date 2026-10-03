using AwesomeAssertions;
using Bunit;
using PanoramicData.Blazor.Models.Tiles;

namespace PanoramicData.Blazor.Test.Components;

/// <summary>
/// Curved connector drawing tests for <see cref="PDTiles"/>.
/// </summary>
public partial class PDTilesTests
{
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
}
