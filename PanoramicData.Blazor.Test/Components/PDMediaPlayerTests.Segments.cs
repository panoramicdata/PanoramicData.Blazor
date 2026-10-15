using AwesomeAssertions;
using Bunit;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;
using PanoramicData.Blazor.Enums;
using PanoramicData.Blazor.Models;

namespace PanoramicData.Blazor.Test.Components;

/// <summary>
/// Tests for the segments of <see cref="PDMediaPlayer"/>: the lanes, the list, the now playing strip and transcript,
/// navigation, playing a segment on its own and the entered and left events.
/// </summary>
public partial class PDMediaPlayerTests
{
	private static readonly PDMediaTextLine[] _lines =
	[
		new() { Start = TimeSpan.FromSeconds(120), Text = "Good morning.", Speaker = "Host" },
		new() { Start = TimeSpan.FromSeconds(130), Text = "Here is the news." },
		new() { Start = TimeSpan.FromSeconds(150), End = TimeSpan.FromSeconds(160), Text = "That was the news." }
	];

	// deliberately unsorted, with an overlap, a gap, an empty segment and one past the end of the media
	private static readonly PDMediaSegment[] _segments =
	[
		new() { Id = "talk", Start = TimeSpan.FromSeconds(120), End = TimeSpan.FromSeconds(180), Kind = "talk", Label = "Morning news", Lines = _lines },
		new() { Id = "song1", Start = TimeSpan.Zero, End = TimeSpan.FromSeconds(120), Kind = "song", Label = "Artist: First song", Detail = "Album" },
		new() { Id = "ad", Start = TimeSpan.FromSeconds(170), End = TimeSpan.FromSeconds(200), Kind = "ad", Label = "Advert", Text = "Buy things." },
		new() { Id = "empty", Start = TimeSpan.FromSeconds(300), End = TimeSpan.FromSeconds(300), Kind = "song" },
		new() { Id = "song2", Start = TimeSpan.FromSeconds(240), End = TimeSpan.FromSeconds(4000), Kind = "song", Label = "Long set" }
	];

	private Task<IRenderedComponent<PDMediaPlayer>> RenderWithSegmentsAsync(Action<ComponentParameterCollectionBuilder<PDMediaPlayer>>? configure = null)
		=> RenderReadyAsync(p =>
		{
			p.Add(x => x.Segments, _segments);
			configure?.Invoke(p);
		});

	private static IReadOnlyList<string> SegmentLabels(IRenderedComponent<PDMediaPlayer> cut)
		=> [.. cut.FindAll(".pdmp-segment-button .pdmp-marker-label").Select(e => e.TextContent)];

	/// <summary>Valid segments are drawn in lanes beneath the seek bar, overlaps in a second lane, and listed in order.</summary>
	[Fact]
	public async Task Segments_AreDrawnAndListed()
	{
		var cut = await RenderWithSegmentsAsync(p => p.Add(x => x.KindLabels, new Dictionary<string, string> { ["talk"] = "Talk" }));

		cut.Instance.SortedSegments.Should().HaveCount(4);
		var bars = cut.FindAll(".pdmp-segment-bar");
		bars.Select(b => b.TextContent).Should().Equal("Artist: First song", "Morning news", "Advert", "Long set");
		bars[2].GetAttribute("style").Should().Contain("--pdmp-segment-lane:1");
		bars[3].GetAttribute("style").Should().Contain("width:93.333%");
		bars[1].GetAttribute("title").Should().Be("Morning news (Talk), 0:02:00 to 0:03:00");
		cut.Find(".pdmp-segment-lanes").GetAttribute("style").Should().Be("--pdmp-segment-lanes:2");
		cut.Find(".pdmp-segment-lanes").GetAttribute("aria-hidden").Should().Be("true");
		cut.Find(".pdmp-track + .pdmp-segment-lanes").Should().NotBeNull();

		SegmentLabels(cut).Should().Equal("Artist: First song", "Morning news", "Advert", "Long set");
		cut.FindAll(".pdmp-segment-button .pdmp-marker-time")[0].TextContent.Should().Be("0:00:00 to 0:02:00 (2:00)");
		cut.FindAll(".pdmp-segment-button .pdmp-marker-kind")[1].TextContent.Should().Be("Talk");
		cut.Find(".pdmp-segment-button .pdmp-marker-detail").TextContent.Should().Be("Album");
		cut.Find(".pdmp-segments-toggle .badge").TextContent.Should().Be("4");
	}

