namespace PanoramicData.Blazor.Enums;

/// <summary>
/// Specifies how much of a media file the browser should load before playback starts, as the <c>preload</c>
/// attribute of the media element.
/// </summary>
public enum MediaPreload
{
	/// <summary>Load nothing until the user plays the media.</summary>
	None,

	/// <summary>Load only the metadata (such as the duration). This is the default.</summary>
	Metadata,

	/// <summary>Let the browser load as much as it sees fit.</summary>
	Auto
}
