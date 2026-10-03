using AwesomeAssertions;
using Bunit;
using PanoramicData.Blazor.Models.Tiles;

namespace PanoramicData.Blazor.Test.Components;

/// <summary>
/// Random connector generation tests for <see cref="PDTiles"/>.
/// </summary>
public partial class PDTilesTests
{
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
		if (direction == ConnectorDirection.Orthogonal)
		{
			isDiagonal.Should().OnlyContain(d => !d);
		}
		else if (direction == ConnectorDirection.Diagonal)
		{
			isDiagonal.Should().OnlyContain(d => d);
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

	/// <summary>A tile hidden by its definition gets no connectors, while its visible neighbours stay connected.</summary>
	[Fact]
	public void Random_HiddenTile_GetsNoConnectors()
	{
		var cut = Render<PDTiles>(p => p
			.Add(x => x.Options, new TileGridOptions { Columns = 2, Rows = 2 })
			.Add(x => x.ConnectorOptions, EveryEdge())
			.Add(x => x.Logos, ["tiles/Alpha Logo.svg"])
			.Add(x => x.Tiles, [new TileDefinition { Column = 1, Row = 1, Visible = false }]));

		var connectors = cut.Instance.GenerateRandomConnectors();

		// Of the six neighbouring pairs, the three that include the hidden tile are skipped
		connectors.Should().HaveCount(3)
			.And.OnlyContain(c => !(c.StartTile.Column == 1 && c.StartTile.Row == 1) && !(c.EndTile.Column == 1 && c.EndTile.Row == 1));
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
}