	/// <summary>The lanes can sit above the seek bar.</summary>
	[Fact]
	public async Task SegmentLanes_CanSitAbove()
	{
		var cut = await RenderWithSegmentsAsync(p => p.Add(x => x.SegmentLanePlacement, MediaSegmentLanePlacement.Above));

		cut.Find(".pdmp-segment-lanes + .pdmp-track").Should().NotBeNull();
	}

	/// <summary>A live stream lists its segments but cannot draw them.</summary>
	[Fact]
	public async Task LiveStream_ListsButDoesNotDrawSegments()
	{
		var cut = RenderPlayer(p => p.Add(x => x.Segments, _segments));

		await RaiseAsync(cut, "loadedmetadata", State(duration: null, infinite: true));

		cut.FindAll(".pdmp-segment-bar").Should().BeEmpty();
		SegmentLabels(cut).Should().HaveCount(4);
	}

	/// <summary>Clicking a bar seeks with the lead and raises OnSegmentSelected; double clicking plays it on its own.</summary>
	[Fact]
	public async Task Bar_SeeksOrPlays()
	{
		var selected = new List<string>();
		var cut = await RenderWithSegmentsAsync(p => p
			.Add(x => x.SeekLead, TimeSpan.FromSeconds(3))
			.Add(x => x.OnSegmentSelected, s => selected.Add(s.Id)));

		await cut.FindAll(".pdmp-segment-bar")[1].ClickAsync(new MouseEventArgs());
		await cut.FindAll(".pdmp-segment-bar")[2].DoubleClickAsync(new MouseEventArgs());

		SeekTargets().Should().Equal(117.0, 170.0);
		selected.Should().Equal("talk", "ad");
		CountOf("play").Should().Be(1);
		cut.Instance.PlayingSegment!.Id.Should().Be("ad");
	}

	/// <summary>A segment played on its own pauses at its end; looping starts it again; seeking well away cancels it.</summary>
	[Fact]
	public async Task PlaySegment_StopsOrLoops()
	{
		var cut = await RenderWithSegmentsAsync();
		var ad = cut.Instance.SortedSegments.Single(s => s.Id == "ad");

		await cut.InvokeAsync(() => cut.Instance.PlaySegmentAsync(ad));
		await RaiseAsync(cut, "timeupdate", State(time: 185));
		CountOf("pause").Should().Be(0);
		await RaiseAsync(cut, "timeupdate", State(time: 199.97));
		CountOf("pause").Should().Be(1);
		cut.Instance.PlayingSegment.Should().BeNull();

		await cut.InvokeAsync(() => cut.Instance.PlaySegmentAsync(ad, true));
		cut.Instance.IsLoopingSegment.Should().BeTrue();
		await RaiseAsync(cut, "timeupdate", State(time: 200.2));
		SeekTargets().Should().Equal(170.0, 170.0, 170.0);

		await RaiseAsync(cut, "seeked", State(time: 10));
		cut.Instance.PlayingSegment.Should().BeNull();
		cut.Instance.IsLoopingSegment.Should().BeFalse();
		CountOf("pause").Should().Be(1);
	}

	/// <summary>The loop toggle loops the current segment without seeking, and turning it off lets playback carry on.</summary>
	[Fact]
	public async Task LoopToggle_LoopsTheCurrentSegment()
	{
		var cut = await RenderWithSegmentsAsync();
		await RaiseAsync(cut, "timeupdate", State(time: 60));

		await cut.Find("button.pdmp-loop-segment").ClickAsync(new MouseEventArgs());

		cut.Find("button.pdmp-loop-segment").GetAttribute("aria-pressed").Should().Be("true");
		cut.Instance.PlayingSegment!.Id.Should().Be("song1");
		SeekTargets().Should().BeEmpty();

		await cut.Find("button.pdmp-loop-segment").ClickAsync(new MouseEventArgs());
		cut.Instance.PlayingSegment.Should().BeNull();
		cut.Instance.IsLoopingSegment.Should().BeFalse();

		// between segments there is nothing to loop
		await RaiseAsync(cut, "timeupdate", State(time: 210));
		await cut.InvokeAsync(() => cut.Instance.SetSegmentLoopAsync(true));
		cut.Instance.IsLoopingSegment.Should().BeFalse();
	}

