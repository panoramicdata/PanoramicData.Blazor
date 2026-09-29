using AwesomeAssertions;
using PanoramicData.Blazor.Models.ColorPicker;

namespace PanoramicData.Blazor.Test.Models.ColorPicker;

/// <summary>Tests for <see cref="PaletteColor"/> and the predefined <see cref="ColorPalettes"/>.</summary>
public class PaletteColorTests
{
	/// <summary>A new palette colour is unnamed black.</summary>
	[Fact]
	public void New_IsUnnamedBlack()
	{
		var color = new PaletteColor();

		color.Color.Should().Be("#000000");
		color.Name.Should().BeNull();
		color.ToolTip.Should().BeNull();
	}

	/// <summary>The value constructor sets the colour and optional name, and every member is settable.</summary>
	[Fact]
	public void ValueConstructor_SetsColourAndName()
	{
		new PaletteColor("#FF0000").Name.Should().BeNull();

		var color = new PaletteColor("#FF0000", "Red") { ToolTip = "Danger" };

		color.Color.Should().Be("#FF0000");
		color.Name.Should().Be("Red");
		color.ToolTip.Should().Be("Danger");
	}

	/// <summary>Each palette has the expected number of colours, starting and ending where documented.</summary>
	[Theory]
	[InlineData("Basic", 16, "White", "Purple")]
	[InlineData("Material", 19, "Red", "Blue Grey")]
	[InlineData("Extended", 44, "Red", "Black")]
	[InlineData("Grayscale", 21, "White", "Black")]
	public void Palettes_HaveExpectedContents(string palette, int count, string first, string last)
	{
		var colors = Palette(palette);

		colors.Should().HaveCount(count);
		colors[0].Name.Should().Be(first);
		colors[^1].Name.Should().Be(last);
	}

	/// <summary>Every palette entry is a named, parseable six digit hex colour.</summary>
	[Theory]
	[InlineData("Basic")]
	[InlineData("Material")]
	[InlineData("Extended")]
	[InlineData("Grayscale")]
	public void Palettes_ContainValidNamedHexColours(string palette)
	{
		foreach (var color in Palette(palette))
		{
			color.Color.Should().MatchRegex("^#[0-9A-F]{6}$");
			color.Name.Should().NotBeNullOrWhiteSpace();
			ColorValue.FromHex(color.Color).ToHex().Should().Be(color.Color);
		}
	}

	/// <summary>Each read of a palette returns a fresh list, so a caller cannot alter the shared palette.</summary>
	[Fact]
	public void Palettes_ReturnFreshLists()
	{
		var first = ColorPalettes.Basic;
		first.Clear();

		ColorPalettes.Basic.Should().HaveCount(16);
	}

	private static List<PaletteColor> Palette(string name) => name switch
	{
		"Basic" => ColorPalettes.Basic,
		"Material" => ColorPalettes.Material,
		"Extended" => ColorPalettes.Extended,
		_ => ColorPalettes.Grayscale
	};
}
