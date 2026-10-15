using AwesomeAssertions;
using Bunit;
using Microsoft.AspNetCore.Components.Web;
using PanoramicData.Blazor.Enums;
using PanoramicData.Blazor.Models;

namespace PanoramicData.Blazor.Test.Components;

/// <summary>
/// Tests for the markers of <see cref="PDMediaPlayer"/>: the seek bar ticks, the list, the kind filter and
/// navigation.
/// </summary>
public partial class PDMediaPlayerTests
{
	private static PDMediaMarker Marker(double seconds, string kind, string? label = null, double? length = null, string? color = null, string? css = null, string? detail = null)
		=> new()
		{
			Id = $"{kind}-{seconds}",
			Position = TimeSpan.FromSeconds(seconds),
			Length = length is { } l ? TimeSpan.FromSeconds(l) : null,
			Kind = kind,
			Label = label ?? $"{kind} at {seconds}",
			Color = color,
			CssClass = css,
			Detail = detail
		};

	// deliberately unsorted, with an overlap and one past the end of the media
	private static readonly PDMediaMarker[] _markers =
	[
		Marker(300, "clipping", "Loud"),
		Marker(60, "dead-air", "Gap", length: 30, detail: "Below -60 dBFS"),
		Marker(75, "clipping", "Overlap"),
		Marker(4000, "dead-air", "After the end"),
		Marker(600, "segment", "News")
	];

	private Task<IRenderedComponent<PDMediaPlayer>> RenderWithMarkersAsync(Action<ComponentParameterCollectionBuilder<PDMediaPlayer>>? configure = null)
		=> RenderReadyAsync(p =>
		{
			p.Add(x => x.Markers, _markers);
			configure?.Invoke(p);
		});

	private static IReadOnlyList<string> ListedLabels(IRenderedComponent<PDMediaPlayer> cut)
		=> [.. cut.FindAll(".pdmp-marker-label").Select(e => e.TextContent)];

	/// <summary>Markers are listed in time order with their time, kind and label, and drawn on the seek bar.</summary>
	[Fact]
	public async Task Markers_AreListedInOrderAndDrawn()
	{
		var cut = await RenderWithMarkersAsync(p => p.Add(x => x.KindLabels, new Dictionary<string, string> { ["Dead-Air"] = "Dead air" }));

		ListedLabels(cut).Should().Equal("Gap", "Overlap", "Loud", "News", "After the end");
		cut.FindAll(".pdmp-marker-time").Select(e => e.TextContent).Should().Equal(
			"0:01:00 to 0:01:30", "0:01:15", "0:05:00", "0:10:00", "1:06:40");
		cut.FindAll(".pdmp-marker-kind").Select(e => e.TextContent).Should().Equal("Dead air", "clipping", "clipping", "segment", "Dead air");
		cut.Find(".pdmp-marker-detail").TextContent.Should().Be("Below -60 dBFS");
		cut.Find(".pdmp-markers-toggle .badge").TextContent.Should().Be("5");
		cut.Instance.SortedMarkers.Should().HaveCount(5);

		var ticks = cut.FindAll(".pdmp-tick");
		ticks.Should().HaveCount(5);
		cut.FindAll(".pdmp-tick-span").Should().ContainSingle();
		ticks.Select(t => t.GetAttribute("style")).Should().Contain(s => s!.StartsWith("left:100%;", StringComparison.Ordinal));
		ticks.Select(t => t.GetAttribute("title")).Should().Contain("0:01:00 to 0:01:30 Dead air: Gap");
		cut.Find(".pdmp-lane").GetAttribute("aria-hidden").Should().Be("true");
	}

	/// <summary>Kinds get their colour from the marker, then KindColors, then the built-in palette; unsafe colours are refused.</summary>
	[Fact]
	public async Task Markers_AreColouredByKind()
	{
		PDMediaMarker[] markers =
		[
			Marker(10, "a"),
			Marker(20, "b", color: "#123456"),
			Marker(30, "c"),
			Marker(40, "c", color: "red; x: y"),
			Marker(50, "", label: "No kind")
		];
		var cut = await RenderReadyAsync(p => p
			.Add(x => x.Markers, markers)
			.Add(x => x.KindColors, new Dictionary<string, string> { ["C"] = "rgb(1, 2, 3)", ["a"] = "url(x)" }));

		var styles = cut.FindAll(".pdmp-marker-button").Select(e => e.GetAttribute("style")).ToList();
		styles[0].Should().Be("--pdmp-marker-color:#dc3545");
		styles[1].Should().Be("--pdmp-marker-color:#123456");
		styles[2].Should().Be("--pdmp-marker-color:rgb(1, 2, 3)");
		styles[3].Should().Be("--pdmp-marker-color:rgb(1, 2, 3)");
		styles[4].Should().Be("--pdmp-marker-color:#0d6efd");
		cut.FindAll(".pdmp-kind-chip")[0].TextContent.Trim().Should().StartWith("Marker");
	}

