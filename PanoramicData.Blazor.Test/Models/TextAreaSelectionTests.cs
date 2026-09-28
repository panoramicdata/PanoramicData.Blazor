using AwesomeAssertions;
using PanoramicData.Blazor.Models;

namespace PanoramicData.Blazor.Test.Models;

/// <summary>Tests for <see cref="TextAreaSelection"/>.</summary>
public class TextAreaSelectionTests
{
	/// <summary>A new selection is empty at the start, and its members round-trip.</summary>
	[Fact]
	public void Members_DefaultAndRoundTrip()
	{
		var selection = new TextAreaSelection();
		selection.Start.Should().Be(0);
		selection.End.Should().Be(0);
		selection.Value.Should().BeEmpty();

		selection.Start = 2;
		selection.End = 6;
		selection.Value = "text";

		selection.Start.Should().Be(2);
		selection.End.Should().Be(6);
		selection.Value.Should().Be("text");
	}
}
