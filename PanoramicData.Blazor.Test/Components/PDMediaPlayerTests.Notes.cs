using AwesomeAssertions;
using Bunit;
using PanoramicData.Blazor.Models;

namespace PanoramicData.Blazor.Test.Components;

public partial class PDMediaPlayerTests
{
	/// <summary>The note lane is opt-in, clipped and filtered by voice.</summary>
	[Fact]
	public async Task Notes_AreOptInAndClipToMediaDuration()
	{
		PDMediaNote[] notes =
		[
			new() { Start = TimeSpan.FromSeconds(9), Duration = TimeSpan.FromSeconds(3), MidiNote = 60, Voice = "Pad", Label = "C4", Color = "#abcdef" },
			new() { Start = TimeSpan.FromSeconds(2), Duration = TimeSpan.FromSeconds(1), MidiNote = 64, Voice = "Melody" },
			new() { Duration = TimeSpan.Zero, MidiNote = 62 },
			new() { Duration = TimeSpan.FromSeconds(1), MidiNote = 128 },
			new() { Start = TimeSpan.FromSeconds(-1), Duration = TimeSpan.FromSeconds(1), MidiNote = 60 }
		];
		var cut = await RenderReadyAsync(p => p.Add(x => x.Notes, notes), 10);
		cut.FindAll(".pdmp-piano").Should().BeEmpty();
		cut.Render(p => p.Add(x => x.ShowNotes, true));
		cut.FindAll(".pdmp-note").Should().HaveCount(2);
		cut.Find(".pdmp-note").GetAttribute("style").Should().Contain("left:90%;width:10%")
			.And.Contain("#abcdef");
		cut.Find(".pdmp-pitch-labels").TextContent.Should().Contain("C4");
		cut.Render(p => p.Add(x => x.HiddenNoteVoices, new[] { "Pad" }));
		cut.FindAll(".pdmp-note").Should().HaveCount(1);
		cut.Find(".pdmp-note").GetAttribute("aria-label").Should().Contain("Melody");
		cut.Render(p => p.Add(x => x.Notes, Array.Empty<PDMediaNote>()));
		cut.FindAll(".pdmp-note").Should().BeEmpty();
	}

	/// <summary>Notes seek to their start and accept only hexadecimal colours.</summary>
	[Fact]
	public async Task NoteButton_SeeksAndRejectsUnsafeColour()
	{
		var cut = await RenderReadyAsync(p => p.Add(x => x.ShowNotes, true).Add(x => x.Notes, new[]
		{
			new PDMediaNote { Start = TimeSpan.FromSeconds(2), Duration = TimeSpan.FromSeconds(1), MidiNote = 69, Color = "red;position:fixed" }
		}), 10);
		cut.Find(".pdmp-note").GetAttribute("style").Should().Contain("#20c997");
		await cut.Find(".pdmp-note").ClickAsync(new());
		SeekTargets().Should().Contain(2);
	}
	/// <summary>Pitch and frequency views can coexist and be switched off independently.</summary>
	[Fact]
	public async Task Notes_AndSpectrogram_RenderIndependently()
	{
		var cut = await RenderReadyAsync(p => p
			.Add(x => x.ShowNotes, true)
			.Add(x => x.ShowSpectrogram, true), 10);
		cut.FindAll(".pdmp-piano").Should().HaveCount(1);
		cut.FindAll("canvas.pdmp-visual-canvas").Should().HaveCount(1);
		cut.Render(p => p.Add(x => x.ShowNotes, false));
		cut.FindAll(".pdmp-piano").Should().BeEmpty();
		cut.FindAll("canvas.pdmp-visual-canvas").Should().HaveCount(1);
		cut.Render(p => p.Add(x => x.ShowNotes, true).Add(x => x.ShowSpectrogram, false));
		cut.FindAll(".pdmp-piano").Should().HaveCount(1);
		cut.FindAll("canvas.pdmp-visual-canvas").Should().BeEmpty();
	}

}
