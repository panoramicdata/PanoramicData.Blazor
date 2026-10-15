using AwesomeAssertions;
using PanoramicData.Blazor.Helpers;
using PanoramicData.Blazor.Models;

namespace PanoramicData.Blazor.Test.Helpers;

/// <summary>Tests for <see cref="MediaTimelineLayout"/>.</summary>
public class MediaTimelineLayoutTests
{
	private static readonly TimeSpan _minute = TimeSpan.FromMinutes(1);

	private static PDMediaMarker At(double seconds, string kind = "a", double? length = null, string? color = null, string? css = null, string? label = null)
		=> new()
		{
			Id = $"{kind}{seconds}",
			Position = TimeSpan.FromSeconds(seconds),
			Length = length is { } l ? TimeSpan.FromSeconds(l) : null,
			Kind = kind,
			Color = color,
			CssClass = css,
			Label = label
		};

	private static IReadOnlyList<MediaMarkerTick> Ticks(IEnumerable<PDMediaMarker> markers, TimeSpan? duration, int resolution = 100)
		=> MediaTimelineLayout.GetMarkerTicks(MediaMarkerNavigator.Sort(markers), duration, resolution, m => m.Color ?? "red");

	/// <summary>Percentages are limited to 0 to 100 and are 0 without a positive duration.</summary>
	[Theory]
	[InlineData(30, 60, 50)]
	[InlineData(-5, 60, 0)]
	[InlineData(90, 60, 100)]
	[InlineData(10, 0, 0)]
	public void ToPercent_IsLimited(double position, double duration, double expected)
		=> MediaTimelineLayout.ToPercent(TimeSpan.FromSeconds(position), TimeSpan.FromSeconds(duration)).Should().Be(expected);

	/// <summary>Positions are limited to the media: never negative, and not past a known end.</summary>
	[Fact]
	public void ClampPosition_LimitsToTheMedia()
	{
		MediaTimelineLayout.ClampPosition(TimeSpan.FromSeconds(-1), _minute).Should().Be(TimeSpan.Zero);
		MediaTimelineLayout.ClampPosition(TimeSpan.FromSeconds(90), _minute).Should().Be(_minute);
		MediaTimelineLayout.ClampPosition(TimeSpan.FromSeconds(90), null).Should().Be(TimeSpan.FromSeconds(90));
		MediaTimelineLayout.ClampPosition(TimeSpan.FromSeconds(30), _minute).Should().Be(TimeSpan.FromSeconds(30));
	}

	/// <summary>Buffered pairs become segments; empty, inverted, non-finite and odd trailing values are skipped.</summary>
	[Fact]
	public void GetBufferedSegments_ConvertsPairs()
	{
		var segments = MediaTimelineLayout.GetBufferedSegments([0, 15, 30, 30, double.NaN, 40, 45, 90, 50], _minute);

		segments.Should().Equal(new MediaBarSegment(0, 25), new MediaBarSegment(75, 25));
		MediaTimelineLayout.GetBufferedSegments(null, _minute).Should().BeEmpty();
		MediaTimelineLayout.GetBufferedSegments([0, 10], null).Should().BeEmpty();
		MediaTimelineLayout.GetBufferedSegments([0, 10], TimeSpan.Zero).Should().BeEmpty();
	}

	/// <summary>The shaded range defaults its missing end to the media's ends and is absent when empty or unknown.</summary>
	[Fact]
	public void GetRangeSegment_ShadesTheRange()
	{
		MediaTimelineLayout.GetRangeSegment(TimeSpan.FromSeconds(6), TimeSpan.FromSeconds(54), _minute).Should().Be(new MediaBarSegment(10, 80));
		MediaTimelineLayout.GetRangeSegment(TimeSpan.FromSeconds(30), null, _minute).Should().Be(new MediaBarSegment(50, 50));
		MediaTimelineLayout.GetRangeSegment(null, TimeSpan.FromSeconds(15), _minute).Should().Be(new MediaBarSegment(0, 25));
		MediaTimelineLayout.GetRangeSegment(null, null, _minute).Should().BeNull();
		MediaTimelineLayout.GetRangeSegment(TimeSpan.FromSeconds(30), TimeSpan.FromSeconds(10), _minute).Should().BeNull();
		MediaTimelineLayout.GetRangeSegment(TimeSpan.FromSeconds(30), null, null).Should().BeNull();
		MediaTimelineLayout.GetRangeSegment(TimeSpan.FromSeconds(30), null, TimeSpan.Zero).Should().BeNull();
	}

