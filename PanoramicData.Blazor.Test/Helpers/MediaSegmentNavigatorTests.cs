using AwesomeAssertions;
using PanoramicData.Blazor.Helpers;
using PanoramicData.Blazor.Models;

namespace PanoramicData.Blazor.Test.Helpers;

/// <summary>Tests for <see cref="MediaSegmentNavigator"/> and <see cref="PDMediaSegment"/>.</summary>
public class MediaSegmentNavigatorTests
{
	private static readonly TimeSpan _tolerance = TimeSpan.FromSeconds(0.5);

	private static PDMediaSegment Seg(double start, double end, string kind = "song", string? id = null)
		=> new() { Id = id ?? $"{kind}{start}", Start = TimeSpan.FromSeconds(start), End = TimeSpan.FromSeconds(end), Kind = kind };

	private static readonly IReadOnlyList<PDMediaSegment> _sorted = MediaSegmentNavigator.Sort(
	[
		Seg(0, 60),
		Seg(50, 70, "talk"),
		Seg(60, 120),
		Seg(130, 200, "talk")
	]);

	private static string?[] Ids(IEnumerable<PDMediaSegment?> segments) => [.. segments.Select(s => s?.Id)];

	/// <summary>Sorting orders by start then end, keeps ties in order, and drops nulls and empty or inverted segments.</summary>
	[Fact]
	public void Sort_OrdersAndDropsInvalidSegments()
	{
		var sorted = MediaSegmentNavigator.Sort(
		[
			Seg(10, 30, id: "long"),
			null,
			Seg(10, 20, id: "short"),
			Seg(5, 5, id: "empty"),
			Seg(8, 2, id: "inverted"),
			Seg(10, 20, id: "short-tie"),
			Seg(0, 1, id: "first")
		]);

		Ids(sorted).Should().Equal("first", "short", "short-tie", "long");
		MediaSegmentNavigator.Sort(null).Should().BeEmpty();
	}

	/// <summary>Filtering keeps the listed kinds (ignoring case), or one kind, or everything for a null set.</summary>
	[Fact]
	public void Filter_KeepsTheVisibleKinds()
	{
		Ids(MediaSegmentNavigator.Filter(_sorted, new HashSet<string> { "TALK" })).Should().Equal("talk50", "talk130");
		MediaSegmentNavigator.Filter(_sorted, (IReadOnlySet<string>?)null).Should().BeSameAs(_sorted);
		Ids(MediaSegmentNavigator.Filter(_sorted, " Song ")).Should().Equal("song0", "song60");
	}

	/// <summary>Counts are per kind, ordered by kind.</summary>
	[Fact]
	public void CountByKind_CountsEachKind()
		=> MediaSegmentNavigator.CountByKind(_sorted).Should().Equal(
			new KeyValuePair<string, int>("song", 2),
			new KeyValuePair<string, int>("talk", 2));

	/// <summary>Next and previous work on segment starts, with the tolerance.</summary>
	[Theory]
	[InlineData(0, "talk50", null)]
	[InlineData(49.6, "song60", "song0")]
	[InlineData(55, "song60", "talk50")]
	[InlineData(130.2, null, "song60")]
	[InlineData(131, null, "talk130")]
	public void NextAndPrevious_UseTheStarts(double cursor, string? next, string? previous)
	{
		(MediaSegmentNavigator.Next(_sorted, TimeSpan.FromSeconds(cursor), _tolerance)?.Id).Should().Be(next);
		(MediaSegmentNavigator.Previous(_sorted, TimeSpan.FromSeconds(cursor), _tolerance)?.Id).Should().Be(previous);
	}

	/// <summary>Active segments contain the position, start included and end excluded, overlaps included.</summary>
	[Theory]
	[InlineData(0, new[] { "song0" })]
	[InlineData(55, new[] { "song0", "talk50" })]
	[InlineData(60, new[] { "talk50", "song60" })]
	[InlineData(125, new string[0])]
	[InlineData(200, new string[0])]
	public void Active_FindsContainingSegments(double position, string[] expected)
		=> Ids(MediaSegmentNavigator.Active(_sorted, TimeSpan.FromSeconds(position))).Should().Equal(expected);

	/// <summary>The active line is the last started line that has not ended.</summary>
	[Fact]
	public void ActiveLine_FindsTheLineBeingSpoken()
	{
		var first = new PDMediaTextLine { Start = TimeSpan.FromSeconds(10), Text = "one" };
		var second = new PDMediaTextLine { Start = TimeSpan.FromSeconds(20), End = TimeSpan.FromSeconds(25), Text = "two" };
		PDMediaTextLine[] lines = [second, first];

		MediaSegmentNavigator.ActiveLine(lines, TimeSpan.FromSeconds(5)).Should().BeNull();
		MediaSegmentNavigator.ActiveLine(lines, TimeSpan.FromSeconds(15)).Should().BeSameAs(first);
		MediaSegmentNavigator.ActiveLine(lines, TimeSpan.FromSeconds(22)).Should().BeSameAs(second);
		MediaSegmentNavigator.ActiveLine(lines, TimeSpan.FromSeconds(25)).Should().BeNull();
		MediaSegmentNavigator.ActiveLine(null, TimeSpan.FromSeconds(15)).Should().BeNull();
		MediaSegmentNavigator.ActiveLine([], TimeSpan.FromSeconds(15)).Should().BeNull();
	}

	/// <summary>Every lookup needs a list, and GetKind needs a segment.</summary>
	[Fact]
	public void NullArguments_Throw()
	{
		new Action(() => MediaSegmentNavigator.GetKind(null!)).Should().Throw<ArgumentNullException>();
		new Action(() => MediaSegmentNavigator.Filter(null!, "a")).Should().Throw<ArgumentNullException>();
		new Action(() => MediaSegmentNavigator.Filter(null!, (IReadOnlySet<string>?)null)).Should().Throw<ArgumentNullException>();
		new Action(() => MediaSegmentNavigator.CountByKind(null!)).Should().Throw<ArgumentNullException>();
		new Action(() => MediaSegmentNavigator.Next(null!, TimeSpan.Zero, TimeSpan.Zero)).Should().Throw<ArgumentNullException>();
		new Action(() => MediaSegmentNavigator.Previous(null!, TimeSpan.Zero, TimeSpan.Zero)).Should().Throw<ArgumentNullException>();
		new Action(() => MediaSegmentNavigator.Active(null!, TimeSpan.Zero)).Should().Throw<ArgumentNullException>();
	}

	/// <summary>A segment without a kind has the empty kind, and its length and validity follow its start and end.</summary>
	[Fact]
	public void Segment_KindLengthAndValidity()
	{
		var noKind = new PDMediaSegment { Start = TimeSpan.FromSeconds(1), End = TimeSpan.FromSeconds(4), Kind = null! };
		var inverted = Seg(5, 2);

		MediaSegmentNavigator.GetKind(noKind).Should().BeEmpty();
		noKind.Length.Should().Be(TimeSpan.FromSeconds(3));
		noKind.IsValid.Should().BeTrue();
		inverted.Length.Should().Be(TimeSpan.Zero);
		inverted.IsValid.Should().BeFalse();
	}
}