	/// <summary>Kind chips show each kind with its count, and toggling one hides that kind from the bar and the list.</summary>
	[Fact]
	public async Task KindChip_TogglesTheKind()
	{
		IReadOnlySet<string>? raised = null;
		var cut = await RenderWithMarkersAsync(p => p.Add(x => x.VisibleKindsChanged, k => raised = k));

		var chips = cut.FindAll(".pdmp-kind-chip");
		chips.Select(c => c.TextContent.Trim().Replace("\n", string.Empty, StringComparison.Ordinal)).Should().HaveCount(3);
		chips[0].TextContent.Should().Contain("clipping").And.Contain("2");
		chips[0].GetAttribute("aria-pressed").Should().Be("true");

		await chips[0].ClickAsync(new MouseEventArgs());

		raised.Should().BeEquivalentTo(["dead-air", "segment"]);
		cut.Instance.IsKindVisible("clipping").Should().BeFalse();
		cut.Find(".pdmp-kind-chip").GetAttribute("aria-pressed").Should().Be("false");
		cut.Find(".pdmp-kind-chip").ClassList.Should().Contain("pdmp-kind-off");
		ListedLabels(cut).Should().Equal("Gap", "News", "After the end");
		cut.FindAll(".pdmp-tick").Should().HaveCount(3);
		cut.Instance.VisibleMarkers.Should().HaveCount(3);

		await cut.Find(".pdmp-kind-chip").ClickAsync(new MouseEventArgs());
		ListedLabels(cut).Should().HaveCount(5);
	}

	/// <summary>Setting a kind to the visibility it already has changes nothing.</summary>
	[Fact]
	public async Task SetKindVisible_NoChange_DoesNothing()
	{
		var raisedCount = 0;
		var cut = await RenderWithMarkersAsync(p => p.Add(x => x.VisibleKindsChanged, _ => raisedCount++));

		await cut.InvokeAsync(() => cut.Instance.SetKindVisibleAsync("segment", true));
		await cut.InvokeAsync(() => cut.Instance.SetKindVisibleAsync("segment", false));
		await cut.InvokeAsync(() => cut.Instance.SetKindVisibleAsync(" Segment ", false));
		await cut.InvokeAsync(() => cut.Instance.SetKindVisibleAsync(null!, false));

		raisedCount.Should().Be(1);
		cut.Instance.IsKindVisible("SEGMENT").Should().BeFalse();
		cut.Instance.IsKindVisible(null!).Should().BeFalse();
	}

	/// <summary>The VisibleKinds parameter filters without regard to case; hiding everything shows a message.</summary>
	[Fact]
	public async Task VisibleKindsParameter_Filters()
	{
		var cut = await RenderWithMarkersAsync(p => p.Add(x => x.VisibleKinds, new HashSet<string> { "SEGMENT", " dead-air ", null! }));

		ListedLabels(cut).Should().Equal("Gap", "News", "After the end");

		cut.Render(p => p.Add(x => x.VisibleKinds, new HashSet<string>()));

		cut.FindAll(".pdmp-marker-button").Should().BeEmpty();
		cut.Find(".pdmp-markers-empty").TextContent.Should().Be("No markers of the selected kinds.");
		cut.FindAll(".pdmp-lane").Should().BeEmpty();
		cut.Find("button.pdmp-next-marker").HasAttribute("disabled").Should().BeTrue();
	}

	/// <summary>Without markers there is no list, no chips and no marker buttons.</summary>
	[Fact]
	public async Task NoMarkers_HidesTheMarkerUi()
	{
		var cut = await RenderReadyAsync(p => p.Add(x => x.Markers, []));

		cut.FindAll(".pdmp-markers").Should().BeEmpty();
		cut.FindAll(".pdmp-kinds").Should().BeEmpty();
		cut.FindAll("button.pdmp-next-marker").Should().BeEmpty();
		cut.FindAll("button.pdmp-previous-marker").Should().BeEmpty();
		cut.Instance.CurrentMarker.Should().BeNull();
		(await cut.InvokeAsync(() => cut.Instance.NextMarkerAsync())).Should().BeNull();
		SeekTargets().Should().BeEmpty();
	}

