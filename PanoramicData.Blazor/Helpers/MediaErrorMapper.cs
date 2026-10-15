namespace PanoramicData.Blazor.Helpers;

/// <summary>
/// Turns what the browser reports about a failed media load into a <see cref="MediaPlayerError"/> with a friendly
/// message. A media element does not expose the HTTP status of its request, so for network and format errors the
/// player asks the server for the first byte of the source to find out whether it was refused (401, 403) or missing
/// (404).
/// </summary>
internal static class MediaErrorMapper
{
	/// <summary>The browser's <c>MEDIA_ERR_ABORTED</c> code.</summary>
	internal const int Aborted = 1;

	/// <summary>The browser's <c>MEDIA_ERR_NETWORK</c> code.</summary>
	internal const int Network = 2;

	/// <summary>The browser's <c>MEDIA_ERR_DECODE</c> code.</summary>
	internal const int Decode = 3;

	/// <summary>The browser's <c>MEDIA_ERR_SRC_NOT_SUPPORTED</c> code, which is also what a refused or missing file gives.</summary>
	internal const int SourceNotSupported = 4;

	/// <summary>
	/// Determines whether the HTTP status of the source is worth finding out: only for network and source errors, and
	/// only for sources fetched over HTTP (not <c>blob:</c> or <c>data:</c> URLs).
	/// </summary>
	/// <param name="errorCode">The browser's error code.</param>
	/// <param name="src">The media source.</param>
	/// <returns>Whether to ask the server.</returns>
	internal static bool ShouldProbe(int? errorCode, string? src)
	{
		if (errorCode is not (Network or SourceNotSupported) || string.IsNullOrWhiteSpace(src))
		{
			return false;
		}

		return !src.StartsWith("blob:", StringComparison.OrdinalIgnoreCase)
			&& !src.StartsWith("data:", StringComparison.OrdinalIgnoreCase);
	}

	/// <summary>
	/// Classifies an error. An HTTP error status, when known, takes precedence over the browser's error code.
	/// </summary>
	/// <param name="errorCode">The browser's error code, if any.</param>
	/// <param name="browserMessage">The browser's message, if any.</param>
	/// <param name="statusCode">The HTTP status of the source, if it was found out.</param>
	/// <returns>The error.</returns>
	internal static MediaPlayerError Map(int? errorCode, string? browserMessage, int? statusCode)
	{
		var message = string.IsNullOrWhiteSpace(browserMessage) ? null : browserMessage;
		var (kind, text) = statusCode switch
		{
			401 => (MediaErrorKind.Unauthorized, "You need to sign in to play this media."),
			403 => (MediaErrorKind.Forbidden, "You do not have permission to play this media."),
			404 or 410 => (MediaErrorKind.NotFound, "The media could not be found."),
			>= 400 and < 600 => (MediaErrorKind.ServerError, string.Create(CultureInfo.InvariantCulture, $"The server could not supply the media (status {statusCode}).")),
			_ => MapCode(errorCode)
		};

		return new MediaPlayerError(kind, text, statusCode is >= 400 and < 600 ? statusCode : null, errorCode, message);
	}

	private static (MediaErrorKind Kind, string Text) MapCode(int? errorCode) => errorCode switch
	{
		Aborted => (MediaErrorKind.Aborted, "Loading the media was stopped."),
		Network => (MediaErrorKind.Network, "A network error stopped the media from loading."),
		Decode => (MediaErrorKind.Decode, "The media could not be decoded; the file may be damaged."),
		SourceNotSupported => (MediaErrorKind.NotSupported, "The media format is not supported, or the file could not be opened."),
		_ => (MediaErrorKind.Unknown, "The media could not be played.")
	};
}
