using AwesomeAssertions;
using Bunit;
using Microsoft.AspNetCore.Components.Web;
using PanoramicData.Blazor.Models;

namespace PanoramicData.Blazor.Test.Components;

/// <summary>
/// Tests for selecting a section (segment) of <see cref="PDMediaPlayer"/>: where the playhead goes, which section is shown as current
/// afterwards, and what is raised. The sections are the shared test set: "First song" 0 to 2:00, "Morning news" 2:00 to 3:00, "Advert"
/// 2:50 to 3:20 (overlapping the news) and "Long set" from 4:00, in bars 0 to 3.
/// </summary>
public partial class PDMediaPlayerTests
{
	private const int FirstSongBar = 0;
	private const int NewsBar = 1;
	private const int AdvertBar = 2;
	private const int LongSetBar = 3;

	private static string CurrentBarLabel(IRenderedComponent<PDMediaPlayer> cut)
		=> string.Join("|", cut.FindAll(".pdmp-segment-bar.pdmp-segment-current").Select(b => b.TextContent));

	private static string CurrentRowLabel(IRenderedComponent<PDMediaPlayer> cut)
		=> cut.Find(".pdmp-segment-list [aria-current=true] .pdmp-marker-label").TextContent;

	private static Task ClickBarAsync(IRenderedComponent<PDMediaPlayer> cut, int bar)
		=> cut.FindAll(".pdmp-segment-bar")[bar].ClickAsync(new MouseEventArgs());

	/// <summary>With no lead, selecting a section puts the playhead exactly at its start and makes it the current section at once.</summary>
	[Theory]
	[InlineData(FirstSongBar, 0.0, "Artist: First song")]
	[InlineData(NewsBar, 120.0, "Morning news")]
	[InlineData(AdvertBar, 170.0, "Advert")]
	[InlineData(LongSetBar, 240.0, "Long set")]
	public async Task NoLead_SelectingASection_GoesToItsStartAndMakesItCurrent(int bar, double start, string label)
	{
		var cut = await RenderWithSegmentsAsync();
		await RaiseAsync(cut, "timeupdate", State(time: 1000));

		await ClickBarAsync(cut, bar);

		SeekTargets().Should().Equal(start);
		cut.Instance.CurrentSegment!.Label.Should().Be(label);
		CurrentRowLabel(cut).Should().Be(label);
		CurrentBarLabel(cut).Should().Contain(label);
		cut.Find(".pdmp-now-label").TextContent.Should().Be(label);
		cut.Find("input.pdmp-seek").GetAttribute("value").Should().Be(start.ToString(System.Globalization.CultureInfo.InvariantCulture));
	}

	/// <summary>With a lead, the playhead goes that far before the section, and the selected section is the current one, not the one before it.</summary>
	[Theory]
	[InlineData(NewsBar, 117.0, "Morning news")]
	[InlineData(AdvertBar, 167.0, "Advert")]
	[InlineData(LongSetBar, 237.0, "Long set")]
	public async Task WithALead_SelectingASection_MakesItCurrentEvenThoughThePlayheadIsJustBefore(int bar, double target, string label)
	{
		var cut = await RenderWithSegmentsAsync(p => p.Add(x => x.SeekLead, TimeSpan.FromSeconds(3)));
		await RaiseAsync(cut, "timeupdate", State(time: 5));

		await ClickBarAsync(cut, bar);

		SeekTargets().Should().Equal(target);
		cut.Instance.CurrentSegment!.Label.Should().Be(label);
		CurrentRowLabel(cut).Should().Be(label);
		CurrentBarLabel(cut).Should().Contain(label);
		cut.Find(".pdmp-now-label").TextContent.Should().Be(label);
		cut.Find("input.pdmp-seek").GetAttribute("value").Should().Be(target.ToString(System.Globalization.CultureInfo.InvariantCulture));
	}

