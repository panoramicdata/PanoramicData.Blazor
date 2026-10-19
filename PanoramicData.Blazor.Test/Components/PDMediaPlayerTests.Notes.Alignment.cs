using AwesomeAssertions;
using Bunit;
using PanoramicData.Blazor.Models;

namespace PanoramicData.Blazor.Test.Components;

public partial class PDMediaPlayerTests
{
	/// <summary>Section annotations use the same clipped percentages as the player's segment lanes.</summary>
	[Fact]
	public async Task NoteSections_ClipAtMediaBoundariesAndSeek()
	{
		var cut = await RenderReadyAsync(p => p.Add(x => x.ShowNotes, true)
			.Add(x => x.SeekLead, TimeSpan.Zero)
			.Add(x => x.Segments, new[]
			{
				new PDMediaSegment { Start = TimeSpan.FromSeconds(-2), End = TimeSpan.FromSeconds(4), Label = "Verse A", Kind = "verse" },
				new PDMediaSegment { Start = TimeSpan.FromSeconds(8), End = TimeSpan.FromSeconds(12), Label = "Chorus B", Kind = "chorus" }
			}), 10);
		var labels = cut.FindAll(".pdmp-note-section");
		labels.Should().HaveCount(2);
		labels[0].GetAttribute("style").Should().Contain("left:0%;width:40%");
		labels[1].GetAttribute("style").Should().Contain("left:80%;width:20%");
		await labels[1].ClickAsync(new());
		SeekTargets().Should().Contain(8);
		cut.Render(p => p.Add(x => x.ShowNoteSegments, false));
		cut.FindAll(".pdmp-note-section").Should().BeEmpty();
		cut.FindAll(".pdmp-note-boundary").Should().BeEmpty();
		cut.FindAll(".pdmp-segment-bar").Should().HaveCount(2);
	}

	/// <summary>Long recordings receive a bounded set of ticks, including both ends.</summary>
	[Theory]
	[InlineData(0.5)]
	[InlineData(120)]
	[InlineData(300)]
	[InlineData(7200)]
	public async Task NoteTimeLabels_AreBoundedAndIncludeBothEnds(double seconds)
	{
		var cut = await RenderReadyAsync(p => p.Add(x => x.ShowNotes, true), seconds);
		var ticks = cut.FindAll(".pdmp-note-times span");
		ticks.Count.Should().BeInRange(2, 8);
		ticks[0].GetAttribute("style").Should().Be("left:0%");
		ticks[^1].GetAttribute("style").Should().Be("left:100%");
		ticks[0].TextContent.Should().Be("0 s");
		ticks[^1].TextContent.Should().Be($"{seconds.ToString(System.Globalization.CultureInfo.InvariantCulture)} s");
	}

	/// <summary>Notes and playhead use the full time-axis width; pitch labels are inside that grid.</summary>
	[Fact]
	public async Task NoteGrid_ContainsPitchLabelsAndSharesTimePercentages()
	{
		var cut = await RenderReadyAsync(p => p.Add(x => x.ShowNotes, true).Add(x => x.NotesHeight, 1200)
			.Add(x => x.Notes, new[] { new PDMediaNote { Start = TimeSpan.FromSeconds(5), Duration = TimeSpan.FromSeconds(1), MidiNote = 60 } }), 10);
		cut.FindAll(".pdmp-note-grid .pdmp-pitch-labels").Should().HaveCount(1);
		cut.Find(".pdmp-note-grid").GetAttribute("style").Should().Be("height:800px");
		cut.Find(".pdmp-note").GetAttribute("style").Should().Contain("left:50%;width:10%");
		await RaiseAsync(cut, "timeupdate", State(time: 5, duration: 10));
		cut.Find(".pdmp-note-playhead").GetAttribute("style").Should().Be("left:50%");
		cut.Find(".pdmp-progress").GetAttribute("style").Should().Be("width:50%");
	}
}
