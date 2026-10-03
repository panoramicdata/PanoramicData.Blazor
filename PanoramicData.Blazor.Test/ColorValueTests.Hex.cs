using PanoramicData.Blazor.Models.ColorPicker;
using Shouldly;

namespace PanoramicData.Blazor.Test;

/// <summary>
/// Hex parsing and formatting tests for <see cref="PanoramicData.Blazor.Models.ColorPicker.ColorValue"/>.
/// </summary>
public partial class ColorValueTests
{
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

    /// <summary>Verifies that converting a hex string to a ColorValue and back to hex preserves the original string.</summary>
    [Fact]
    public void WhenRoundTripHexThenValuesPreserved()
    {
        var original = "#3A7BDF";
        var color = ColorValue.FromHex(original);

        color.ToHex().ShouldBe(original);
    }
}