	/// <summary>The now playing strip shows the current segment, or that playback is between segments.</summary>
	[Fact]
	public async Task NowPlaying_ShowsTheCurrentSegment()
	{
		var cut = await RenderWithSegmentsAsync();
		await RaiseAsync(cut, "timeupdate", State(time: 175));

		cut.Find(".pdmp-now-label").TextContent.Should().Be("Advert");
		cut.Find(".pdmp-now-time").TextContent.Should().Be("0:02:50 to 0:03:20");
		cut.Find(".pdmp-now").GetAttribute("aria-live").Should().Be("polite");
		cut.Instance.CurrentSegments.Select(s => s.Id).Should().Equal("talk", "ad");
		cut.Find(".pdmp-segment-list [aria-current=true] .pdmp-marker-label").TextContent.Should().Be("Advert");

		await RaiseAsync(cut, "timeupdate", State(time: 210));
		cut.Find(".pdmp-now-empty").TextContent.Should().Be("Between segments");
		cut.Instance.CurrentSegment.Should().BeNull();

		cut.Render(p => p.Add(x => x.ShowNowPlaying, false));
		cut.FindAll(".pdmp-now").Should().BeEmpty();
	}

	/// <summary>The play segment button and the P shortcut play the current segment on its own; P does nothing between segments.</summary>
	[Fact]
	public async Task PlayCurrentSegment_FromTheStripAndShortcut()
	{
		var cut = await RenderWithSegmentsAsync();
		await RaiseAsync(cut, "timeupdate", State(time: 60));

		await cut.Find("button.pdmp-play-segment").ClickAsync(new MouseEventArgs());
		await RaiseAsync(cut, "timeupdate", State(time: 250));
		await PressAsync(cut, "p");
		await RaiseAsync(cut, "timeupdate", State(time: 210));
		await PressAsync(cut, "P");

		// between segments P has nothing to play, and the seek back to 210 s cancelled playing the long set
		SeekTargets().Should().Equal(0.0, 240.0);
		cut.Instance.PlayingSegment.Should().BeNull();
	}

	/// <summary>Timed lines are shown when the transcript is expanded, the spoken line is highlighted and kept in view, and selecting a line seeks.</summary>
	[Fact]
	public async Task Transcript_FollowsPlayback()
	{
		var cut = await RenderWithSegmentsAsync(p => p.Add(x => x.Id, "mp"));
		await RaiseAsync(cut, "timeupdate", State(time: 135));
		cut.FindAll(".pdmp-transcript").Should().BeEmpty();

		await cut.Find("button.pdmp-transcript-toggle").ClickAsync(new MouseEventArgs());

		cut.Find("button.pdmp-transcript-toggle").GetAttribute("aria-expanded").Should().Be("true");
		cut.Find("button.pdmp-transcript-toggle").GetAttribute("aria-controls").Should().Be("mp-transcript");
		cut.FindAll("#mp-transcript .pdmp-line-text").Select(e => e.TextContent).Should().Equal("Host: Good morning.", "Here is the news.", "That was the news.");
		cut.Find(".pdmp-line-active .pdmp-line-text").TextContent.Should().Be("Here is the news.");
		CountOf("reveal").Should().Be(1);

		await RaiseAsync(cut, "timeupdate", State(time: 136));
		CountOf("reveal").Should().Be(1);
		await RaiseAsync(cut, "timeupdate", State(time: 155));
		cut.Find("[aria-current=true].pdmp-line-button .pdmp-line-text").TextContent.Should().Be("That was the news.");
		CountOf("reveal").Should().Be(2);

		await cut.FindAll(".pdmp-line-button")[0].ClickAsync(new MouseEventArgs());
		SeekTargets().Should().Equal(120.0);

		await RaiseAsync(cut, "timeupdate", State(time: 165));
		cut.FindAll(".pdmp-line-active").Should().BeEmpty();
	}

