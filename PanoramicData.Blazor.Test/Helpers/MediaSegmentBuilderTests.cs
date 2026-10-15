using AwesomeAssertions;
using PanoramicData.Blazor.Models;

namespace PanoramicData.Blazor.Test.Helpers;

/// <summary>Tests for <see cref="PDMediaSegment.FromMarkers(IEnumerable{PDMediaMarker}, TimeSpan)"/>.</summary>
public class MediaSegmentBuilderTests
{
	private static readonly TimeSpan _duration = TimeSpan.FromSeconds(100);

	private static PDMediaMarker At(double seconds, string kind, string? label = null)
		=> new()
		{
			Id = $"{kind}{seconds}",
			Position = TimeSpan.FromSeconds(seconds),
			Kind = kind,
			Label = label ?? kind,
			Detail = $"detail {seconds}",
			Color = "#123456",
			CssClass = "css"
		};

	private static string[] Describe(IEnumerable<PDMediaSegment> segments)
		=> [.. segments.Select(s => $"{s.Kind} {s.Start.TotalSeconds}-{s.End.TotalSeconds}")];

	/// <summary>Each marker opens a segment that runs to the next, and the last runs to the duration, whatever the input order.</summary>
	[Fact]
	public void FromMarkers_RunsMarkerToMarker()
	{
		var segments = PDMediaSegment.FromMarkers([At(40, "talk"), null, At(10, "song"), At(70, "song")], _duration);

		Describe(segments).Should().Equal("song 10-40", "talk 40-70", "song 70-100");
		segments[0].Should().Match<PDMediaSegment>(s =>
			s.Id == "song10" && s.Label == "song" && s.Detail == "detail 10" && s.Color == "#123456" && s.CssClass == "css");
	}

	/// <summary>Markers at the same time make an empty segment, which is left out; so are segments past the end.</summary>
	[Fact]
	public void FromMarkers_LeavesOutEmptySegments()
	{
		var segments = PDMediaSegment.FromMarkers([At(-5, "intro"), At(10, "a"), At(10, "b"), At(100, "c"), At(120, "d")], _duration);

		Describe(segments).Should().Equal("intro 0-10", "b 10-100");
	}

	/// <summary>Skipped kinds are ignored, and end kinds close a segment and leave a gap.</summary>
	[Fact]
	public void FromMarkers_SkipsAndEnds()
	{
		var options = new MediaSegmentsFromMarkersOptions { SkipKinds = ["CLIPPING"], EndKinds = [" end "] };

		var segments = PDMediaSegment.FromMarkers(
			[At(0, "song"), At(20, "clipping"), At(30, "end"), At(50, "talk"), At(60, "end")],
			_duration,
			options);

		Describe(segments).Should().Equal("song 0-30", "talk 50-60");
	}

	/// <summary>Neighbouring segments of the same kind merge when asked, keeping the first one's label; gaps prevent it.</summary>
	[Fact]
	public void FromMarkers_MergesAdjacentSameKind()
	{
		var options = new MediaSegmentsFromMarkersOptions { MergeAdjacentSameKind = true, EndKinds = ["end"] };

		var segments = PDMediaSegment.FromMarkers(
			[At(0, "song", "First"), At(10, "SONG", "Second"), At(20, "talk"), At(30, "end"), At(40, "talk"), At(50, "song")],
			_duration,
			options);

		Describe(segments).Should().Equal("song 0-20", "talk 20-30", "talk 40-50", "song 50-100");
		segments[0].Label.Should().Be("First");
		segments[0].Id.Should().Be("song0");
	}

	/// <summary>No markers give no segments, and options are required.</summary>
	[Fact]
	public void FromMarkers_EdgeCases()
	{
		PDMediaSegment.FromMarkers(null, _duration).Should().BeEmpty();
		PDMediaSegment.FromMarkers([At(10, "a")], TimeSpan.Zero).Should().BeEmpty();
		var noOptions = () => PDMediaSegment.FromMarkers([], _duration, null!);
		noOptions.Should().Throw<ArgumentNullException>();
	}
}
