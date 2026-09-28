using AwesomeAssertions;
using PanoramicData.Blazor.Models.Tiles;

namespace PanoramicData.Blazor.Test.Models.Tiles;

/// <summary>Tests for <see cref="TileClickEventArgs"/> and <see cref="ConnectorClickEventArgs"/>.</summary>
public class TileClickEventArgsTests
{
	/// <summary>A new tile click names no tile, and its members round-trip.</summary>
	[Fact]
	public void TileClick_DefaultsAndRoundTrip()
	{
		var empty = new TileClickEventArgs();
		empty.TileId.Should().Be(0);
		empty.TileName.Should().BeEmpty();
		empty.Tile.Should().BeNull();

		var tile = new TileDefinition { Column = 2, Row = 3 };
		var args = new TileClickEventArgs { TileId = 7, TileName = "Web", Column = 2, Row = 3, Tile = tile };

		args.TileId.Should().Be(7);
		args.TileName.Should().Be("Web");
		args.Column.Should().Be(2);
		args.Row.Should().Be(3);
		args.Tile.Should().BeSameAs(tile);
	}

	/// <summary>A new connector click has non-null start and end coordinates, and its members round-trip.</summary>
	[Fact]
	public void ConnectorClick_DefaultsAndRoundTrip()
	{
		var empty = new ConnectorClickEventArgs();
		empty.ConnectorName.Should().BeEmpty();
		empty.StartTile.Should().NotBeNull();
		empty.EndTile.Should().NotBeNull();
		empty.Connector.Should().BeNull();

		var connector = new TileConnector();
		var args = new ConnectorClickEventArgs
		{
			ConnectorName = "link",
			StartTile = new TileCoordinate { Column = 1, Row = 2 },
			EndTile = new TileCoordinate { Column = 3, Row = 4 },
			Connector = connector
		};

		args.ConnectorName.Should().Be("link");
		args.StartTile.Column.Should().Be(1);
		args.EndTile.Row.Should().Be(4);
		args.Connector.Should().BeSameAs(connector);
	}
}
