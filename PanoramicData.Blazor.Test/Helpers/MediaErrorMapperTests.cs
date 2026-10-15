using AwesomeAssertions;
using PanoramicData.Blazor.Enums;
using PanoramicData.Blazor.Helpers;

namespace PanoramicData.Blazor.Test.Helpers;

/// <summary>Tests for <see cref="MediaErrorMapper"/>.</summary>
public class MediaErrorMapperTests
{
	/// <summary>Only network and source errors on HTTP sources are worth asking the server about.</summary>
	[Theory]
	[InlineData(2, "/show.wav", true)]
	[InlineData(4, "https://example.com/show.wav", true)]
	[InlineData(3, "/show.wav", false)]
	[InlineData(1, "/show.wav", false)]
	[InlineData(null, "/show.wav", false)]
	[InlineData(4, "blob:https://example.com/123", false)]
	[InlineData(4, "DATA:audio/wav;base64,AAAA", false)]
	[InlineData(4, " ", false)]
	[InlineData(4, null, false)]
	public void ShouldProbe_OnlyForHttpSourceErrors(int? code, string? src, bool expected)
		=> MediaErrorMapper.ShouldProbe(code, src).Should().Be(expected);

	/// <summary>An HTTP error status takes precedence and is kept on the error.</summary>
	[Theory]
	[InlineData(401, MediaErrorKind.Unauthorized, "sign in")]
	[InlineData(403, MediaErrorKind.Forbidden, "permission")]
	[InlineData(404, MediaErrorKind.NotFound, "could not be found")]
	[InlineData(410, MediaErrorKind.NotFound, "could not be found")]
	[InlineData(500, MediaErrorKind.ServerError, "status 500")]
	[InlineData(416, MediaErrorKind.ServerError, "status 416")]
	public void Map_HttpStatus(int status, MediaErrorKind kind, string text)
	{
		var error = MediaErrorMapper.Map(4, "DEMUXER_ERROR_COULD_NOT_OPEN", status);

		error.Kind.Should().Be(kind);
		error.Message.Should().Contain(text);
		error.StatusCode.Should().Be(status);
		error.MediaErrorCode.Should().Be(4);
		error.BrowserMessage.Should().Be("DEMUXER_ERROR_COULD_NOT_OPEN");
	}

	/// <summary>Without an error status, the browser's code decides; a success status is not kept.</summary>
	[Theory]
	[InlineData(1, MediaErrorKind.Aborted)]
	[InlineData(2, MediaErrorKind.Network)]
	[InlineData(3, MediaErrorKind.Decode)]
	[InlineData(4, MediaErrorKind.NotSupported)]
	[InlineData(null, MediaErrorKind.Unknown)]
	[InlineData(99, MediaErrorKind.Unknown)]
	public void Map_BrowserCode(int? code, MediaErrorKind kind)
	{
		var error = MediaErrorMapper.Map(code, " ", 206);

		error.Kind.Should().Be(kind);
		error.Message.Should().NotBeNullOrWhiteSpace();
		error.StatusCode.Should().BeNull();
		error.BrowserMessage.Should().BeNull();
	}
}
