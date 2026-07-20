namespace PanoramicData.Blazor.Models;

/// <summary>
/// Identifies the animation used when a <see cref="PanoramicData.Blazor.PDChat"/> toast enters or leaves the screen.
/// The same catalogue is used for both the entry and exit phase; the meaning is direction-aware
/// (for example, <see cref="Grow"/> grows on entry and the mirror shrink on exit).
/// </summary>
public enum PDChatToastAnimation
{
	/// <summary>No animation; the toast appears and disappears instantly.</summary>
	None = 0,

	/// <summary>The toast fades in / out via opacity only.</summary>
	Fade = 1,

	/// <summary>The toast grows from a small scale to full size (default entry animation).</summary>
	Grow = 2,

	/// <summary>The toast shrinks to a small scale (default exit animation).</summary>
	Shrink = 3,

	/// <summary>The toast slides in from / out towards the anchored edge.</summary>
	Slide = 4
}
