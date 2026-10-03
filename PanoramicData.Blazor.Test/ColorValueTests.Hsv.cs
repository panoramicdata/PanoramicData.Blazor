using PanoramicData.Blazor.Models.ColorPicker;
using Shouldly;

namespace PanoramicData.Blazor.Test;

/// <summary>
/// HSV conversion tests for <see cref="PanoramicData.Blazor.Models.ColorPicker.ColorValue"/>.
/// </summary>
public partial class ColorValueTests
{
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
}