	/// <summary>The case from the demo: with a lead, selecting the section after the current one must not leave the current one outlined.</summary>
	[Fact]
	public async Task WithALead_SelectingTheNextSection_NoLongerShowsThePreviousOneAsCurrent()
	{
		var cut = await RenderWithSegmentsAsync(p => p.Add(x => x.SeekLead, TimeSpan.FromSeconds(2)));
		await RaiseAsync(cut, "timeupdate", State(time: 60));
		CurrentBarLabel(cut).Should().Be("Artist: First song");

		await ClickBarAsync(cut, NewsBar);

		SeekTargets().Should().Equal(118.0);
		CurrentBarLabel(cut).Should().Be("Morning news");
		CurrentRowLabel(cut).Should().Be("Morning news");
		cut.Find(".pdmp-now-label").TextContent.Should().Be("Morning news");
	}

	/// <summary>The selected section stays current when the media reports the position it has sought to.</summary>
	[Fact]
	public async Task WithALead_TheSelectedSectionStaysCurrentWhenThePlayerReportsTheSeek()
	{
		var cut = await RenderWithSegmentsAsync(p => p.Add(x => x.SeekLead, TimeSpan.FromSeconds(3)));

		await ClickBarAsync(cut, NewsBar);
		await RaiseAsync(cut, "seeked", State(time: 117));
		cut.Instance.CurrentSegment!.Id.Should().Be("talk");

		await RaiseAsync(cut, "timeupdate", State(time: 117.5));
		cut.Instance.CurrentSegment!.Id.Should().Be("talk");
		CurrentRowLabel(cut).Should().Be("Morning news");

		await RaiseAsync(cut, "timeupdate", State(time: 121));
		cut.Instance.CurrentSegment!.Id.Should().Be("talk");
	}

	/// <summary>The lead is used the same way for segments and markers, so a section and a marker at the same time agree.</summary>
	[Fact]
	public async Task WithALead_SectionsAndMarkersAgreeAboutWhereThePlayheadIs()
	{
		PDMediaMarker[] markers = [new() { Id = "m", Position = TimeSpan.FromSeconds(120), Kind = "mark", Label = "News starts" }];
		var cut = await RenderWithSegmentsAsync(p => p
			.Add(x => x.SeekLead, TimeSpan.FromSeconds(3))
			.Add(x => x.Markers, markers));

		await ClickBarAsync(cut, NewsBar);

		cut.Instance.CurrentMarker!.Id.Should().Be("m");
		cut.Instance.CurrentSegment!.Id.Should().Be("talk");
	}

	/// <summary>A lead longer than the time before the section clamps the seek to the start of the media.</summary>
	[Fact]
	public async Task WithALongLead_TheSeekClampsToTheStartAndTheSectionIsStillCurrent()
	{
		PDMediaSegment[] segments =
		[
			new() { Id = "early", Start = TimeSpan.FromSeconds(2), End = TimeSpan.FromSeconds(10), Label = "Early" },
			new() { Id = "later", Start = TimeSpan.FromSeconds(20), End = TimeSpan.FromSeconds(30), Label = "Later" }
		];
		var cut = await RenderReadyAsync(p =>
		{
			p.Add(x => x.Segments, segments);
			p.Add(x => x.SeekLead, TimeSpan.FromSeconds(5));
		});

		await ClickBarAsync(cut, 0);

		SeekTargets().Should().Equal(0.0);
		cut.Instance.CurrentSegment!.Id.Should().Be("early");
	}

	/// <summary>A negative lead is the same as none.</summary>
	[Fact]
	public async Task ANegativeLead_IsTreatedAsNone()
	{
		var cut = await RenderWithSegmentsAsync(p => p.Add(x => x.SeekLead, TimeSpan.FromSeconds(-4)));

		await ClickBarAsync(cut, NewsBar);

		SeekTargets().Should().Equal(120.0);
		cut.Instance.CurrentSegment!.Id.Should().Be("talk");
	}

