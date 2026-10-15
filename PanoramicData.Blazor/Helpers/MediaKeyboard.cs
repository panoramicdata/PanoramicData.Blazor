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
	Faster
}

/// <summary>
/// Maps key presses to <see cref="PDMediaPlayer"/> shortcuts. The player only listens while focus is inside it, and
/// its speed menu does not pass key presses on, so the shortcuts never fire while typing elsewhere.
/// </summary>
internal static class MediaKeyboard
{
	/// <summary>
	/// Gets the shortcut for a key press. Presses with Ctrl, Alt or Meta held are never shortcuts, so browser and
	/// system shortcuts keep working.
	/// </summary>
	/// <param name="args">The key press.</param>
	/// <returns>The shortcut, or <see cref="MediaShortcut.None"/>.</returns>
	internal static MediaShortcut Resolve(KeyboardEventArgs args)
	{
		ArgumentNullException.ThrowIfNull(args);
		if (args.CtrlKey || args.AltKey || args.MetaKey || args.Repeat)
		{
			return MediaShortcut.None;
		}

		return args.Key switch
		{
			"k" or "K" => MediaShortcut.TogglePlay,
			"j" or "J" or "[" => MediaShortcut.PreviousMarker,
			"l" or "L" or "]" => MediaShortcut.NextMarker,
			"m" or "M" => MediaShortcut.ToggleMute,
			"<" => MediaShortcut.Slower,
			">" => MediaShortcut.Faster,
			_ => MediaShortcut.None
		};
	}
}