	/// <summary>Plain text is shown as it is, and the transcript can start expanded.</summary>
	[Fact]
	public async Task Transcript_PlainTextAndStartExpanded()
	{
		var cut = await RenderWithSegmentsAsync(p => p.Add(x => x.TranscriptExpanded, true));
		await RaiseAsync(cut, "timeupdate", State(time: 190));

		cut.Find(".pdmp-transcript-text").TextContent.Should().Be("Buy things.");

		await RaiseAsync(cut, "timeupdate", State(time: 60));
		cut.FindAll("button.pdmp-transcript-toggle").Should().BeEmpty();
		cut.FindAll(".pdmp-transcript").Should().BeEmpty();
	}

	/// <summary>Next and previous segment walk the visible starts, by buttons, shortcuts and methods, and stop at the ends.</summary>
	[Fact]
	public async Task NextAndPreviousSegment_Walk()
	{
		var cut = await RenderWithSegmentsAsync();

		await cut.Find("button.pdmp-next-segment").ClickAsync(new MouseEventArgs());
		await PressAsync(cut, ".");
		await PressAsync(cut, ".");
		(await cut.InvokeAsync(() => cut.Instance.NextSegmentAsync())).Should().BeNull();
		await PressAsync(cut, ",");
		await cut.Find("button.pdmp-previous-segment").ClickAsync(new MouseEventArgs());
		(await cut.InvokeAsync(() => cut.Instance.PreviousSegmentAsync("song")))!.Id.Should().Be("song1");
		(await cut.InvokeAsync(() => cut.Instance.NextSegmentAsync("AD")))!.Id.Should().Be("ad");

		SeekTargets().Should().Equal(120.0, 170.0, 240.0, 170.0, 120.0, 0.0, 170.0);
	}

	/// <summary>A segment that starts past the end of the media is not found again once there.</summary>
	[Fact]
	public async Task NextSegment_PastTheEnd_IsNotFoundAgain()
	{
		PDMediaSegment[] late = [new() { Id = "late", Start = TimeSpan.FromSeconds(5000), End = TimeSpan.FromSeconds(6000) }];
		var cut = await RenderReadyAsync(p => p.Add(x => x.Segments, late));

		(await cut.InvokeAsync(() => cut.Instance.NextSegmentAsync()))!.Id.Should().Be("late");
		(await cut.InvokeAsync(() => cut.Instance.NextSegmentAsync())).Should().BeNull();
		SeekTargets().Should().Equal(3600.0);
	}

	/// <summary>Entering and leaving segments raises events, for overlapping segments and for seeks too.</summary>
	[Fact]
	public async Task Crossings_RaiseEnteredAndLeft()
	{
		var crossings = new List<string>();
		var cut = await RenderWithSegmentsAsync(p => p
			.Add(x => x.OnSegmentEntered, s => crossings.Add($"in {s.Id}"))
			.Add(x => x.OnSegmentLeft, s => crossings.Add($"out {s.Id}")));

		await RaiseAsync(cut, "timeupdate", State(time: 10));
		await RaiseAsync(cut, "timeupdate", State(time: 125));
		await RaiseAsync(cut, "timeupdate", State(time: 175));
		await RaiseAsync(cut, "seeked", State(time: 250));
		await RaiseAsync(cut, "progress", State(time: 10));

		crossings.Should().Equal("in song1", "out song1", "in talk", "in ad", "out talk", "out ad", "in song2");
	}

