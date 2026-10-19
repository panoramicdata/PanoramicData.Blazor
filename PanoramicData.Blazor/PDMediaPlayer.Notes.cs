namespace PanoramicData.Blazor;

public partial class PDMediaPlayer
{
	/// <summary>Whether to show a separate piano-roll lane with pitch labels. Default false.</summary>
	[Parameter]
	public bool ShowNotes { get; set; }

	/// <summary>Notes positioned on the media timeline; invalid pitches and nonpositive durations are ignored.</summary>
	[Parameter]
	public IReadOnlyList<PDMediaNote> Notes { get; set; } = [];

	/// <summary>Voice identifiers to omit from the piano roll; this does not mute the media audio.</summary>
	[Parameter]
	public IReadOnlyList<string> HiddenNoteVoices { get; set; } = [];

	private static readonly string[] _pitchNames = ["C", "C#", "D", "D#", "E", "F", "F#", "G", "G#", "A", "A#", "B"];

	private static string NoteName(int midi) => _pitchNames[midi % 12] + ((midi / 12) - 1).ToString(CultureInfo.InvariantCulture);

	private IReadOnlyList<PDMediaNote> VisibleNotes => Notes.Where(n => n.MidiNote is >= 0 and <= 127
		&& n.Duration > TimeSpan.Zero && n.Start >= TimeSpan.Zero && n.Start.TotalSeconds < DurationSeconds
		&& !HiddenNoteVoices.Contains(n.Voice, StringComparer.Ordinal)).ToArray();

	private string NoteStyle(PDMediaNote note, int low, int high)
	{
		var left = 100 * note.Start.TotalSeconds / DurationSeconds;
		var width = 100 * Math.Min(note.Duration.TotalSeconds, DurationSeconds - note.Start.TotalSeconds) / DurationSeconds;
		var bottom = 100.0 * (note.MidiNote - low) / (high - low + 1);
		var height = 100.0 / (high - low + 1);
		var color = note.Color is { Length: 4 or 7 or 9 } c && c[0] == '#' && c.Skip(1).All(Uri.IsHexDigit) ? c : "#20c997";
		return $"left:{Percent(left)};width:{Percent(width)};bottom:{Percent(bottom)};height:{Percent(height)};background-color:{color}";
	}

	private static string NoteTitle(PDMediaNote note) => $"{note.Label ?? NoteName(note.MidiNote)} ({note.Voice}), {FormatNumber(note.Start.TotalSeconds)} s, {FormatNumber(note.Duration.TotalSeconds)} s";
}
