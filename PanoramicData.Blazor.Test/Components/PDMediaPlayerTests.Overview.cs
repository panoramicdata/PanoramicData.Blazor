using AwesomeAssertions;
using Bunit;
using PanoramicData.Blazor.Models;

namespace PanoramicData.Blazor.Test.Components;

/// <summary>
/// Tests for the overview bar and for the seek bar and piano roll following the view of <see cref="PDMediaPlayer"/>.
/// </summary>
public partial class PDMediaPlayerTests
{
	private static PDMediaNote[] OverviewNotes() =>
	[
		new() { Start = TimeSpan.FromSeconds(1), Duration = TimeSpan.FromSeconds(2), MidiNote = 60, Voice = "melody" },
		new() { Start = TimeSpan.FromSeconds(5), Duration = TimeSpan.FromSeconds(1), MidiNote = 64, Voice = "melody" }
	];

	/// <summary>The overview bar is absent by default and shows with its handles when asked for.</summary>
	[Fact]
	public void Overview_ShowsOnlyWhenAskedFor()
	{
		RenderPlayer(p => p.Add(x => x.ShowSpectrogram, true)).FindAll(".pdmp-overview").Should().BeEmpty();

		var cut = RenderPlayer(p =>
		{
			p.Add(x => x.ShowSpectrogram, true);
			p.Add(x => x.ShowSpectrogramOverview, true);
		});

		cut.FindAll(".pdmp-overview").Should().HaveCount(1);
		cut.FindAll(".pdmp-overview-window").Should().HaveCount(1);
		cut.FindAll(".pdmp-overview-handle").Should().HaveCount(2);
		cut.FindAll(".pdmp-overview-playhead").Should().HaveCount(1);
	}

	/// <summary>The overview belongs to the visualisation, so it does not show without one.</summary>
	[Fact]
	public void Overview_NeedsAVisualisation()
	{
		RenderPlayer(p => p.Add(x => x.ShowSpectrogramOverview, true)).FindAll(".pdmp-overview").Should().BeEmpty();
	}

	/// <summary>The seek bar follows the view only when asked to, and its parts are inside a box that can be zoomed.</summary>
	[Fact]
	public void SeekBar_FollowsTheViewOnlyWhenAskedTo()
	{
		var plain = RenderPlayer(p => p.Add(x => x.ShowSpectrogram, true));
		plain.FindAll(".pdmp-track .pdmp-view-inner").Should().HaveCount(1);
		plain.FindAll(".pdmp-track.pdmp-follow-view").Should().BeEmpty();
		plain.FindAll(".pdmp-segment-lanes.pdmp-follow-view").Should().BeEmpty();

		var follows = RenderPlayer(p =>
		{
			p.Add(x => x.ShowSpectrogram, true);
			p.Add(x => x.SeekBarFollowsView, true);
		});
		follows.FindAll(".pdmp-track.pdmp-follow-view").Should().HaveCount(1);
		follows.FindAll(".pdmp-track.pdmp-follow-view .pdmp-seek").Should().HaveCount(1);
	}

	/// <summary>The piano roll follows the view only when asked to, and its time-positioned parts are in boxes that can be zoomed.</summary>
	[Fact]
	public async Task PianoRoll_FollowsTheViewOnlyWhenAskedTo()
	{
		var plain = await RenderReadyAsync(p =>
		{
			p.Add(x => x.ShowSpectrogram, true);
			p.Add(x => x.ShowNotes, true);
			p.Add(x => x.Notes, OverviewNotes());
		}, duration: 10);
		plain.FindAll(".pdmp-note-grid .pdmp-view-inner .pdmp-note").Should().HaveCount(2);
		plain.FindAll(".pdmp-follow-view").Should().BeEmpty();

		var follows = await RenderReadyAsync(p =>
		{
			p.Add(x => x.ShowSpectrogram, true);
			p.Add(x => x.ShowNotes, true);
			p.Add(x => x.Notes, OverviewNotes());
			p.Add(x => x.NotesFollowView, true);
		}, duration: 10);
		follows.FindAll(".pdmp-note-grid.pdmp-follow-view").Should().HaveCount(1);
		follows.FindAll(".pdmp-note-times.pdmp-follow-view .pdmp-view-inner span").Should().NotBeEmpty();
		follows.FindAll(".pdmp-pitch-labels .pdmp-view-inner").Should().BeEmpty();
	}
}
