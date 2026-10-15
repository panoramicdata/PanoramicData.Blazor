using AwesomeAssertions;
using PanoramicData.Blazor.Helpers;
using PanoramicData.Blazor.Models;

namespace PanoramicData.Blazor.Test.Helpers;

/// <summary>Tests for <see cref="MediaSegmentLayout"/>.</summary>
public class MediaSegmentLayoutTests
{
	private static readonly TimeSpan _minute = TimeSpan.FromMinutes(1);

	private static PDMediaSegment Seg(double start, double end, string kind = "song", int? lane = null, string? color = null)
		=> new()
		{
			Id = $"{kind}{start}",
			Start = TimeSpan.FromSeconds(start),
			End = TimeSpan.FromSeconds(end),
			Kind = kind,
			Lane = lane,
			Color = color,
			Label = $"{kind} {start}"
		};

	private static IReadOnlyList<MediaSegmentBar> Bars(IEnumerable<PDMediaSegment> segments, TimeSpan? duration, int maxLanes = 3, int resolution = 100)
		=> MediaSegmentLayout.GetBars(MediaSegmentNavigator.Sort(segments), duration, maxLanes, resolution, s => s.Color ?? "red");

	/// <summary>Overlapping segments go into further lanes, and a lane is reused once its segment has ended.</summary>
	[Fact]
	public void PackLanes_PacksOverlaps()
	{
		var sorted = MediaSegmentNavigator.Sort([Seg(0, 30), Seg(10, 20), Seg(15, 40), Seg(30, 50), Seg(45, 60)]);

		MediaSegmentLayout.PackLanes(sorted, 3).Should().Equal(0, 1, 2, 0, 1);
	}

	/// <summary>A segment's own lane is used, limited to the last lane, and overflow folds into the last lane.</summary>
	[Fact]
	public void PackLanes_ForcedAndOverflow()
	{
		var sorted = MediaSegmentNavigator.Sort([Seg(0, 30, lane: 5), Seg(1, 30), Seg(2, 30), Seg(3, 30, lane: -1)]);

		MediaSegmentLayout.PackLanes(sorted, 2).Should().Equal(1, 0, 1, 1);
		MediaSegmentLayout.PackLanes(sorted, 0).Should().Equal(0, 0, 0, 0);
	}

	/// <summary>Without a duration or segments nothing is drawn.</summary>
	[Fact]
	public void GetBars_NothingToDraw()
	{
		Bars([], _minute).Should().BeEmpty();
		Bars([Seg(0, 10)], null).Should().BeEmpty();
		Bars([Seg(0, 10)], TimeSpan.Zero).Should().BeEmpty();
	}

	/// <summary>Bars are placed by percentage in their lanes; segments past the end are cut at the end or left out.</summary>
	[Fact]
	public void GetBars_PlacesAndClamps()
	{
		var bars = Bars([Seg(15, 30), Seg(20, 90, "talk"), Seg(70, 80, "late")], _minute);

		bars.Should().HaveCount(2);
		bars[0].Should().Match<MediaSegmentBar>(b => b.Lane == 0 && b.LeftPercent == 25 && b.WidthPercent == 25 && b.Count == 1);
		bars[1].Should().Match<MediaSegmentBar>(b => b.Lane == 1 && Math.Abs(b.LeftPercent - (100.0 / 3)) < 1e-9 && Math.Abs(b.LeftPercent + b.WidthPercent - 100) < 1e-9);
	}

	/// <summary>Short neighbours of the same kind and colour in a lane merge; long ones, other kinds and other colours do not.</summary>
	[Fact]
	public void GetBars_MergesShortNeighbours()
	{
		// 100 slots over a minute: a slot is 0.6 s
		var bars = Bars(
			[Seg(10, 10.2), Seg(10.3, 10.5), Seg(10.6, 10.7), Seg(10.8, 10.9, "talk"), Seg(20, 30), Seg(30, 40), Seg(50, 50.1), Seg(50.2, 50.3, color: "blue")],
			_minute,
			maxLanes: 1);

		bars.Select(b => b.Count).Should().Equal(3, 1, 1, 1, 1, 1);
		bars[0].End.Should().Be(TimeSpan.FromSeconds(10.7));

		// a short segment inside another in the same lane keeps the longer end
		Bars([Seg(10, 10.5, lane: 0), Seg(10.1, 10.2, lane: 0)], _minute).Single().End.Should().Be(TimeSpan.FromSeconds(10.5));
	}

	/// <summary>Thousands of short segments are drawn as a bounded number of bars.</summary>
	[Fact]
	public void GetBars_BoundsTheBarCount()
	{
		var segments = Enumerable.Range(0, 10_000).Select(i => Seg(i * 0.36, (i * 0.36) + 0.2));

		var bars = Bars(segments, TimeSpan.FromHours(1), resolution: 400);

		bars.Count.Should().BeLessThanOrEqualTo(800);
		bars.Sum(b => b.Count).Should().Be(10_000);
	}

	/// <summary>Tooltips describe one segment, with its confidence and source, or the merged segments.</summary>
	[Fact]
	public void DescribeBar_DescribesOneOrMany()
	{
		var labelled = new PDMediaSegment { Start = TimeSpan.FromSeconds(65), End = TimeSpan.FromSeconds(70), Label = "Song A", Confidence = 0.874, Source = "detector" };
		var unlabelled = Seg(65, 70);
		var single = MediaSegmentLayout.GetBars([labelled], TimeSpan.FromMinutes(2), 1, 100, _ => "red")[0];
		var plain = MediaSegmentLayout.GetBars([new PDMediaSegment { Start = unlabelled.Start, End = unlabelled.End }], TimeSpan.FromMinutes(2), 1, 100, _ => "red")[0];
		var merged = Bars([Seg(65, 65.1), Seg(65.2, 65.3)], TimeSpan.FromMinutes(2))[0];

		MediaSegmentLayout.DescribeBar(single, "Song", TimeSpan.FromMinutes(2)).Should().Be("Song A (Song), 1:05 to 1:10, 87% confidence, from detector");
		MediaSegmentLayout.DescribeBar(plain, "Song", TimeSpan.FromMinutes(2)).Should().Be("Song, 1:05 to 1:10");
		MediaSegmentLayout.DescribeBar(merged, "Song", TimeSpan.FromMinutes(2)).Should().Be("2 Song segments, 1:05 to 1:05");
	}
}
