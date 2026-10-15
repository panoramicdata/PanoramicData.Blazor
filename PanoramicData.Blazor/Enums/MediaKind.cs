namespace PanoramicData.Blazor.Enums;

/// <summary>
/// Specifies which native HTML media element a <see cref="PDMediaPlayer"/> renders.
/// </summary>
public enum MediaKind
{
	/// <summary>An <c>audio</c> element; only the player's own controls are visible.</summary>
	Audio,

	/// <summary>A <c>video</c> element, shown above the player's controls.</summary>
	Video
}