	/// <summary>A range includes its start and excludes its end; with no range the answer is null.</summary>
	[Theory]
	[InlineData(4.9, 5d, 55d, false)]
	[InlineData(5, 5d, 55d, true)]
	[InlineData(54.9, 5d, 55d, true)]
	[InlineData(55, 5d, 55d, false)]
	[InlineData(0, null, 55d, true)]
	[InlineData(500, 5d, null, true)]
	[InlineData(1, 5d, null, false)]
	[InlineData(1, null, null, null)]
	public void IsInRange_IncludesStartExcludesEnd(double position, double? start, double? end, bool? expected)
		=> MediaTimelineLayout.IsInRange(
				TimeSpan.FromSeconds(position),
				start is { } s ? TimeSpan.FromSeconds(s) : null,
				end is { } e ? TimeSpan.FromSeconds(e) : null)
			.Should().Be(expected);

	/// <summary>Without markers or a known duration nothing is drawn.</summary>
	[Fact]
	public void GetMarkerTicks_NothingToDraw()
	{
		Ticks([], _minute).Should().BeEmpty();
		Ticks([At(1)], null).Should().BeEmpty();
		Ticks([At(1)], TimeSpan.Zero).Should().BeEmpty();
	}

	/// <summary>A point marker is a zero-width tick at its position; a span covers its length.</summary>
	[Fact]
	public void GetMarkerTicks_PointsAndSpans()
	{
		var ticks = Ticks([At(15), At(30, "b", length: 6, css: "loud", label: "x")], _minute);

		ticks.Should().HaveCount(2);
		ticks[0].Should().Match<MediaMarkerTick>(t => t.LeftPercent == 25 && t.WidthPercent == 0 && !t.IsSpan && t.Count == 1 && t.Kind == "a");
		ticks[1].Should().Match<MediaMarkerTick>(t => t.LeftPercent == 50 && t.WidthPercent == 10 && t.IsSpan && t.CssClass == "loud");
	}

	/// <summary>Markers of one kind in the same slot merge; other kinds, colours or classes in that slot do not.</summary>
	[Fact]
	public void GetMarkerTicks_MergesNeighboursInTheSameSlot()
	{
		// 100 slots over a minute: a slot is 0.6 s
		var ticks = Ticks([At(30.0), At(30.1), At(30.2), At(30.1, "b"), At(30.1, color: "blue"), At(45)], _minute);

		ticks.Should().HaveCount(4);
		var merged = ticks.Single(t => t.Count == 3);
		merged.Start.Should().Be(TimeSpan.FromSeconds(30));
		merged.End.Should().Be(TimeSpan.FromSeconds(30.2));
		merged.First.Id.Should().Be("a30");
		ticks.Sum(t => t.Count).Should().Be(6);
	}

	/// <summary>Overlapping spans of a kind merge into one span that reaches the latest end.</summary>
	[Fact]
	public void GetMarkerTicks_MergesOverlappingSpans()
	{
		var ticks = Ticks([At(10, length: 10), At(15, length: 20), At(40)], _minute);

		ticks.Should().HaveCount(2);
		ticks[0].Count.Should().Be(2);
		ticks[0].IsSpan.Should().BeTrue();
		ticks[0].End.Should().Be(TimeSpan.FromSeconds(35));
		ticks[1].IsSpan.Should().BeFalse();
	}

	/// <summary>A point merged into a span makes the tick a span; a span merged into a point does too.</summary>
	[Fact]
	public void GetMarkerTicks_PointThenSpanInOneSlotIsASpan()
		=> Ticks([At(30), At(30.1, length: 5)], _minute).Single().IsSpan.Should().BeTrue();

	/// <summary>Markers past the end are drawn at the end, and negative ones at the start.</summary>
	[Fact]
	public void GetMarkerTicks_ClampsToTheMedia()
	{
		var ticks = Ticks([At(-5), At(90, "b"), At(55, "c", length: 30)], _minute);

		ticks.Single(t => t.Kind == "a").LeftPercent.Should().Be(0);
		ticks.Single(t => t.Kind == "b").LeftPercent.Should().Be(100);
		var span = ticks.Single(t => t.Kind == "c");
		(span.LeftPercent + span.WidthPercent).Should().BeApproximately(100, 1e-9);
	}