	/// <summary>The list can be collapsed and expanded, and can start collapsed.</summary>
	[Fact]
	public async Task MarkerList_Collapses()
	{
		var cut = await RenderWithMarkersAsync(p => p.Add(x => x.Id, "mp"));
		var toggle = cut.Find(".pdmp-markers-toggle");
		toggle.GetAttribute("aria-expanded").Should().Be("true");
		toggle.GetAttribute("aria-controls").Should().Be("mp-markers");
		cut.Find("#mp-markers").Should().NotBeNull();

		await toggle.ClickAsync(new MouseEventArgs());

		cut.Find(".pdmp-markers-toggle").GetAttribute("aria-expanded").Should().Be("false");
		cut.FindAll("#mp-markers").Should().BeEmpty();

		cut.Render(p => p.Add(x => x.MarkerListExpanded, false));
		cut.Render(p => p.Add(x => x.MarkerListExpanded, true));
		cut.FindAll("#mp-markers").Should().ContainSingle();
	}

	/// <summary>With the list hidden the chips move into the player; they can be hidden too. The list can sit beside the player.</summary>
	[Fact]
	public async Task MarkerUiOptions()
	{
		var cut = await RenderWithMarkersAsync(p => p.Add(x => x.ShowMarkerList, false));
		cut.FindAll(".pdmp-markers").Should().BeEmpty();
		cut.FindAll(".pdmp-player .pdmp-kinds").Should().ContainSingle();

		cut.Render(p => p.Add(x => x.ShowKindFilter, false));
		cut.FindAll(".pdmp-kinds").Should().BeEmpty();

		cut.Render(p => p.Add(x => x.MarkerListPlacement, MediaMarkerListPlacement.Side).Add(x => x.ShowMarkerList, true).Add(x => x.MarkerListMaxHeight, "10rem"));
		cut.Find(".pd-media-player").ClassList.Should().Contain("pdmp-side");
		cut.Find(".pdmp-marker-list").GetAttribute("style").Should().Be("max-height:10rem");
	}

	/// <summary>Selecting a marker from the list seeks SeekLead before it and raises OnMarkerSelected.</summary>
	[Fact]
	public async Task ListItem_SeeksWithTheLead()
	{
		var selected = new List<PDMediaMarker>();
		var cut = await RenderWithMarkersAsync(p => p
			.Add(x => x.SeekLead, TimeSpan.FromSeconds(3))
			.Add(x => x.OnMarkerSelected, m => selected.Add(m)));

		await cut.FindAll(".pdmp-marker-button")[3].ClickAsync(new MouseEventArgs());

		SeekTargets().Should().Equal(597.0);
		selected.Select(m => m.Label).Should().Equal("News");
		cut.Find("[aria-current=true] .pdmp-marker-label").TextContent.Should().Be("News");
		cut.Find(".pdmp-current .pdmp-marker-label").TextContent.Should().Be("News");
	}

	/// <summary>Clicking a tick on the seek bar selects its marker; a marker past the end seeks to the end.</summary>
	[Fact]
	public async Task Tick_SelectsItsMarker()
	{
		var selected = new List<string?>();
		var cut = await RenderWithMarkersAsync(p => p.Add(x => x.OnMarkerSelected, m => selected.Add(m.Label)));

		var lastTick = cut.FindAll(".pdmp-tick").Single(t => t.GetAttribute("style")!.StartsWith("left:100%;", StringComparison.Ordinal));
		await lastTick.ClickAsync(new MouseEventArgs());

		selected.Should().Equal("After the end");
		SeekTargets().Should().Equal(3600.0);
	}

