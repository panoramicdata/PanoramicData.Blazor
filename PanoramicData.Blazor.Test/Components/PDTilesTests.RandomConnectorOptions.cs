using AwesomeAssertions;
using Bunit;
using PanoramicData.Blazor.Models.Tiles;

namespace PanoramicData.Blazor.Test.Components;

/// <summary>
/// Random connector tests for <see cref="PDTiles"/> covering population, hidden tiles, copied options, fill patterns and curve modes.
/// </summary>
public partial class PDTilesTests
{
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
