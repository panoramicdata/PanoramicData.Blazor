using AwesomeAssertions;
using PanoramicData.Blazor.Arguments;

namespace PanoramicData.Blazor.Test.Arguments;

/// <summary>Tests for <see cref="DropZoneUploadEventArgs"/>.</summary>
public class DropZoneUploadEventArgsTests
{
	/// <summary>The constructor captures every upload detail, and batch values start at zero.</summary>
	[Fact]
	public void Constructor_CapturesUploadDetails()
	{
		var args = new DropZoneUploadEventArgs("/folder", "file.txt", 123, "key-1", "session-1");

		args.Path.Should().Be("/folder");
		args.Name.Should().Be("file.txt");
		args.Size.Should().Be(123);
		args.Key.Should().Be("key-1");
		args.SessionId.Should().Be("session-1");
		args.FormFields.Should().BeEmpty();
		args.BatchCount.Should().Be(0);
		args.BatchProgress.Should().Be(0);
	}

	/// <summary>The full path joins path and name with exactly one separator.</summary>
	[Theory]
	[InlineData("/folder", "file.txt", "/folder/file.txt")]
	[InlineData("/folder/", "file.txt", "/folder/file.txt")]
	[InlineData("/folder", "/file.txt", "/folder/file.txt")]
	[InlineData("/folder/", "/file.txt", "/folder/file.txt")]
	[InlineData("", "file.txt", "/file.txt")]
	[InlineData("/", "file.txt", "/file.txt")]
	public void FullPath_JoinsWithSingleSeparator(string path, string name, string expected)
	{
		var args = new DropZoneUploadEventArgs(path, name, 0, string.Empty, string.Empty);

		args.FullPath.Should().Be(expected);
	}

	/// <summary>All members are settable, and the full path follows changes to path and name.</summary>
	[Fact]
	public void SettableMembers_RoundTrip()
	{
		var args = new DropZoneUploadEventArgs("/a", "b", 1, "k", "s")
		{
			Path = "/x",
			Name = "y.txt",
			Size = 99,
			Key = "k2",
			SessionId = "s2",
			FormFields = new Dictionary<string, string> { ["field"] = "value" },
			BatchCount = 4,
			BatchProgress = 2
		};

		args.FullPath.Should().Be("/x/y.txt");
		args.Size.Should().Be(99);
		args.Key.Should().Be("k2");
		args.SessionId.Should().Be("s2");
		args.FormFields.Should().ContainKey("field");
		args.BatchCount.Should().Be(4);
		args.BatchProgress.Should().Be(2);
	}
}
