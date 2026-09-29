using AwesomeAssertions;
using PanoramicData.Blazor.Models.Tiles;

namespace PanoramicData.Blazor.Test.Models.Tiles;

/// <summary>Tests for <see cref="TileConnectorOptions"/>.</summary>
public class TileConnectorOptionsTests
{
	/// <summary>New options draw animated straight connectors in every direction with a random fill.</summary>
	[Fact]
	public void New_HasDocumentedDefaults()
	{
		var options = new TileConnectorOptions();

		options.ConnectionMode.Should().Be(ConnectionMode.StraightLine);
		options.CurveTension.Should().Be(50);
		options.FillPattern.Should().Be(ConnectorFillPattern.Random);
		options.Direction.Should().Be(ConnectorDirection.All);
		options.PerEdge.Should().BeNull();
		options.Population.Should().Be(50);
		options.Height.Should().Be(100);
		options.VerticalAlign.Should().Be(ConnectorVerticalAlign.Bottom);
		options.Opacity.Should().Be(80);
		options.Animation.Should().BeTrue();
		options.AnimationSpeed.Should().Be(35);
	}

	/// <summary>All members round-trip.</summary>
	[Fact]
	public void SettableMembers_RoundTrip()
	{
		var options = new TileConnectorOptions
		{
			ConnectionMode = ConnectionMode.RowCurves,
			CurveTension = 10,
			FillPattern = ConnectorFillPattern.Bars,
			Direction = ConnectorDirection.DiagonalFrontBack,
			PerEdge = 2,
			Population = 25,
			Height = 60,
			VerticalAlign = ConnectorVerticalAlign.Center,
			Opacity = 20,
			Animation = false,
			AnimationSpeed = 5
		};

		options.ConnectionMode.Should().Be(ConnectionMode.RowCurves);
		options.CurveTension.Should().Be(10);
		options.FillPattern.Should().Be(ConnectorFillPattern.Bars);
		options.Direction.Should().Be(ConnectorDirection.DiagonalFrontBack);
		options.PerEdge.Should().Be(2);
		options.Population.Should().Be(25);
		options.Height.Should().Be(60);
		options.VerticalAlign.Should().Be(ConnectorVerticalAlign.Center);
		options.Opacity.Should().Be(20);
		options.Animation.Should().BeFalse();
		options.AnimationSpeed.Should().Be(5);
	}
}
