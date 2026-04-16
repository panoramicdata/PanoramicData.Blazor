using PanoramicData.Blazor.Extensions;
using Shouldly;

namespace PanoramicData.Blazor.Test;

public class ColorExtensionsTests
{
    [Fact]
    public void WhenInterpolatingAtZeroThenReturnsFirstColor()
    {
        var result = ColorExtensions.Interpolate("#000000", "#FFFFFF", 0.0);

        result.ShouldBe("#000000");
    }

    [Fact]
    public void WhenInterpolatingAtOneThenReturnsSecondColor()
    {
        var result = ColorExtensions.Interpolate("#000000", "#FFFFFF", 1.0);

        result.ShouldBe("#FFFFFF");
    }

    [Fact]
    public void WhenInterpolatingAtHalfThenReturnsMidpoint()
    {
        var result = ColorExtensions.Interpolate("#000000", "#FFFFFF", 0.5);

        // 127 or 128 depending on rounding - either is valid midpoint
        result.ShouldBeOneOf("#7F7F7F", "#808080");
    }

    [Fact]
    public void WhenInvalidColorThenReturnsFirstColor()
    {
        var result = ColorExtensions.Interpolate("#FF0000", "not-a-color", 0.5);

        result.ShouldBe("#FF0000");
    }
}
