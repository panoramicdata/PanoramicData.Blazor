using AwesomeAssertions;
using PanoramicData.Blazor.Helpers;
using PanoramicData.Blazor.Models;

namespace PanoramicData.Blazor.Test.Helpers;

/// <summary>Tests for <see cref="MediaMarkerNavigator"/>.</summary>
public class MediaMarkerNavigatorTests
{
	private static readonly TimeSpan _tolerance = TimeSpan.FromSeconds(0.5);

	private static PDMediaMarker At(double seconds, string kind = "a", string? id = null)
		=> new() { Id = id ?? $"{kind}{seconds}", Position = TimeSpan.FromSeconds(seconds), Kind = kind };

	private static readonly IReadOnlyList<PDMediaMarker> _sorted = MediaMarkerNavigator.Sort(
	[
		At(10),
		At(20, "b"),
		At(30),
		At(40, "b")
	]);

	private static string? IdOf(PDMediaMarker? marker) => marker?.Id;

	// a deterministic, evenly spread sequence in [0, 1): the fractional parts of multiples of the golden ratio
	private static double Spread(int i) => i * 0.6180339887498949 % 1;

	/// <summary>Sorting orders by position, keeps the given order for ties and drops nulls.</summary>
	[Fact]
	public void Sort_OrdersByPositionStablyAndDropsNulls()
	{
		var sorted = MediaMarkerNavigator.Sort([At(30, id: "late"), null, At(10, id: "first-tie"), At(10, id: "second-tie"), At(5, id: "early")]);

		sorted.Select(m => m.Id).Should().Equal("early", "first-tie", "second-tie", "late");
		MediaMarkerNavigator.Sort(null).Should().BeEmpty();
	}

	/// <summary>Filtering by a set keeps the listed kinds, ignoring case and spaces; a null set keeps everything.</summary>
	[Fact]
	public void Filter_BySet_KeepsTheVisibleKinds()
	{
		var markers = MediaMarkerNavigator.Sort([At(1, "Dead-Air"), At(2, " clipping "), At(3, "other")]);

		MediaMarkerNavigator.Filter(markers, new HashSet<string> { "dead-air", "CLIPPING" }).Select(m => m.Position.TotalSeconds)
			.Should().Equal(1, 2);
		MediaMarkerNavigator.Filter(markers, (IReadOnlySet<string>?)null).Should().BeSameAs(markers);
		MediaMarkerNavigator.Filter(markers, new HashSet<string>()).Should().BeEmpty();
	}

	/// <summary>Filtering by one kind ignores case and surrounding spaces; a null kind means markers without a kind.</summary>
	[Fact]
	public void Filter_ByKind_KeepsThatKind()
	{
		var markers = MediaMarkerNavigator.Sort([At(1), At(2, "B"), At(3, ""), new PDMediaMarker { Position = TimeSpan.FromSeconds(4), Kind = null! }]);

		MediaMarkerNavigator.Filter(markers, " b ").Select(m => m.Position.TotalSeconds).Should().Equal(2);
		MediaMarkerNavigator.Filter(markers, (string)null!).Select(m => m.Position.TotalSeconds).Should().Equal(3, 4);
	}

	/// <summary>Counts are per kind, ignoring case, ordered by kind.</summary>
	[Fact]
	public void CountByKind_CountsEachKind()
		=> MediaMarkerNavigator.CountByKind([At(1, "b"), At(2), At(3, "B"), At(4, "")])
			.Should().Equal(
				new KeyValuePair<string, int>("", 1),
				new KeyValuePair<string, int>("a", 1),
				new KeyValuePair<string, int>("b", 2));

	/// <summary>Next finds the first marker after the cursor plus the tolerance.</summary>
	[Theory]
	[InlineData(0, "a10")]
	[InlineData(9.4, "a10")]
	[InlineData(9.9, "b20")]
	[InlineData(10, "b20")]
	[InlineData(10.2, "b20")]
	[InlineData(19.6, "a30")]
	[InlineData(19.4, "b20")]
	[InlineData(40, null)]
	[InlineData(100, null)]
	public void Next_SkipsTheMarkerJustReached(double cursor, string? expected)
		=> IdOf(MediaMarkerNavigator.Next(_sorted, TimeSpan.FromSeconds(cursor), _tolerance)).Should().Be(expected);

	/// <summary>Previous returns to the start of the current marker once past the tolerance, otherwise the one before.</summary>
	[Theory]
	[InlineData(0, null)]
	[InlineData(10, null)]
	[InlineData(10.4, null)]
	[InlineData(10.6, "a10")]
	[InlineData(20, "a10")]
	[InlineData(25, "b20")]
	[InlineData(100, "b40")]
	public void Previous_RestartsOrGoesBack(double cursor, string? expected)
		=> IdOf(MediaMarkerNavigator.Previous(_sorted, TimeSpan.FromSeconds(cursor), _tolerance)).Should().Be(expected);

