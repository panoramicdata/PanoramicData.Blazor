using AwesomeAssertions;
using PanoramicData.Blazor.Helpers;

namespace PanoramicData.Blazor.Test.Helpers;

/// <summary>Tests for <see cref="MediaPlaybackRates"/>.</summary>
public class MediaPlaybackRatesTests
{
	/// <summary>The default list is 0.75 to 2 in quarter steps.</summary>
	[Fact]
	public void Default_IsTheDocumentedList()
		=> MediaPlaybackRates.Default.Should().Equal(0.75, 1, 1.25, 1.5, 1.75, 2);

	/// <summary>Rates are limited to 0.5 to 4 and rounded; a rate that is not a finite number becomes 1.</summary>
	[Theory]
	[InlineData(0.1, 0.5)]
	[InlineData(9, 4)]
	[InlineData(1.333, 1.33)]
	[InlineData(double.NaN, 1)]
	[InlineData(double.PositiveInfinity, 1)]
	[InlineData(2, 2)]
	public void Clamp_LimitsTheRate(double rate, double expected)
		=> MediaPlaybackRates.Clamp(rate).Should().Be(expected);

	/// <summary>Out-of-range, non-finite and duplicate rates are dropped and the rest sorted.</summary>
	[Fact]
	public void Normalize_CleansTheList()
		=> MediaPlaybackRates.Normalize([2, 0.25, 1, double.NaN, 1.0001, 5, 0.5, 4])
			.Should().Equal(0.5, 1, 2, 4);

	/// <summary>A missing list, or one with nothing valid in it, gives the default list.</summary>
	[Fact]
	public void Normalize_EmptyOrMissing_GivesTheDefault()
	{
		MediaPlaybackRates.Normalize(null).Should().BeSameAs(MediaPlaybackRates.Default);
		MediaPlaybackRates.Normalize([]).Should().BeSameAs(MediaPlaybackRates.Default);
		MediaPlaybackRates.Normalize([0.1, 10]).Should().BeSameAs(MediaPlaybackRates.Default);
	}

	/// <summary>Stepping finds the neighbouring rate, from a rate in the list or between two, and stops at the ends.</summary>
	[Theory]
	[InlineData(1, true, 1.25)]
	[InlineData(1, false, 0.75)]
	[InlineData(1.1, true, 1.25)]
	[InlineData(1.1, false, 1)]
	[InlineData(2, true, 2)]
	[InlineData(0.75, false, 0.75)]
	public void Step_FindsTheNeighbouringRate(double current, bool faster, double expected)
		=> MediaPlaybackRates.Step(MediaPlaybackRates.Default, current, faster).Should().Be(expected);

	/// <summary>Step needs a list.</summary>
	[Fact]
	public void Step_WithoutAList_Throws()
	{
		var act = () => MediaPlaybackRates.Step(null!, 1, true);

		act.Should().Throw<ArgumentNullException>();
	}

	/// <summary>Labels use the invariant culture and a times sign.</summary>
	[Theory]
	[InlineData(1, "1×")]
	[InlineData(1.25, "1.25×")]
	[InlineData(0.5, "0.5×")]
	public void FormatLabel_ShowsTheRate(double rate, string expected)
		=> MediaPlaybackRates.FormatLabel(rate).Should().Be(expected);
}