	/// <summary>Thousands of markers are drawn as at most one tick per slot per kind.</summary>
	[Fact]
	public void GetMarkerTicks_BoundsTheTickCount()
	{
		var markers = Enumerable.Range(0, 10_000).Select(i => At(i * 0.36, i % 2 == 0 ? "even" : "odd"));

		var ticks = Ticks(markers, TimeSpan.FromHours(1), 400);

		ticks.Count.Should().BeLessThanOrEqualTo(800);
		ticks.Sum(t => t.Count).Should().Be(10_000);
	}

	/// <summary>A resolution below one counts as one slot.</summary>
	[Fact]
	public void GetMarkerTicks_ResolutionBelowOne_IsOneSlot()
		=> Ticks([At(1), At(59)], _minute, 0).Should().ContainSingle().Which.Count.Should().Be(2);

	/// <summary>The seek step is a tenth of a second below a minute, otherwise a second.</summary>
	[Fact]
	public void GetSeekStep_DependsOnTheDuration()
	{
		MediaTimelineLayout.GetSeekStep(TimeSpan.FromSeconds(30)).Should().Be(0.1);
		MediaTimelineLayout.GetSeekStep(TimeSpan.FromMinutes(5)).Should().Be(1);
		MediaTimelineLayout.GetSeekStep(null).Should().Be(1);
	}

	/// <summary>Update rates are limited to 1 to 30 a second; not-a-number gives the default of 4.</summary>
	[Theory]
	[InlineData(4, 250)]
	[InlineData(0, 1000)]
	[InlineData(100, 33)]
	[InlineData(double.NaN, 250)]
	public void GetUpdateIntervalMilliseconds_LimitsTheRate(double rate, int expected)
		=> MediaTimelineLayout.GetUpdateIntervalMilliseconds(rate).Should().Be(expected);

	/// <summary>Colours that could break out of a style attribute are refused.</summary>
	[Theory]
	[InlineData("#ff0000", "#ff0000")]
	[InlineData(" rgb(1, 2, 3) ", "rgb(1, 2, 3)")]
	[InlineData("hsl(10 50% 50%)", "hsl(10 50% 50%)")]
	[InlineData("var(--bs-danger)", "var(--bs-danger)")]
	[InlineData("red; background: url(x)", null)]
	[InlineData("url(http://x)", null)]
	[InlineData("url(x)", null)]
	[InlineData("rebeccapurple", "rebeccapurple")]
	[InlineData("rgb(1 2 3 / 50%)", "rgb(1 2 3 / 50%)")]
	[InlineData("", null)]
	[InlineData(null, null)]
	public void SanitizeColor_AllowsOnlyPlainColours(string? color, string? expected)
		=> MediaTimelineLayout.SanitizeColor(color).Should().Be(expected);

	/// <summary>Tooltips give the time and label of one marker, or the count and time span of merged markers.</summary>
	[Fact]
	public void DescribeTick_DescribesOneOrMany()
	{
		var single = Ticks([At(65, label: "Gap")], TimeSpan.FromMinutes(2))[0];
		var unlabelled = Ticks([At(65)], TimeSpan.FromMinutes(2))[0];
		var span = Ticks([At(65, length: 5)], TimeSpan.FromMinutes(2))[0];
		var merged = Ticks([At(65), At(65.5)], TimeSpan.FromMinutes(2))[0];

		MediaTimelineLayout.DescribeTick(single, "Dead air", TimeSpan.FromMinutes(2)).Should().Be("1:05 Dead air: Gap");
		MediaTimelineLayout.DescribeTick(unlabelled, "Dead air", TimeSpan.FromMinutes(2)).Should().Be("1:05 Dead air");
		MediaTimelineLayout.DescribeTick(span, "Dead air", TimeSpan.FromMinutes(2)).Should().Be("1:05 to 1:10 Dead air");
		MediaTimelineLayout.DescribeTick(merged, "Dead air", TimeSpan.FromMinutes(2)).Should().Be("2 Dead air markers, 1:05 to 1:05");
	}
}
