using AwesomeAssertions;
using PanoramicData.Blazor.Models;

namespace PanoramicData.Blazor.Test.Models;

/// <summary>Tests for <see cref="ListenerInput"/>.</summary>
public class ListenerInputTests
{
	/// <summary>A new input has no text, is stamped with the current time and is not injected.</summary>
	[Fact]
	public void New_HasDocumentedDefaults()
	{
		var before = DateTimeOffset.UtcNow;

		var input = new ListenerInput();

		input.Text.Should().BeEmpty();
		input.Timestamp.Should().BeOnOrAfter(before).And.BeOnOrBefore(DateTimeOffset.UtcNow);
		input.IsInjected.Should().BeFalse();
	}

	/// <summary>All members round-trip.</summary>
	[Fact]
	public void SettableMembers_RoundTrip()
	{
		var stamp = new DateTimeOffset(2024, 5, 6, 7, 8, 9, TimeSpan.Zero);

		var input = new ListenerInput { Text = "hello", Timestamp = stamp, IsInjected = true };

		input.Text.Should().Be("hello");
		input.Timestamp.Should().Be(stamp);
		input.IsInjected.Should().BeTrue();
	}
}