	/// <summary>Next and previous walk the visible markers relative to the position, and stop at the first and last.</summary>
	[Fact]
	public async Task NextAndPrevious_WalkTheMarkers()
	{
		var cut = await RenderWithMarkersAsync();

		(await cut.InvokeAsync(() => cut.Instance.PreviousMarkerAsync())).Should().BeNull();
		(await cut.InvokeAsync(() => cut.Instance.NextMarkerAsync()))!.Label.Should().Be("Gap");
		(await cut.InvokeAsync(() => cut.Instance.NextMarkerAsync()))!.Label.Should().Be("Overlap");
		(await cut.InvokeAsync(() => cut.Instance.NextMarkerAsync()))!.Label.Should().Be("Loud");
		(await cut.InvokeAsync(() => cut.Instance.NextMarkerAsync()))!.Label.Should().Be("News");
		(await cut.InvokeAsync(() => cut.Instance.NextMarkerAsync()))!.Label.Should().Be("After the end");
		(await cut.InvokeAsync(() => cut.Instance.NextMarkerAsync())).Should().BeNull();
		(await cut.InvokeAsync(() => cut.Instance.PreviousMarkerAsync()))!.Label.Should().Be("News");

		SeekTargets().Should().Equal(60.0, 75.0, 300.0, 600.0, 3600.0, 600.0);
	}

	/// <summary>Previous returns to the start of the current marker when well into it, and to the one before within the tolerance.</summary>
	[Fact]
	public async Task Previous_UsesTheTolerance()
	{
		var cut = await RenderWithMarkersAsync(p => p.Add(x => x.MarkerTolerance, TimeSpan.FromSeconds(2)));

		await RaiseAsync(cut, "timeupdate", State(time: 310));
		(await cut.InvokeAsync(() => cut.Instance.PreviousMarkerAsync()))!.Label.Should().Be("Loud");

		await RaiseAsync(cut, "timeupdate", State(time: 301));
		(await cut.InvokeAsync(() => cut.Instance.PreviousMarkerAsync()))!.Label.Should().Be("Overlap");

		await RaiseAsync(cut, "timeupdate", State(time: 74));
		(await cut.InvokeAsync(() => cut.Instance.NextMarkerAsync()))!.Label.Should().Be("Loud");
	}

	/// <summary>With a seek lead, navigation counts from the position plus the lead, so it moves on from the marker just reached.</summary>
	[Fact]
	public async Task NextAndPrevious_AllowForTheSeekLead()
	{
		var cut = await RenderWithMarkersAsync(p => p.Add(x => x.SeekLead, TimeSpan.FromSeconds(5)));

		await RaiseAsync(cut, "timeupdate", State(time: 200));
		(await cut.InvokeAsync(() => cut.Instance.NextMarkerAsync()))!.Label.Should().Be("Loud");
		await RaiseAsync(cut, "seeked", State(time: 295));
		cut.Instance.CurrentMarker!.Label.Should().Be("Loud");
		(await cut.InvokeAsync(() => cut.Instance.NextMarkerAsync()))!.Label.Should().Be("News");
		await RaiseAsync(cut, "seeked", State(time: 595));
		(await cut.InvokeAsync(() => cut.Instance.PreviousMarkerAsync()))!.Label.Should().Be("Loud");

		SeekTargets().Should().Equal(295.0, 595.0, 295.0);
	}

	/// <summary>A negative seek lead counts as none, and a lead before the start seeks to the start.</summary>
	[Fact]
	public async Task SeekLead_IsLimited()
	{
		var cut = await RenderWithMarkersAsync(p => p.Add(x => x.SeekLead, TimeSpan.FromSeconds(-5)));
		await cut.InvokeAsync(() => cut.Instance.SeekToMarkerAsync(_markers[1]));

		cut.Render(p => p.Add(x => x.SeekLead, TimeSpan.FromMinutes(5)));
		await cut.InvokeAsync(() => cut.Instance.SeekToMarkerAsync(_markers[1]));

		SeekTargets().Should().Equal(60.0, 0.0);
		var act = () => cut.InvokeAsync(() => cut.Instance.SeekToMarkerAsync(null!));
		await act.Should().ThrowAsync<ArgumentNullException>();
	}

	/// <summary>Hidden kinds are skipped by next and previous, unless a kind is asked for, which ignores the filter.</summary>
	[Fact]
	public async Task Navigation_RespectsTheKindFilter()
	{
		var cut = await RenderWithMarkersAsync(p => p.Add(x => x.VisibleKinds, new HashSet<string> { "segment", "dead-air" }));

		(await cut.InvokeAsync(() => cut.Instance.NextMarkerAsync()))!.Label.Should().Be("Gap");
		(await cut.InvokeAsync(() => cut.Instance.NextMarkerAsync()))!.Label.Should().Be("News");
		(await cut.InvokeAsync(() => cut.Instance.PreviousMarkerAsync("clipping")))!.Label.Should().Be("Loud");
		(await cut.InvokeAsync(() => cut.Instance.NextMarkerAsync("CLIPPING"))).Should().BeNull();
		(await cut.InvokeAsync(() => cut.Instance.PreviousMarkerAsync("dead-air")))!.Label.Should().Be("Gap");
	}

