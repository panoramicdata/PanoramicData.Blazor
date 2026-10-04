using PanoramicData.Blazor.Models.ColorPicker;
using Shouldly;

namespace PanoramicData.Blazor.Test;

/// <summary>Tests for the ColorValue class.</summary>
public partial class ColorValueTests
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
