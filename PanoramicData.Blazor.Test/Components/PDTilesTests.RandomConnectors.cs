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
}
