using System.Globalization;
using AwesomeAssertions;
using Bunit;

namespace PanoramicData.Blazor.Test;

/// <summary>
/// Tests that an invalid timestamp format falls back to the default rather than failing to render the message.
/// </summary>
public partial class PDMessageTests
{
	/// <summary>Verifies that a format .NET cannot use shows the time in the default format.</summary>
	[Theory]
	[InlineData("H")]
	[InlineData("%")]
	[InlineData("'unterminated")]
	public void An_invalid_timestamp_format_falls_back_to_the_default(string format)
	{
		var component = RenderMessage(Message(true), p => p.Add(x => x.MessageTimestampFormat, format));

		component.Find(".pdchat-timestamp").TextContent.Should()
			.Be(Sent.ToLocalTime().ToString("HH:mm:ss", CultureInfo.CurrentCulture));
	}
}
