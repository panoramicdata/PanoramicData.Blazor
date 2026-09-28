using AwesomeAssertions;
using PanoramicData.Blazor.Models;

namespace PanoramicData.Blazor.Test.Models;

/// <summary>Tests for <see cref="ChatThought"/>.</summary>
public class ChatThoughtTests
{
	/// <summary>The record exposes its title and text.</summary>
	[Fact]
	public void Constructor_CapturesTitleAndText()
	{
		var thought = new ChatThought("Planning", "First, read the file.");

		thought.Title.Should().Be("Planning");
		thought.Text.Should().Be("First, read the file.");
	}

	/// <summary>Two thoughts with the same title and text are equal, and differ when either differs.</summary>
	[Fact]
	public void Equality_IsByValue()
	{
		var a = new ChatThought("T", "X");

		a.Should().Be(new ChatThought("T", "X"));
		a.Should().NotBe(new ChatThought("T", "Y"));
		a.Should().NotBe(a with { Title = "U" });
	}
}