	/// <summary>The previous and next buttons call the navigation.</summary>
	[Fact]
	public async Task MarkerButtons_Navigate()
	{
		var cut = await RenderWithMarkersAsync();

		await cut.Find("button.pdmp-next-marker").ClickAsync(new MouseEventArgs());
		await cut.Find("button.pdmp-next-marker").ClickAsync(new MouseEventArgs());
		await cut.Find("button.pdmp-previous-marker").ClickAsync(new MouseEventArgs());

		SeekTargets().Should().Equal(60.0, 75.0, 60.0);
	}

	/// <summary>The current marker follows the position.</summary>
	[Fact]
	public async Task CurrentMarker_FollowsThePosition()
	{
		var cut = await RenderWithMarkersAsync();
		cut.FindAll("[aria-current]").Should().BeEmpty();

		await RaiseAsync(cut, "timeupdate", State(time: 80));
		cut.Find("[aria-current=true] .pdmp-marker-label").TextContent.Should().Be("Overlap");

		await RaiseAsync(cut, "timeupdate", State(time: 3500));
		cut.Find("[aria-current=true] .pdmp-marker-label").TextContent.Should().Be("News");
	}

	/// <summary>Replacing the markers, or changing the count of the same list, rebuilds everything.</summary>
	[Fact]
	public async Task Markers_ChangeIsPickedUp()
	{
		var list = new List<PDMediaMarker> { Marker(10, "a") };
		var cut = await RenderReadyAsync(p => p.Add(x => x.Markers, list));
		cut.FindAll(".pdmp-marker-button").Should().ContainSingle();

		list.Add(Marker(20, "b"));
		cut.Render(p => p.Add(x => x.Markers, list));
		cut.FindAll(".pdmp-marker-button").Should().HaveCount(2);

		cut.Render(p => p.Add(x => x.Markers, [Marker(5, "c")]));
		ListedLabels(cut).Should().Equal("c at 5");

		cut.Render(p => p.Add(x => x.KindColors, new Dictionary<string, string> { ["c"] = "#000" }).Add(x => x.MarkerTickResolution, 10));
		cut.Find(".pdmp-tick").GetAttribute("style").Should().EndWith("--pdmp-marker-color:#000");
	}

	/// <summary>Thousands of markers are drawn as a bounded number of ticks, and the list renders only some rows.</summary>
	[Fact]
	public async Task ManyMarkers_RenderEfficiently()
	{
		var many = Enumerable.Range(0, 5000).Select(i => Marker(i * 0.72, i % 2 == 0 ? "even" : "odd")).ToArray();

		var cut = await RenderReadyAsync(p => p.Add(x => x.Markers, many).Add(x => x.MarkerTickResolution, 200));

		cut.FindAll(".pdmp-tick").Count.Should().BeLessThanOrEqualTo(400);
		cut.FindAll(".pdmp-tick-merged").Should().NotBeEmpty();
		cut.FindAll(".pdmp-marker-button").Count.Should().BeLessThan(5000);
		cut.Find(".pdmp-markers-toggle .badge").TextContent.Should().Be("5000");
	}

	/// <summary>A marker's own class is applied to its tick and list item.</summary>
	[Fact]
	public async Task MarkerCssClass_IsApplied()
	{
		var cut = await RenderReadyAsync(p => p.Add(x => x.Markers, [Marker(10, "a", css: "special")]));

		cut.Find(".pdmp-tick").ClassList.Should().Contain("special");
		cut.Find("li.special").Should().NotBeNull();
	}

	/// <summary>Markers are not drawn on the bar of a live stream, but are still listed.</summary>
	[Fact]
	public async Task LiveStream_ListsButDoesNotDrawMarkers()
	{
		var cut = RenderPlayer(p => p.Add(x => x.Markers, _markers));

		await RaiseAsync(cut, "loadedmetadata", State(duration: null, infinite: true));

		cut.FindAll(".pdmp-tick").Should().BeEmpty();
		cut.FindAll(".pdmp-marker-button").Should().HaveCount(5);
	}
}
