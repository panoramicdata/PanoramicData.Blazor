using AwesomeAssertions;
using PanoramicData.Blazor.Models.Tiles;

namespace PanoramicData.Blazor.Test.Models.Tiles;

/// <summary>Tests for <see cref="TileGridOptions"/>.</summary>
public class TileGridOptionsTests
{
	/// <summary>New options describe a fully populated three by three grid, aligned middle right.</summary>
	[Fact]
	public void New_HasDocumentedDefaults()
	{
		var options = new TileGridOptions();

		options.Columns.Should().Be(3);
		options.Rows.Should().Be(3);
		options.Depth.Should().Be(15);
		options.LogoSize.Should().Be(80);
		options.LogoRotation.Should().Be(0);
		options.Gap.Should().Be(100);
		options.Population.Should().Be(100);
		options.TileColor.Should().Be("#373737");
		options.BackgroundColor.Should().Be("#000624");
		options.ShowBackground.Should().BeTrue();
		options.LineColor.Should().Be("#c0c0c0");
		options.LineOpacity.Should().Be(15);
		options.Glow.Should().Be(30);
		options.GlowFalloff.Should().Be(100);
		options.Reflection.Should().Be(75);
		options.ReflectionDepth.Should().Be(100);
		options.Perspective.Should().Be(0);
		options.ShowFloating.Should().BeFalse();
		options.FloatingPopulation.Should().Be(50);
		options.FloatHeight.Should().Be(80);
		options.FloatSize.Should().Be(60);
		options.Scale.Should().Be(100);
		options.Padding.Should().Be(5);
		options.Alignment.Should().Be(GridAlignment.MiddleRight);
		options.MaxGridWidthPercent.Should().BeNull();
		options.MaxGridHeightPercent.Should().BeNull();
		options.ContentWrapping.Should().BeFalse();
	}

	/// <summary>Every settable member round-trips.</summary>
	[Fact]
	public void AllMembers_RoundTrip()
	{
		var options = new TileGridOptions();

		foreach (var property in typeof(TileGridOptions).GetProperties().Where(p => p.CanWrite))
		{
			var sample = SampleFor(property.PropertyType);

			property.SetValue(options, sample);

			property.GetValue(options).Should().Be(sample, property.Name);
		}
	}

	private static object SampleFor(Type type)
	{
		var underlying = Nullable.GetUnderlyingType(type) ?? type;
		if (underlying == typeof(string))
		{
			return "#abcdef";
		}

		if (underlying == typeof(bool))
		{
			return true;
		}

		return underlying == typeof(GridAlignment) ? GridAlignment.BottomLeft : 42;
	}
}