	/// <summary>Selecting sections one after another moves the current section each time, and selecting the same one again keeps it.</summary>
	[Theory]
	[InlineData(0.0)]
	[InlineData(2.0)]
	public async Task SelectingSectionsInTurn_MovesTheCurrentSectionEachTime(double lead)
	{
		var cut = await RenderWithSegmentsAsync(p => p.Add(x => x.SeekLead, TimeSpan.FromSeconds(lead)));
		var order = new[] { (NewsBar, "talk"), (LongSetBar, "song2"), (FirstSongBar, "song1"), (FirstSongBar, "song1"), (NewsBar, "talk") };

		foreach (var (bar, id) in order)
		{
			await ClickBarAsync(cut, bar);
			cut.Instance.CurrentSegment!.Id.Should().Be(id);
			CurrentRowLabel(cut).Should().Be(cut.Instance.CurrentSegment!.Label);
		}

		SeekTargets().Should().Equal(120.0 - lead, 240.0 - lead, Math.Max(0, 0 - lead), Math.Max(0, 0 - lead), 120.0 - lead);
	}

	/// <summary>Where the sections overlap, selecting the one that starts later makes it the current one.</summary>
	[Theory]
	[InlineData(0.0)]
	[InlineData(3.0)]
	public async Task SelectingAnOverlappingSection_MakesTheLaterStartingOneCurrent(double lead)
	{
		var cut = await RenderWithSegmentsAsync(p => p.Add(x => x.SeekLead, TimeSpan.FromSeconds(lead)));

		await ClickBarAsync(cut, AdvertBar);

		cut.Instance.CurrentSegment!.Id.Should().Be("ad");
		cut.Instance.CurrentSegments.Select(s => s.Id).Should().Equal("talk", "ad");
		CurrentBarLabel(cut).Should().Contain("Advert");
	}

	/// <summary>Raising of OnSegmentSelected does not depend on the lead; the section is always the one clicked.</summary>
	[Theory]
	[InlineData(0.0)]
	[InlineData(3.0)]
	public async Task SelectingASection_RaisesSelectedWithThatSection(double lead)
	{
		var selected = new List<string>();
		var cut = await RenderWithSegmentsAsync(p => p
			.Add(x => x.SeekLead, TimeSpan.FromSeconds(lead))
			.Add(x => x.OnSegmentSelected, s => selected.Add(s.Id)));

		await ClickBarAsync(cut, NewsBar);
		await ClickBarAsync(cut, LongSetBar);

		selected.Should().Equal("talk", "song2");
	}

	/// <summary>Entered and left follow where the media actually is, so a lead delays entering until the section starts.</summary>
	[Fact]
	public async Task WithALead_EnteredIsRaisedOnlyWhenPlaybackReachesTheSection()
	{
		var entered = new List<string>();
		var left = new List<string>();
		var cut = await RenderWithSegmentsAsync(p => p
			.Add(x => x.SeekLead, TimeSpan.FromSeconds(3))
			.Add(x => x.OnSegmentEntered, s => entered.Add(s.Id))
			.Add(x => x.OnSegmentLeft, s => left.Add(s.Id)));
		await RaiseAsync(cut, "timeupdate", State(time: 60));
		entered.Clear();

		await ClickBarAsync(cut, NewsBar);
		await RaiseAsync(cut, "timeupdate", State(time: 117));
		entered.Should().BeEmpty();

		await RaiseAsync(cut, "timeupdate", State(time: 120.5));
		entered.Should().Equal("talk");
		left.Should().Equal("song1");
	}