	/// <summary>A negative tolerance counts as zero.</summary>
	[Fact]
	public void NegativeTolerance_CountsAsZero()
	{
		IdOf(MediaMarkerNavigator.Next(_sorted, TimeSpan.FromSeconds(10), TimeSpan.FromSeconds(-5))).Should().Be("b20");
		IdOf(MediaMarkerNavigator.Previous(_sorted, TimeSpan.FromSeconds(20), TimeSpan.FromSeconds(-5))).Should().Be("a10");
	}

	/// <summary>The current marker is the last that starts at or before the cursor.</summary>
	[Theory]
	[InlineData(0, null)]
	[InlineData(10, "a10")]
	[InlineData(19.9, "a10")]
	[InlineData(45, "b40")]
	public void Current_IsTheLastStartedMarker(double cursor, string? expected)
		=> IdOf(MediaMarkerNavigator.Current(_sorted, TimeSpan.FromSeconds(cursor))).Should().Be(expected);

	/// <summary>With no markers there is nothing to find.</summary>
	[Fact]
	public void EmptyList_FindsNothing()
	{
		MediaMarkerNavigator.Next([], TimeSpan.Zero, _tolerance).Should().BeNull();
		MediaMarkerNavigator.Previous([], TimeSpan.FromHours(1), _tolerance).Should().BeNull();
		MediaMarkerNavigator.Current([], TimeSpan.FromHours(1)).Should().BeNull();
	}

	/// <summary>Lookups in a large list agree with a linear search.</summary>
	[Fact]
	public void LargeList_LookupsMatchALinearSearch()
	{
		var markers = MediaMarkerNavigator.Sort(Enumerable.Range(0, 10_000).Select(i => At(Spread(i) * 7200, id: $"m{i}")));
		for (var i = 0; i < 200; i++)
		{
			var cursor = TimeSpan.FromSeconds(Spread(i + 10_000) * 7300);
			MediaMarkerNavigator.Next(markers, cursor, _tolerance).Should().BeSameAs(markers.FirstOrDefault(m => m.Position > cursor + _tolerance));
			MediaMarkerNavigator.Previous(markers, cursor, _tolerance).Should().BeSameAs(markers.LastOrDefault(m => m.Position < cursor - _tolerance));
			MediaMarkerNavigator.Current(markers, cursor).Should().BeSameAs(markers.LastOrDefault(m => m.Position <= cursor));
		}
	}

	/// <summary>Every lookup needs a list, and GetKind needs a marker.</summary>
	[Fact]
	public void NullArguments_Throw()
	{
		new Action(() => MediaMarkerNavigator.GetKind(null!)).Should().Throw<ArgumentNullException>();
		new Action(() => MediaMarkerNavigator.Filter(null!, "a")).Should().Throw<ArgumentNullException>();
		new Action(() => MediaMarkerNavigator.Filter(null!, (IReadOnlySet<string>?)null)).Should().Throw<ArgumentNullException>();
		new Action(() => MediaMarkerNavigator.CountByKind(null!)).Should().Throw<ArgumentNullException>();
		new Action(() => MediaMarkerNavigator.Next(null!, TimeSpan.Zero, TimeSpan.Zero)).Should().Throw<ArgumentNullException>();
		new Action(() => MediaMarkerNavigator.Previous(null!, TimeSpan.Zero, TimeSpan.Zero)).Should().Throw<ArgumentNullException>();
		new Action(() => MediaMarkerNavigator.Current(null!, TimeSpan.Zero)).Should().Throw<ArgumentNullException>();
	}

	/// <summary>A marker with a positive length is a span ending after its length; otherwise it is a point.</summary>
	[Fact]
	public void PDMediaMarker_SpanAndEnd()
	{
		var point = At(5);
		var zero = new PDMediaMarker { Position = TimeSpan.FromSeconds(5), Length = TimeSpan.Zero };
		var span = new PDMediaMarker { Position = TimeSpan.FromSeconds(5), Length = TimeSpan.FromSeconds(3) };

		point.IsSpan.Should().BeFalse();
		point.End.Should().Be(TimeSpan.FromSeconds(5));
		zero.IsSpan.Should().BeFalse();
		zero.End.Should().Be(TimeSpan.FromSeconds(5));
		span.IsSpan.Should().BeTrue();
		span.End.Should().Be(TimeSpan.FromSeconds(8));
	}
}