	/// <summary>Segment kind chips filter the lanes, the list and navigation, and raise VisibleSegmentKindsChanged.</summary>
	[Fact]
	public async Task SegmentKindChips_Filter()
	{
		IReadOnlySet<string>? raised = null;
		var cut = await RenderWithSegmentsAsync(p => p.Add(x => x.VisibleSegmentKindsChanged, k => raised = k));

		var chips = cut.FindAll(".pdmp-segment-kinds .pdmp-kind-chip");
		chips.Select(c => c.TextContent.Trim()[..2]).Should().Equal("ad", "so", "ta");
		await chips[1].ClickAsync(new MouseEventArgs());

		raised.Should().BeEquivalentTo(["ad", "talk"]);
		cut.Instance.IsSegmentKindVisible("SONG").Should().BeFalse();
		SegmentLabels(cut).Should().Equal("Morning news", "Advert");
		cut.FindAll(".pdmp-segment-bar").Should().HaveCount(2);
		cut.Instance.VisibleSegments.Should().HaveCount(2);
		(await cut.InvokeAsync(() => cut.Instance.NextSegmentAsync()))!.Id.Should().Be("talk");

		await cut.InvokeAsync(() => cut.Instance.SetSegmentKindVisibleAsync("song", false));
		await cut.InvokeAsync(() => cut.Instance.SetSegmentKindVisibleAsync("song", true));
		SegmentLabels(cut).Should().HaveCount(4);
	}

	/// <summary>The VisibleSegmentKinds parameter filters; hiding every kind shows a message and disables navigation.</summary>
	[Fact]
	public async Task VisibleSegmentKindsParameter_Filters()
	{
		var cut = await RenderWithSegmentsAsync(p => p.Add(x => x.VisibleSegmentKinds, new HashSet<string> { " TALK ", null! }));

		SegmentLabels(cut).Should().Equal("Morning news");

		cut.Render(p => p.Add(x => x.VisibleSegmentKinds, new HashSet<string>()));

		cut.Find(".pdmp-segments-panel .pdmp-markers-empty").TextContent.Should().Be("No segments of the selected kinds.");
		cut.Find("button.pdmp-next-segment").HasAttribute("disabled").Should().BeTrue();
		cut.FindAll(".pdmp-segment-lanes").Should().BeEmpty();
	}

	/// <summary>The segment list can be collapsed, start collapsed, or be hidden.</summary>
	[Fact]
	public async Task SegmentList_Collapses()
	{
		var cut = await RenderWithSegmentsAsync(p => p.Add(x => x.Id, "mp").Add(x => x.SegmentListExpanded, false));
		cut.FindAll("#mp-segments").Should().BeEmpty();

		await cut.Find(".pdmp-segments-toggle").ClickAsync(new MouseEventArgs());
		cut.Find(".pdmp-segments-toggle").GetAttribute("aria-expanded").Should().Be("true");
		cut.FindAll("#mp-segments").Should().ContainSingle();

		cut.Render(p => p.Add(x => x.ShowSegmentList, false));
		cut.FindAll(".pdmp-segments-panel").Should().BeEmpty();
	}

	/// <summary>The play button on a list row plays that segment on its own.</summary>
	[Fact]
	public async Task ListRowPlayButton_PlaysTheSegment()
	{
		var cut = await RenderWithSegmentsAsync();

		await cut.FindAll("button.pdmp-segment-play")[2].ClickAsync(new MouseEventArgs());

		cut.Instance.PlayingSegment!.Id.Should().Be("ad");
		cut.FindAll("button.pdmp-segment-play")[2].GetAttribute("aria-label").Should().Be("Play Advert only");
	}

	/// <summary>Changing the source forgets the segment being played and the segments playback was in.</summary>
	[Fact]
	public async Task SourceChange_ResetsSegmentPlayback()
	{
		var entered = 0;
		var cut = await RenderWithSegmentsAsync(p => p.Add(x => x.OnSegmentEntered, _ => entered++));
		await RaiseAsync(cut, "timeupdate", State(time: 10));
		await cut.InvokeAsync(() => cut.Instance.PlaySegmentAsync(cut.Instance.SortedSegments[0], true));

		cut.Render(p => p.Add(x => x.Src, "/media/next.wav"));
		await RaiseAsync(cut, "loadedmetadata", State(duration: 3600));
		await RaiseAsync(cut, "timeupdate", State(time: 10));

		cut.Instance.PlayingSegment.Should().BeNull();
		cut.Instance.IsLoopingSegment.Should().BeFalse();
		entered.Should().Be(2);
	}

