using PanoramicData.Blazor.Extensions;
using Shouldly;

namespace PanoramicData.Blazor.Test;

/// <summary>Tests for the ColorExtensions extension methods.</summary>
public class ColorExtensionsTests
{
    /// <summary>Verifies that interpolating at position 0.0 returns the first color unchanged.</summary>
    [Fact]
    public void WhenInterpolatingAtZeroThenReturnsFirstColor()
    {
        var result = ColorExtensions.Interpolate("#000000", "#FFFFFF", 0.0);

        result.ShouldBe("#000000");
    }

    /// <summary>Verifies that interpolating at position 1.0 returns the second color unchanged.</summary>
    [Fact]
    public void WhenInterpolatingAtOneThenReturnsSecondColor()
    {
        var result = ColorExtensions.Interpolate("#000000", "#FFFFFF", 1.0);

        result.ShouldBe("#FFFFFF");
    }

    /// <summary>Verifies that interpolating at position 0.5 returns a color at the midpoint between the two inputs.</summary>
    [Fact]
    public void WhenInterpolatingAtHalfThenReturnsMidpoint()
    {
        var result = ColorExtensions.Interpolate("#000000", "#FFFFFF", 0.5);

        // 127 or 128 depending on rounding - either is valid midpoint
        result.ShouldBeOneOf("#7F7F7F", "#808080");
    }

    /// <summary>Verifies that when the second color string is invalid, interpolation returns the first color.</summary>
    [Fact]
    public void WhenInvalidColorThenReturnsFirstColor()
    {
        var result = ColorExtensions.Interpolate("#FF0000", "not-a-color", 0.5);

        result.ShouldBe("#FF0000");
    }
}
