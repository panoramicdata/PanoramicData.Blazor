using AwesomeAssertions;
using PanoramicData.Blazor.Models.Tiles;

namespace PanoramicData.Blazor.Test.Models.Tiles;

/// <summary>Tests for <see cref="TileDefinition"/>.</summary>
public class TileDefinitionTests
{
	/// <summary>A new tile is visible at the origin with every styling override unset.</summary>
	[Fact]
	public void New_HasDocumentedDefaults()
	{
		var tile = new TileDefinition();

		tile.Column.Should().Be(0);
		tile.Row.Should().Be(0);
		tile.Logo.Should().BeNull();
		tile.Color.Should().BeNull();
		tile.Depth.Should().BeNull();
		tile.LogoSize.Should().BeNull();
		tile.LogoRotation.Should().BeNull();
		tile.Reflection.Should().BeNull();
		tile.ReflectionDepth.Should().BeNull();
		tile.Glow.Should().BeNull();
		tile.Visible.Should().BeTrue();
		tile.Tag.Should().BeNull();
	}

	/// <summary>All members round-trip.</summary>
	[Fact]
	public void SettableMembers_RoundTrip()
	{
		var tag = new object();

		var tile = new TileDefinition
		{
			Column = 1,
			Row = 2,
			Logo = "logo.svg",
			Color = "#123456",
			Depth = 3,
			LogoSize = 4,
			LogoRotation = 5,
			Reflection = 6,
			ReflectionDepth = 7,
			Glow = 8,
			Visible = false,
			Tag = tag
		};

		tile.Column.Should().Be(1);
		tile.Row.Should().Be(2);
		tile.Logo.Should().Be("logo.svg");
		tile.Color.Should().Be("#123456");
		tile.Depth.Should().Be(3);
		tile.LogoSize.Should().Be(4);
		tile.LogoRotation.Should().Be(5);
		tile.Reflection.Should().Be(6);
		tile.ReflectionDepth.Should().Be(7);
		tile.Glow.Should().Be(8);
		tile.Visible.Should().BeFalse();
		tile.Tag.Should().BeSameAs(tag);
	}
}
