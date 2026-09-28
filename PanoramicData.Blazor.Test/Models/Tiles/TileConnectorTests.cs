using AwesomeAssertions;
using PanoramicData.Blazor.Models.Tiles;

namespace PanoramicData.Blazor.Test.Models.Tiles;

/// <summary>Tests for <see cref="TileConnector"/> and <see cref="TileCoordinate"/>.</summary>
public class TileConnectorTests
{
	/// <summary>A new connector is a solid cyan line at 80% opacity, the only edge between its tiles.</summary>
	[Fact]
	public void New_HasDocumentedDefaults()
	{
		var connector = new TileConnector();

		connector.StartTile.Should().NotBeNull();
		connector.EndTile.Should().NotBeNull();
		connector.Direction.Should().BeEmpty();
		connector.Reversed.Should().BeFalse();
		connector.Color.Should().Be("#00FFFF");
		connector.Opacity.Should().Be(80);
		connector.AnimationSpeed.Should().Be(35);
		connector.FillPattern.Should().Be(ConnectorFillPattern.Solid);
		connector.EdgeIndex.Should().Be(0);
		connector.EdgeTotal.Should().Be(1);
		connector.Height.Should().BeNull();
		connector.VerticalAlign.Should().BeNull();
	}

	/// <summary>All members round-trip.</summary>
	[Fact]
	public void SettableMembers_RoundTrip()
	{
		var connector = new TileConnector
		{
			StartTile = new TileCoordinate { Column = 1, Row = 1 },
			EndTile = new TileCoordinate { Column = 2, Row = 2 },
			Direction = "diagonal",
			Reversed = true,
			Color = "#FF0000",
			Opacity = 50,
			AnimationSpeed = 10,
			FillPattern = ConnectorFillPattern.Chevrons,
			EdgeIndex = 1,
			EdgeTotal = 3,
			Height = 40,
			VerticalAlign = ConnectorVerticalAlign.Top
		};

		connector.StartTile.Column.Should().Be(1);
		connector.EndTile.Row.Should().Be(2);
		connector.Direction.Should().Be("diagonal");
		connector.Reversed.Should().BeTrue();
		connector.Color.Should().Be("#FF0000");
		connector.Opacity.Should().Be(50);
		connector.AnimationSpeed.Should().Be(10);
		connector.FillPattern.Should().Be(ConnectorFillPattern.Chevrons);
		connector.EdgeIndex.Should().Be(1);
		connector.EdgeTotal.Should().Be(3);
		connector.Height.Should().Be(40);
		connector.VerticalAlign.Should().Be(ConnectorVerticalAlign.Top);
	}
}
