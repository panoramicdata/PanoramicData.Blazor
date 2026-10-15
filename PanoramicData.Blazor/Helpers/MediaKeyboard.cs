namespace PanoramicData.Blazor.Helpers;

/// <summary>
/// The actions a <see cref="PDMediaPlayer"/> keyboard shortcut can perform.
/// </summary>
internal enum MediaShortcut
{
	/// <summary>The key is not a shortcut.</summary>
	None,

	/// <summary>Play or pause (K).</summary>
	TogglePlay,

	/// <summary>Go to the previous marker (J or [).</summary>
	PreviousMarker,

	/// <summary>Go to the next marker (L or ]).</summary>
	NextMarker,

	/// <summary>Mute or unmute (M).</summary>
	ToggleMute,

	/// <summary>Choose the next slower rate (&lt;).</summary>
	Slower,

	/// <summary>Choose the next faster rate (&gt;).</summary>
	Faster,

	/// <summary>Go to the previous segment (,).</summary>
	PreviousSegment,

	/// <summary>Go to the next segment (.).</summary>
	NextSegment,

	/// <summary>Play the current segment on its own (P).</summary>
	PlaySegment
}

/// <summary>
/// Maps key presses to <see cref="PDMediaPlayer"/> shortcuts. The player only listens while focus is inside it, and
/// its speed menu does not pass key presses on, so the shortcuts never fire while typing elsewhere.
/// </summary>
internal static class MediaKeyboard
{
	// the shortcut for each key; keys are compared exactly, so both cases of a letter are listed
	private static readonly Dictionary<string, MediaShortcut> _shortcuts = new(StringComparer.Ordinal)
	{
		["k"] = MediaShortcut.TogglePlay,
		["K"] = MediaShortcut.TogglePlay,
		["j"] = MediaShortcut.PreviousMarker,
		["J"] = MediaShortcut.PreviousMarker,
		["["] = MediaShortcut.PreviousMarker,
		["l"] = MediaShortcut.NextMarker,
		["L"] = MediaShortcut.NextMarker,
		["]"] = MediaShortcut.NextMarker,
		["m"] = MediaShortcut.ToggleMute,
		["M"] = MediaShortcut.ToggleMute,
		["<"] = MediaShortcut.Slower,
		[">"] = MediaShortcut.Faster,
		[","] = MediaShortcut.PreviousSegment,
		["."] = MediaShortcut.NextSegment,
		["p"] = MediaShortcut.PlaySegment,
		["P"] = MediaShortcut.PlaySegment
	};

	/// <summary>
	/// Gets the shortcut for a key press. Presses with Ctrl, Alt or Meta held are never shortcuts, so browser and
	/// system shortcuts keep working, and neither are held-down repeats.
	/// </summary>
	/// <param name="args">The key press.</param>
	/// <returns>The shortcut, or <see cref="MediaShortcut.None"/>.</returns>
	internal static MediaShortcut Resolve(KeyboardEventArgs args)
	{
		ArgumentNullException.ThrowIfNull(args);
		var modified = args.CtrlKey || args.AltKey || args.MetaKey || args.Repeat;
		return modified || args.Key is null ? MediaShortcut.None : _shortcuts.GetValueOrDefault(args.Key, MediaShortcut.None);
	}
}