	/// <summary>The list row, the bar, and the next and previous shortcuts all select a section in the same way.</summary>
	[Theory]
	[InlineData(0.0)]
	[InlineData(3.0)]
	public async Task EverySelectionRoute_GivesTheSameSeekAndTheSameCurrentSection(double lead)
	{
		var cut = await RenderWithSegmentsAsync(p => p.Add(x => x.SeekLead, TimeSpan.FromSeconds(lead)));

		await ClickBarAsync(cut, NewsBar);
		var viaBar = (cut.Instance.CurrentSegment!.Id, SeekTargets().Last());

		await ClickBarAsync(cut, FirstSongBar);
		await cut.FindAll(".pdmp-segment-button")[NewsBar].ClickAsync(new MouseEventArgs());
		var viaRow = (cut.Instance.CurrentSegment!.Id, SeekTargets().Last());

		await ClickBarAsync(cut, FirstSongBar);
		var next = await cut.InvokeAsync(() => cut.Instance.NextSegmentAsync());
		var viaNext = (cut.Instance.CurrentSegment!.Id, SeekTargets().Last());

		await cut.InvokeAsync(() => cut.Instance.SeekToSegmentAsync(_segments[0]));
		var viaMethod = (cut.Instance.CurrentSegment!.Id, SeekTargets().Last());

		viaBar.Should().Be(("talk", 120.0 - lead));
		viaRow.Should().Be(viaBar);
		next!.Id.Should().Be("talk");
		viaNext.Should().Be(viaBar);
		viaMethod.Should().Be(viaBar);
	}

	/// <summary>Moving to the next and previous section from a selected section carries on from there, with and without a lead.</summary>
	[Theory]
	[InlineData(0.0)]
	[InlineData(3.0)]
	public async Task NextAndPreviousFromASelectedSection_CarryOnFromIt(double lead)
	{
		var cut = await RenderWithSegmentsAsync(p => p.Add(x => x.SeekLead, TimeSpan.FromSeconds(lead)));

		await ClickBarAsync(cut, NewsBar);
		var next = await cut.InvokeAsync(() => cut.Instance.NextSegmentAsync());
		var next2 = await cut.InvokeAsync(() => cut.Instance.NextSegmentAsync());
		var previous = await cut.InvokeAsync(() => cut.Instance.PreviousSegmentAsync());

		next!.Id.Should().Be("ad");
		next2!.Id.Should().Be("song2");
		previous!.Id.Should().Be("ad");
		cut.Instance.CurrentSegment!.Id.Should().Be("ad");
	}

	/// <summary>Playing a section on its own starts at exactly its start, whatever the lead.</summary>
	[Fact]
	public async Task PlayingASectionOnItsOwn_IgnoresTheLead()
	{
		var cut = await RenderWithSegmentsAsync(p => p.Add(x => x.SeekLead, TimeSpan.FromSeconds(3)));

		await cut.FindAll(".pdmp-segment-bar")[NewsBar].DoubleClickAsync(new MouseEventArgs());

		SeekTargets().Should().Equal(120.0);
		cut.Instance.PlayingSegment!.Id.Should().Be("talk");
		cut.Instance.CurrentSegment!.Id.Should().Be("talk");
	}

	/// <summary>Selection works the same when the seek bar follows the zoomed view of a visualisation.</summary>
	[Theory]
	[InlineData(0.0)]
	[InlineData(3.0)]
	public async Task WhenTheSeekBarFollowsTheView_SelectionIsUnchanged(double lead)
	{
		var cut = await RenderWithSegmentsAsync(p =>
		{
			p.Add(x => x.SeekLead, TimeSpan.FromSeconds(lead));
			p.Add(x => x.ShowSpectrogram, true);
			p.Add(x => x.SeekBarFollowsView, true);
			p.Add(x => x.SpectrogramWindow, TimeSpan.FromSeconds(12));
		});

		cut.FindAll(".pdmp-segment-lanes.pdmp-follow-view").Should().HaveCount(1);
		await ClickBarAsync(cut, AdvertBar);

		SeekTargets().Should().Equal(170.0 - lead);
		cut.Instance.CurrentSegment!.Id.Should().Be("ad");
		CurrentBarLabel(cut).Should().Contain("Advert");
	}

