using AwesomeAssertions;
using PanoramicData.Blazor.Helpers;

namespace PanoramicData.Blazor.Test.Helpers;

/// <summary>Tests for <see cref="TreeMapPalette"/>.</summary>
public class TreeMapPaletteTests
{
	/// <summary>There are ten category colours.</summary>
	[Fact]
	public void CategoryCount_IsTen()
	{
		TreeMapPalette.CategoryCount.Should().Be(10);
	}

	/// <summary>A missing category key uses the first hue.</summary>
	[Theory]
	[InlineData(null)]
	[InlineData("")]
	public void ForCategory_NoKey_UsesFirstHue(string? key)
	{
		TreeMapPalette.ForCategory(key).Should().Be("hsl(210 55% 45%)");
	}

	/// <summary>A category key always maps to the same colour, drawn from the category hues.</summary>
	[Fact]
	public void ForCategory_IsStableAndFromPalette()
	{
		var colour = TreeMapPalette.ForCategory("Documents");

		TreeMapPalette.ForCategory("Documents").Should().Be(colour);
		colour.Should().MatchRegex(@"^hsl\((210|25|145|280|45|190|330|95|255|15) 55% 45%\)$");
	}

	/// <summary>Different keys spread across more than one colour.</summary>
	[Fact]
	public void ForCategory_SpreadsKeys()
	{
		var colours = Enumerable.Range(0, 50).Select(i => TreeMapPalette.ForCategory($"key{i}")).Distinct();

		colours.Should().HaveCountGreaterThan(3);
	}

	/// <summary>Depth shading runs from dark at the root to light at the maximum depth, clamped beyond it.</summary>
	[Theory]
	[InlineData(0, 4, "hsl(210 48% 32%)")]
	[InlineData(2, 4, "hsl(210 48% 51%)")]
	[InlineData(4, 4, "hsl(210 48% 70%)")]
	[InlineData(9, 4, "hsl(210 48% 70%)")]
	[InlineData(-1, 4, "hsl(210 48% 32%)")]
	[InlineData(1, 0, "hsl(210 48% 70%)")]
	public void ForDepth_ShadesByDepth(int depth, int maximumDepth, string expected)
	{
		TreeMapPalette.ForDepth(depth, maximumDepth).Should().Be(expected);
	}

	/// <summary>Heat runs from blue at the minimum to red at the maximum, clamped outside the range.</summary>
	[Theory]
	[InlineData(0, "hsl(210 62% 45%)")]
	[InlineData(50, "hsl(105 62% 45%)")]
	[InlineData(100, "hsl(0 62% 45%)")]
	[InlineData(150, "hsl(0 62% 45%)")]
	[InlineData(-10, "hsl(210 62% 45%)")]
	public void ForHeat_RunsBlueToRed(double value, string expected)
	{
		TreeMapPalette.ForHeat(value, 0, 100).Should().Be(expected);
	}

	/// <summary>An unusable value or an empty range falls back to the coolest colour.</summary>
	[Theory]
	[InlineData(double.NaN, 0, 100)]
	[InlineData(double.PositiveInfinity, 0, 100)]
	[InlineData(5, 10, 10)]
	[InlineData(5, 10, 0)]
	public void ForHeat_UnusableInput_IsCool(double value, double minimum, double maximum)
	{
		TreeMapPalette.ForHeat(value, minimum, maximum).Should().Be("hsl(210 62% 45%)");
	}

	/// <summary>The fallback colour is a muted blue.</summary>
	[Fact]
	public void Fallback_IsMutedBlue()
	{
		TreeMapPalette.Fallback().Should().Be("hsl(210 30% 45%)");
	}
}