	/// <summary>Thousands of segments are drawn as a bounded number of bars, the list renders only some rows, and changes are picked up.</summary>
	[Fact]
	public async Task ManySegments_RenderEfficiently()
	{
		var many = Enumerable.Range(0, 5000).Select(i => new PDMediaSegment
		{
			Id = $"s{i}",
			Start = TimeSpan.FromSeconds(i * 0.72),
			End = TimeSpan.FromSeconds((i * 0.72) + 0.5),
			Kind = "song"
		}).ToList();

		var cut = await RenderReadyAsync(p => p.Add(x => x.Segments, many).Add(x => x.MarkerTickResolution, 200));

		cut.FindAll(".pdmp-segment-bar").Count.Should().BeLessThanOrEqualTo(400);
		cut.FindAll(".pdmp-segment-merged").Should().NotBeEmpty();
		cut.FindAll(".pdmp-segment-button").Count.Should().BeLessThan(5000);

		many.Add(new PDMediaSegment { Id = "extra", Start = TimeSpan.Zero, End = TimeSpan.FromSeconds(10), Kind = "talk" });
		cut.Render(p => p.Add(x => x.Segments, many).Add(x => x.MaxSegmentLanes, 1));
		cut.Find(".pdmp-segments-toggle .badge").TextContent.Should().Be("5001");
		cut.Find(".pdmp-segment-lanes").GetAttribute("style").Should().Be("--pdmp-segment-lanes:1");
	}

	/// <summary>Every segment method checks its argument.</summary>
	[Fact]
	public async Task SegmentMethods_RequireASegment()
	{
		var cut = await RenderWithSegmentsAsync();

		var seek = () => cut.InvokeAsync(() => cut.Instance.SeekToSegmentAsync(null!));
		var play = () => cut.InvokeAsync(() => cut.Instance.PlaySegmentAsync(null!, false));

		await seek.Should().ThrowAsync<ArgumentNullException>();
		await play.Should().ThrowAsync<ArgumentNullException>();
	}

	/// <summary>Removing the segments removes their lanes and list; without a source segment navigation is disabled.</summary>
	[Fact]
	public async Task Segments_RemovedOrWithoutASource()
	{
		var cut = await RenderWithSegmentsAsync();

		cut.Render(p => p.Add(x => x.Segments, (IEnumerable<PDMediaSegment>?)null));

		cut.FindAll(".pdmp-segments-panel").Should().BeEmpty();
		cut.FindAll(".pdmp-segment-lanes").Should().BeEmpty();

		var noSource = Render<PDMediaPlayer>(p => p.Add(x => x.Segments, _segments));
		noSource.Find("button.pdmp-next-segment").HasAttribute("disabled").Should().BeTrue();
	}

	/// <summary>Segment navigation counts from a pending seek, or from where the slider is being dragged.</summary>
	[Fact]
	public async Task NextSegment_FromAPendingSeekOrADrag()
	{
		var cut = RenderPlayer(p => p.Add(x => x.Segments, _segments));
		await cut.InvokeAsync(() => cut.Instance.SeekAsync(TimeSpan.FromSeconds(130)));
		(await cut.InvokeAsync(() => cut.Instance.NextSegmentAsync()))!.Id.Should().Be("ad");

		await RaiseAsync(cut, "loadedmetadata", State(duration: 3600));
		await cut.Find("input.pdmp-seek").InputAsync(new ChangeEventArgs { Value = "200" });
		(await cut.InvokeAsync(() => cut.Instance.NextSegmentAsync()))!.Id.Should().Be("song2");
	}

	/// <summary>A host that does not bind the kind filters can re-render without undoing the user's choice.</summary>
	[Fact]
	public async Task KindChips_SurviveAnUnboundHostReRendering()
	{
		var cut = await RenderWithSegmentsAsync(p => p.Add(x => x.Markers, _markers));

		await cut.Find(".pdmp-segment-kinds .pdmp-kind-chip").ClickAsync(new MouseEventArgs());
		await cut.Find(".pdmp-kinds:not(.pdmp-segment-kinds) .pdmp-kind-chip").ClickAsync(new MouseEventArgs());
		cut.Render(p => p.Add(x => x.Title, "Re-rendered"));

		cut.Instance.IsSegmentKindVisible("ad").Should().BeFalse();
		cut.Instance.IsKindVisible("clipping").Should().BeFalse();
	}
}