	/// <summary>Sections of a hidden kind have no bar and are skipped when selecting by navigation.</summary>
	[Fact]
	public async Task HiddenKinds_HaveNoBarAndAreSkipped()
	{
		var cut = await RenderWithSegmentsAsync(p => p.Add(x => x.VisibleSegmentKinds, new HashSet<string> { "song" }));

		cut.FindAll(".pdmp-segment-bar").Select(b => b.TextContent).Should().Equal("Artist: First song", "Long set");
		await ClickBarAsync(cut, 1);
		SeekTargets().Should().Equal(240.0);
		cut.Instance.CurrentSegment!.Id.Should().Be("song2");
	}

	/// <summary>Until the media is ready its duration is unknown, so there are no bars to select.</summary>
	[Fact]
	public void BeforeTheMediaIsReady_ThereAreNoBars()
	{
		var cut = RenderPlayer(p => p.Add(x => x.Segments, _segments));

		cut.FindAll(".pdmp-segment-bar").Should().BeEmpty();
		cut.FindAll(".pdmp-segment-button").Should().NotBeEmpty();
	}

	/// <summary>A section that is not one of the player's own can still be sought to, and the lead applies to it.</summary>
	[Fact]
	public async Task ASectionNotInTheList_CanBeSoughtTo()
	{
		var cut = await RenderWithSegmentsAsync(p => p.Add(x => x.SeekLead, TimeSpan.FromSeconds(2)));
		var other = new PDMediaSegment { Id = "other", Start = TimeSpan.FromSeconds(500), End = TimeSpan.FromSeconds(600) };

		await cut.InvokeAsync(() => cut.Instance.SeekToSegmentAsync(other));

		SeekTargets().Should().Equal(498.0);
	}

	// ---- markers: the same lead rules, so that markers and sections cannot drift apart ----

	/// <summary>Selecting a marker goes to its position less the lead, and that marker is then the current one.</summary>
	[Theory]
	[InlineData(0.0, 60.0, "Gap")]
	[InlineData(3.0, 57.0, "Gap")]
	public async Task SelectingAMarker_GoesToItsPositionLessTheLeadAndMakesItCurrent(double lead, double target, string label)
	{
		var selected = new List<string>();
		var cut = await RenderWithMarkersAsync(p => p
			.Add(x => x.SeekLead, TimeSpan.FromSeconds(lead))
			.Add(x => x.OnMarkerSelected, m => selected.Add(m.Id)));
		await RaiseAsync(cut, "timeupdate", State(time: 20));

		await cut.InvokeAsync(() => cut.Instance.SeekToMarkerAsync(cut.Instance.SortedMarkers[0]));

		SeekTargets().Should().Equal(target);
		cut.Instance.CurrentMarker!.Label.Should().Be(label);
		selected.Should().Equal("dead-air-60");
		cut.Find("input.pdmp-seek").GetAttribute("value").Should().Be(target.ToString(System.Globalization.CultureInfo.InvariantCulture));
	}

	/// <summary>Walking forward and back through markers lands on each in turn, with and without a lead.</summary>
	[Theory]
	[InlineData(0.0)]
	[InlineData(3.0)]
	public async Task NextAndPreviousMarker_WalkInOrderWithTheLead(double lead)
	{
		var cut = await RenderWithMarkersAsync(p => p.Add(x => x.SeekLead, TimeSpan.FromSeconds(lead)));
		var visited = new List<string>();

		for (var i = 0; i < 3; i++)
		{
			visited.Add((await cut.InvokeAsync(() => cut.Instance.NextMarkerAsync()))!.Label!);
			cut.Instance.CurrentMarker!.Label.Should().Be(visited[^1]);
		}

		visited.Add((await cut.InvokeAsync(() => cut.Instance.PreviousMarkerAsync()))!.Label!);

		visited.Should().Equal("Gap", "Overlap", "Loud", "Overlap");
		SeekTargets().Should().Equal(60.0 - lead, 75.0 - lead, 300.0 - lead, 75.0 - lead);
	}
}
