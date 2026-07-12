using PanoramicData.Blazor.Models.ColorPicker;
using Shouldly;

namespace PanoramicData.Blazor.Test;

/// <summary>Tests for the ColorValue class.</summary>
public class ColorValueTests
{
    /// <summary>Verifies that the default constructor creates a black color with RGB (0, 0, 0) and full alpha.</summary>
    [Fact]
    public void WhenDefaultConstructorThenBlack()
    {
        var color = new ColorValue();

        color.R.ShouldBe((byte)0);
        color.G.ShouldBe((byte)0);
        color.B.ShouldBe((byte)0);
        color.A.ShouldBe(1.0);
    }

    /// <summary>Verifies that constructing a color with RGB values sets the R, G, B components and defaults alpha to 1.0.</summary>
    [Fact]
    public void WhenConstructedWithRgbThenValuesAreSet()
    {
        var color = new ColorValue(255, 128, 0);

        color.R.ShouldBe((byte)255);
        color.G.ShouldBe((byte)128);
        color.B.ShouldBe((byte)0);
        color.A.ShouldBe(1.0);
    }

    /// <summary>Verifies that a six-character hex string is parsed to the correct RGB components.</summary>
    [Fact]
    public void WhenFromHex6ThenParsesCorrectly()
    {
        var color = ColorValue.FromHex("#FF8000");

        color.R.ShouldBe((byte)255);
        color.G.ShouldBe((byte)128);
        color.B.ShouldBe((byte)0);
        color.A.ShouldBe(1.0);
    }

    /// <summary>Verifies that a three-character hex string is expanded to the correct full RGB components.</summary>
    [Fact]
    public void WhenFromHex3ThenExpandsCorrectly()
    {
        var color = ColorValue.FromHex("#F80");

        color.R.ShouldBe((byte)0xFF);
        color.G.ShouldBe((byte)0x88);
        color.B.ShouldBe((byte)0x00);
    }

    /// <summary>Verifies that an eight-character hex string including an alpha component is parsed correctly.</summary>
    [Fact]
    public void WhenFromHex8ThenIncludesAlpha()
    {
        var color = ColorValue.FromHex("#FF000080");

        color.R.ShouldBe((byte)255);
        color.G.ShouldBe((byte)0);
        color.B.ShouldBe((byte)0);
        color.A.ShouldBe(128 / 255.0, 0.01);
    }

    /// <summary>Verifies that a hex string without a leading hash character is parsed correctly.</summary>
    [Fact]
    public void WhenFromHexWithoutHashThenParsesCorrectly()
    {
        var color = ColorValue.FromHex("00FF00");

        color.R.ShouldBe((byte)0);
        color.G.ShouldBe((byte)255);
        color.B.ShouldBe((byte)0);
    }

    /// <summary>Verifies that an invalid hex string leaves the color at its default black values.</summary>
    [Fact]
    public void WhenFromHexInvalidThenKeepsDefaults()
    {
        var color = ColorValue.FromHex("not-a-color");

        color.R.ShouldBe((byte)0);
        color.G.ShouldBe((byte)0);
        color.B.ShouldBe((byte)0);
    }

    /// <summary>Verifies that an empty hex string leaves the color at its default black values.</summary>
    [Fact]
    public void WhenFromHexEmptyThenKeepsDefaults()
    {
        var color = ColorValue.FromHex("");

        color.R.ShouldBe((byte)0);
    }

    /// <summary>Verifies that ToHex formats the color as an uppercase six-character hex string.</summary>
    [Fact]
    public void WhenToHexThenFormatsCorrectly()
    {
        var color = new ColorValue(255, 128, 0);

        color.ToHex().ShouldBe("#FF8000");
    }

    /// <summary>Verifies that ToHexWithAlpha produces a nine-character hex string that includes the alpha component.</summary>
    [Fact]
    public void WhenToHexWithAlphaThenIncludesAlpha()
    {
        var color = new ColorValue(255, 0, 0, 0.5);

        var hex = color.ToHexWithAlpha();

        hex.ShouldStartWith("#FF0000");
        hex.Length.ShouldBe(9); // #RRGGBBAA
    }

    /// <summary>Verifies that ToRgb returns the CSS rgb() function string with the correct component values.</summary>
    [Fact]
    public void WhenToRgbThenFormatsCss()
    {
        var color = new ColorValue(255, 128, 0);

        color.ToRgb().ShouldBe("rgb(255, 128, 0)");
    }

    /// <summary>Verifies that ToRgba returns the CSS rgba() function string including the alpha value.</summary>
    [Fact]
    public void WhenToRgbaThenIncludesAlpha()
    {
        var color = new ColorValue(255, 128, 0, 0.5);

        color.ToRgba().ShouldBe("rgba(255, 128, 0, 0.50)");
    }

    /// <summary>Verifies that ToCss returns a hex string when the color has full opacity.</summary>
    [Fact]
    public void WhenToCssWithFullAlphaThenReturnsHex()
    {
        var color = new ColorValue(255, 0, 0);

        color.ToCss().ShouldBe("#FF0000");
    }

