namespace PanoramicData.Blazor.Models;

/// <summary>
/// Describes why a <see cref="PDMediaPlayer"/> could not play its media.
/// </summary>
/// <param name="Kind">The classified reason.</param>
/// <param name="Message">A friendly message suitable for showing to the user.</param>
/// <param name="StatusCode">The HTTP status the server returned for the media, when it could be found out.</param>
/// <param name="MediaErrorCode">The browser's <c>MediaError.code</c> (1 aborted, 2 network, 3 decode, 4 not supported), if any.</param>
/// <param name="BrowserMessage">The browser's own diagnostic message, if any. It is not meant for end users.</param>
public sealed record MediaPlayerError(
	MediaErrorKind Kind,
	string Message,
	int? StatusCode = null,
	int? MediaErrorCode = null,
	string? BrowserMessage = null);
