namespace PanoramicData.Blazor.Enums;

/// <summary>
/// Specifies how the visualisation of a <see cref="PDMediaPlayer"/> follows the playhead while the media plays.
/// </summary>
public enum MediaFollowMode
{
	/// <summary>The view stays where the zoom and pan put it, and the playhead moves across it, possibly off the edge.</summary>
	Free,

	/// <summary>The view scrolls only as much as needed to keep the playhead a minimum distance from the edges.</summary>
	Margin,

	/// <summary>The playhead stays in the middle of the view and the media scrolls past it.</summary>
	Centre
}
