namespace PanoramicData.Blazor.Enums;

/// <summary>
/// Classifies why a <see cref="PDMediaPlayer"/> could not play its media.
/// </summary>
public enum MediaErrorKind
{
	/// <summary>The reason is not known.</summary>
	Unknown,

	/// <summary>Loading was stopped before it finished.</summary>
	Aborted,

	/// <summary>A network problem stopped the media from loading.</summary>
	Network,

	/// <summary>The media was received but could not be decoded.</summary>
	Decode,

	/// <summary>The media format is not supported, or the file could not be opened.</summary>
	NotSupported,

	/// <summary>The server answered 401: the user is not signed in.</summary>
	Unauthorized,

	/// <summary>The server answered 403: the user may not play this media.</summary>
	Forbidden,

	/// <summary>The server answered 404 or 410: the media does not exist.</summary>
	NotFound,

	/// <summary>The server answered with another error status, such as 500.</summary>
	ServerError
}
