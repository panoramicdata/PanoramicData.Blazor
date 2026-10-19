namespace PanoramicData.Blazor.Models;

/// <summary>A pitched event on a media player's piano roll; drum pitches may be nominal.</summary>
public sealed record PDMediaNote
{
	/// <summary>Start offset from the beginning of the media.</summary>
	public TimeSpan Start { get; init; }

	/// <summary>Written duration of the note.</summary>
	public TimeSpan Duration { get; init; }

	/// <summary>MIDI pitch, from 0 to 127, where 60 is middle C.</summary>
	public int MidiNote { get; init; }

	/// <summary>Voice identifier used by HiddenNoteVoices.</summary>
	public string Voice { get; init; } = string.Empty;

	/// <summary>Optional hexadecimal CSS colour, such as #20c997.</summary>
	public string? Color { get; init; }

	/// <summary>Optional plain text shown in the tooltip.</summary>
	public string? Label { get; init; }
}