    /// <summary>Verifies that ToCss returns an rgba() string when the color has partial opacity.</summary>
    [Fact]
    public void WhenToCssWithPartialAlphaThenReturnsRgba()
    {
        var color = new ColorValue(255, 0, 0, 0.5);

        color.ToCss().ShouldStartWith("rgba(");
    }

    /// <summary>Verifies that cloning a color produces a copy with identical RGBA values.</summary>
    [Fact]
    public void WhenClonedThenValuesMatch()
    {
        var original = new ColorValue(100, 200, 50, 0.8);

        var clone = original.Clone();

        clone.R.ShouldBe(original.R);
        clone.G.ShouldBe(original.G);
        clone.B.ShouldBe(original.B);
        clone.A.ShouldBe(original.A);
    }

    /// <summary>Verifies that modifying a cloned color does not affect the original color's values.</summary>
    [Fact]
    public void WhenClonedThenChangingCloneDoesNotAffectOriginal()
    {
        var original = new ColorValue(100, 200, 50);

        var clone = original.Clone();
        clone.SetRgb(0, 0, 0);

        original.R.ShouldBe((byte)100);
    }

    /// <summary>Verifies that pure red has an HSV hue of 0, saturation of 1, and value of 1.</summary>
    [Fact]
    public void WhenPureRedThenHsvHueIsZero()
    {
        var color = new ColorValue(255, 0, 0);

        color.H.ShouldBe(0, 0.1);
        color.S.ShouldBe(1.0, 0.01);
        color.V.ShouldBe(1.0, 0.01);
    }

    /// <summary>Verifies that pure green has an HSV hue of 120.</summary>
    [Fact]
    public void WhenPureGreenThenHsvHueIs120()
    {
        var color = new ColorValue(0, 255, 0);

        color.H.ShouldBe(120, 0.1);
    }

    /// <summary>Verifies that pure blue has an HSV hue of 240.</summary>
    [Fact]
    public void WhenPureBlueThenHsvHueIs240()
    {
        var color = new ColorValue(0, 0, 255);

        color.H.ShouldBe(240, 0.1);
    }

    /// <summary>Verifies that white has HSV saturation of 0 and value of 1.</summary>
    [Fact]
    public void WhenWhiteThenSaturationIsZero()
    {
        var color = new ColorValue(255, 255, 255);

        color.S.ShouldBe(0, 0.01);
        color.V.ShouldBe(1.0, 0.01);
    }

    /// <summary>Verifies that black has an HSV value of 0.</summary>
    [Fact]
    public void WhenBlackThenValueIsZero()
    {
        var color = new ColorValue(0, 0, 0);

        color.V.ShouldBe(0, 0.01);
    }

    /// <summary>Verifies that creating a color from HSV (0, 1, 1) produces pure red with R=255, G=0, B=0.</summary>
    [Fact]
    public void WhenFromHsvRedThenRgbIsCorrect()
    {
        var color = ColorValue.FromHsv(0, 1, 1);

        color.R.ShouldBe((byte)255);
        color.G.ShouldBe((byte)0);
        color.B.ShouldBe((byte)0);
    }

    /// <summary>Verifies that converting a hex string to a ColorValue and back to hex preserves the original string.</summary>
    [Fact]
    public void WhenRoundTripHexThenValuesPreserved()
    {
        var original = "#3A7BDF";
        var color = ColorValue.FromHex(original);

        color.ToHex().ShouldBe(original);
    }

    /// <summary>Verifies that converting RGB to HSV and back to RGB preserves the original component values.</summary>
    [Fact]
    public void WhenRoundTripHsvThenRgbPreserved()
    {
        var color = new ColorValue(123, 45, 67);
        var h = color.H;
        var s = color.S;
        var v = color.V;

        var roundTrip = ColorValue.FromHsv(h, s, v);

        roundTrip.R.ShouldBe(color.R);
        roundTrip.G.ShouldBe(color.G);
        roundTrip.B.ShouldBe(color.B);
    }

    /// <summary>Verifies that two colors with identical RGBA values are considered equal.</summary>
    [Fact]
    public void WhenEqualColorsThenEqualsReturnsTrue()
    {
        var a = new ColorValue(100, 200, 50, 0.8);
        var b = new ColorValue(100, 200, 50, 0.8);

        a.Equals(b).ShouldBeTrue();
    }

    /// <summary>Verifies that two colors with differing component values are not considered equal.</summary>
    [Fact]
    public void WhenDifferentColorsThenEqualsReturnsFalse()
    {
        var a = new ColorValue(100, 200, 50);
        var b = new ColorValue(100, 200, 51);

        a.Equals(b).ShouldBeFalse();
    }

    /// <summary>Verifies that setting an alpha value greater than 1.0 clamps it to 1.0.</summary>
    [Fact]
    public void WhenAlphaClamped_ThenStaysInRange()
    {
        var color = new ColorValue(0, 0, 0, 5.0);

        color.A.ShouldBe(1.0);
    }
}
