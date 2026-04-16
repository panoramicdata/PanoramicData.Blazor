using PanoramicData.Blazor.Models.ColorPicker;
using Shouldly;

namespace PanoramicData.Blazor.Test;

public class ColorValueTests
{
    [Fact]
    public void WhenDefaultConstructorThenBlack()
    {
        var color = new ColorValue();

        color.R.ShouldBe((byte)0);
        color.G.ShouldBe((byte)0);
        color.B.ShouldBe((byte)0);
        color.A.ShouldBe(1.0);
    }

    [Fact]
    public void WhenConstructedWithRgbThenValuesAreSet()
    {
        var color = new ColorValue(255, 128, 0);

        color.R.ShouldBe((byte)255);
        color.G.ShouldBe((byte)128);
        color.B.ShouldBe((byte)0);
        color.A.ShouldBe(1.0);
    }

    [Fact]
    public void WhenFromHex6ThenParsesCorrectly()
    {
        var color = ColorValue.FromHex("#FF8000");

        color.R.ShouldBe((byte)255);
        color.G.ShouldBe((byte)128);
        color.B.ShouldBe((byte)0);
        color.A.ShouldBe(1.0);
    }

    [Fact]
    public void WhenFromHex3ThenExpandsCorrectly()
    {
        var color = ColorValue.FromHex("#F80");

        color.R.ShouldBe((byte)0xFF);
        color.G.ShouldBe((byte)0x88);
        color.B.ShouldBe((byte)0x00);
    }

    [Fact]
    public void WhenFromHex8ThenIncludesAlpha()
    {
        var color = ColorValue.FromHex("#FF000080");

        color.R.ShouldBe((byte)255);
        color.G.ShouldBe((byte)0);
        color.B.ShouldBe((byte)0);
        color.A.ShouldBe(128 / 255.0, 0.01);
    }

    [Fact]
    public void WhenFromHexWithoutHashThenParsesCorrectly()
    {
        var color = ColorValue.FromHex("00FF00");

        color.R.ShouldBe((byte)0);
        color.G.ShouldBe((byte)255);
        color.B.ShouldBe((byte)0);
    }

    [Fact]
    public void WhenFromHexInvalidThenKeepsDefaults()
    {
        var color = ColorValue.FromHex("not-a-color");

        color.R.ShouldBe((byte)0);
        color.G.ShouldBe((byte)0);
        color.B.ShouldBe((byte)0);
    }

    [Fact]
    public void WhenFromHexEmptyThenKeepsDefaults()
    {
        var color = ColorValue.FromHex("");

        color.R.ShouldBe((byte)0);
    }

    [Fact]
    public void WhenToHexThenFormatsCorrectly()
    {
        var color = new ColorValue(255, 128, 0);

        color.ToHex().ShouldBe("#FF8000");
    }

    [Fact]
    public void WhenToHexWithAlphaThenIncludesAlpha()
    {
        var color = new ColorValue(255, 0, 0, 0.5);

        var hex = color.ToHexWithAlpha();

        hex.ShouldStartWith("#FF0000");
        hex.Length.ShouldBe(9); // #RRGGBBAA
    }

    [Fact]
    public void WhenToRgbThenFormatsCss()
    {
        var color = new ColorValue(255, 128, 0);

        color.ToRgb().ShouldBe("rgb(255, 128, 0)");
    }

    [Fact]
    public void WhenToRgbaThenIncludesAlpha()
    {
        var color = new ColorValue(255, 128, 0, 0.5);

        color.ToRgba().ShouldBe("rgba(255, 128, 0, 0.50)");
    }

    [Fact]
    public void WhenToCssWithFullAlphaThenReturnsHex()
    {
        var color = new ColorValue(255, 0, 0);

        color.ToCss().ShouldBe("#FF0000");
    }

    [Fact]
    public void WhenToCssWithPartialAlphaThenReturnsRgba()
    {
        var color = new ColorValue(255, 0, 0, 0.5);

        color.ToCss().ShouldStartWith("rgba(");
    }

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

    [Fact]
    public void WhenClonedThenChangingCloneDoesNotAffectOriginal()
    {
        var original = new ColorValue(100, 200, 50);

        var clone = original.Clone();
        clone.SetRgb(0, 0, 0);

        original.R.ShouldBe((byte)100);
    }

    [Fact]
    public void WhenPureRedThenHsvHueIsZero()
    {
        var color = new ColorValue(255, 0, 0);

        color.H.ShouldBe(0, 0.1);
        color.S.ShouldBe(1.0, 0.01);
        color.V.ShouldBe(1.0, 0.01);
    }

    [Fact]
    public void WhenPureGreenThenHsvHueIs120()
    {
        var color = new ColorValue(0, 255, 0);

        color.H.ShouldBe(120, 0.1);
    }

    [Fact]
    public void WhenPureBlueThenHsvHueIs240()
    {
        var color = new ColorValue(0, 0, 255);

        color.H.ShouldBe(240, 0.1);
    }

    [Fact]
    public void WhenWhiteThenSaturationIsZero()
    {
        var color = new ColorValue(255, 255, 255);

        color.S.ShouldBe(0, 0.01);
        color.V.ShouldBe(1.0, 0.01);
    }

    [Fact]
    public void WhenBlackThenValueIsZero()
    {
        var color = new ColorValue(0, 0, 0);

        color.V.ShouldBe(0, 0.01);
    }

    [Fact]
    public void WhenFromHsvRedThenRgbIsCorrect()
    {
        var color = ColorValue.FromHsv(0, 1, 1);

        color.R.ShouldBe((byte)255);
        color.G.ShouldBe((byte)0);
        color.B.ShouldBe((byte)0);
    }

    [Fact]
    public void WhenRoundTripHexThenValuesPreserved()
    {
        var original = "#3A7BDF";
        var color = ColorValue.FromHex(original);

        color.ToHex().ShouldBe(original);
    }

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

    [Fact]
    public void WhenEqualColorsThenEqualsReturnsTrue()
    {
        var a = new ColorValue(100, 200, 50, 0.8);
        var b = new ColorValue(100, 200, 50, 0.8);

        a.Equals(b).ShouldBeTrue();
    }

    [Fact]
    public void WhenDifferentColorsThenEqualsReturnsFalse()
    {
        var a = new ColorValue(100, 200, 50);
        var b = new ColorValue(100, 200, 51);

        a.Equals(b).ShouldBeFalse();
    }

    [Fact]
    public void WhenAlphaClamped_ThenStaysInRange()
    {
        var color = new ColorValue(0, 0, 0, 5.0);

        color.A.ShouldBe(1.0);
    }
}
