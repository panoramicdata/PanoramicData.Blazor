using AwesomeAssertions;
using PanoramicData.Blazor.Models.ColorPicker;

namespace PanoramicData.Blazor.Test.Models.ColorPicker;

/// <summary>
/// Tests for the colour space conversions, formatting and equality of <see cref="ColorValue"/> that the
/// original <c>ColorValueTests</c> leave uncovered.
/// </summary>
public class ColorValueTests
{
	private static void AssertRgb(ColorValue color, byte r, byte g, byte b)
	{
		color.R.Should().Be(r);
		color.G.Should().Be(g);
		color.B.Should().Be(b);
	}

	/// <summary>HSV to RGB conversion is correct in each of the six sixty-degree sectors of the hue wheel.</summary>
	[Theory]
	[InlineData(30, 255, 128, 0)]
	[InlineData(90, 128, 255, 0)]
	[InlineData(150, 0, 255, 128)]
	[InlineData(210, 0, 128, 255)]
	[InlineData(270, 128, 0, 255)]
	[InlineData(330, 255, 0, 128)]
	public void FromHsv_ConvertsEachHueSector(double hue, byte r, byte g, byte b)
	{
		var color = ColorValue.FromHsv(hue, 1, 1);

		AssertRgb(color, r, g, b);
		color.H.Should().BeApproximately(hue, 1);
	}

	/// <summary>A negative hue wraps round to the equivalent positive angle.</summary>
	[Fact]
	public void FromHsv_NegativeHue_Wraps()
	{
		var color = ColorValue.FromHsv(-90, 1, 1);

		AssertRgb(color, 128, 0, 255);
	}

	/// <summary>HSV saturation and value are clamped to zero to one, and alpha is clamped too.</summary>
	[Fact]
	public void FromHsv_ClampsInputs()
	{
		var color = ColorValue.FromHsv(0, 2, 5, 3);

		AssertRgb(color, 255, 0, 0);
		color.A.Should().Be(1);
		ColorValue.FromHsv(0, 1, 1, -1).A.Should().Be(0);
	}

	/// <summary>HSL to RGB conversion is correct across the hue wheel and for dark and light colours.</summary>
	[Theory]
	[InlineData(0, 1, 0.5, 255, 0, 0)]
	[InlineData(30, 1, 0.5, 255, 128, 0)]
	[InlineData(120, 1, 0.25, 0, 128, 0)]
	[InlineData(210, 1, 0.5, 0, 127, 255)]
	[InlineData(240, 1, 0.75, 128, 128, 255)]
	[InlineData(330, 1, 0.5, 255, 0, 128)]
	public void FromHsl_ConvertsToRgb(double h, double s, double l, byte r, byte g, byte b)
	{
		var color = ColorValue.FromHsl(h, s, l);

		AssertRgb(color, r, g, b);
		color.L.Should().BeApproximately(l, 0.01);
	}

	/// <summary>A fully desaturated HSL colour is the grey of its lightness.</summary>
	[Fact]
	public void FromHsl_ZeroSaturation_IsGrey()
	{
		var color = ColorValue.FromHsl(200, 0, 0.5, 0.25);

		AssertRgb(color, 128, 128, 128);
		color.A.Should().Be(0.25);
		color.S.Should().Be(0);
	}

	/// <summary>A negative HSL hue wraps round to the equivalent positive angle.</summary>
	[Fact]
	public void FromHsl_NegativeHue_Wraps()
	{
		AssertRgb(ColorValue.FromHsl(-360, 1, 0.5), 255, 0, 0);
		AssertRgb(ColorValue.FromHsl(-240, 1, 0.5), 0, 255, 0);
	}

	/// <summary>A four digit hex colour is expanded, including its alpha digit.</summary>
	[Fact]
	public void FromHex_FourDigits_IncludesAlpha()
	{
		var color = ColorValue.FromHex("#F80C");

		AssertRgb(color, 0xFF, 0x88, 0x00);
		color.A.Should().BeApproximately(0.8, 0.001);
	}

	/// <summary>Hex that cannot be parsed leaves the colour unchanged.</summary>
	[Theory]
	[InlineData("#GGG")]
	[InlineData("#12345")]
	[InlineData("   ")]
	public void SetFromHex_Invalid_KeepsCurrentValues(string hex)
	{
		var color = new ColorValue(10, 20, 30, 0.5);

		color.SetFromHex(hex);

		AssertRgb(color, 10, 20, 30);
		color.A.Should().Be(0.5);
	}

	/// <summary>Setting RGB recalculates the HSV and HSL values.</summary>
	[Fact]
	public void SetRgb_UpdatesHsvAndHsl()
	{
		var color = new ColorValue();

		color.SetRgb(0, 0, 255);

		color.H.Should().Be(240);
		color.S.Should().Be(1);
		color.V.Should().Be(1);
		color.L.Should().Be(0.5);
	}

	/// <summary>The HSL and HSLA representations use degrees and percentages, and the alpha to two places.</summary>
	[Fact]
	public void ToHslAndToHsla_FormatValues()
	{
		var color = new ColorValue(255, 0, 0, 0.5);

		color.ToHsl().Should().Be("hsl(0, 100%, 50%)");
		color.ToHsla().Should().Be($"hsla(0, 100%, 50%, {0.5:F2})");
	}

	/// <summary>The CSS representation is hex when opaque and rgba when translucent.</summary>
	[Fact]
	public void ToCss_DependsOnAlpha()
	{
		new ColorValue(1, 2, 3).ToCss().Should().Be("#010203");
		new ColorValue(1, 2, 3, 0.5).ToCss().Should().Be($"rgba(1, 2, 3, {0.5:F2})");
	}

	/// <summary>Equality compares the RGB components and alpha, and is not equal to null or other types.</summary>
	[Fact]
	public void Equals_ComparesComponents()
	{
		var color = new ColorValue(1, 2, 3, 0.5);
		object same = new ColorValue(1, 2, 3, 0.5);
		object other = "not a colour";

		color.Equals(new ColorValue(1, 2, 3, 0.5)).Should().BeTrue();
		color.Equals(same).Should().BeTrue();
		color.Equals(new ColorValue(1, 2, 4, 0.5)).Should().BeFalse();
		color.Equals(new ColorValue(1, 2, 3, 0.6)).Should().BeFalse();
		color.Equals(other).Should().BeFalse();
		ColorValue? missing = null;
		new ColorValue(1, 2, 3, 0.5).Equals(missing).Should().BeFalse();
	}

	/// <summary>Colours with identical components share a hash code.</summary>
	[Fact]
	public void GetHashCode_SameForIdenticalColours()
	{
		new ColorValue(9, 8, 7, 0.5).GetHashCode().Should().Be(new ColorValue(9, 8, 7, 0.5).GetHashCode());
	}

	/// <summary>The equality operators handle nulls on either side.</summary>
	[Fact]
	public void EqualityOperators_HandleNulls()
	{
		ColorValue? none = null;
		ColorValue? alsoNone = null;
		var red = new ColorValue(255, 0, 0);

		(none == alsoNone).Should().BeTrue();
		(none == red).Should().BeFalse();
		(red == none).Should().BeFalse();
		(red == new ColorValue(255, 0, 0)).Should().BeTrue();
		(red != new ColorValue(0, 0, 0)).Should().BeTrue();
		(red != new ColorValue(255, 0, 0)).Should().BeFalse();
	}
}
