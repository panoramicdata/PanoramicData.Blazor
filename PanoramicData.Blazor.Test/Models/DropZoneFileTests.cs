using AwesomeAssertions;
using PanoramicData.Blazor.Models;

namespace PanoramicData.Blazor.Test.Models;

/// <summary>Tests for <see cref="DropZoneFile"/>, <see cref="DropZoneFileUploadOutcome"/> and <see cref="DropZoneFileUploadProgress"/>.</summary>
public class DropZoneFileTests
{
	/// <summary>A new file has no path, name or size and is not skipped.</summary>
	[Fact]
	public void New_IsEmpty()
	{
		var file = new DropZoneFile();

		file.Path.Should().BeNull();
		file.Name.Should().BeNull();
		file.Size.Should().Be(0);
		file.Skip.Should().BeFalse();
		file.NewName.Should().BeNull();
		file.Key.Should().BeEmpty();
		file.SessionId.Should().BeEmpty();
	}

	/// <summary>Without a root, the full path is the file's own path and name.</summary>
	[Theory]
	[InlineData("/docs/2024/", "a.txt", "/docs/2024/a.txt")]
	[InlineData("docs//2024", "a.txt", "/docs/2024/a.txt")]
	[InlineData(null, "a.txt", "/a.txt")]
	[InlineData("", "a.txt", "/a.txt")]
	public void GetFullPath_WithoutRoot(string? path, string name, string expected)
	{
		var file = new DropZoneFile { Path = path, Name = name };

		file.GetFullPath().Should().Be(expected);
		file.GetFullPath(null).Should().Be(expected);
	}

	/// <summary>A root directory is prefixed to the file's path, collapsing redundant separators.</summary>
	[Theory]
	[InlineData("/uploads/", "/docs", "a.txt", "/uploads/docs/a.txt")]
	[InlineData("uploads", null, "a.txt", "/uploads/a.txt")]
	[InlineData("/", "/", "a.txt", "/a.txt")]
	public void GetFullPath_WithRoot(string rootDir, string? path, string name, string expected)
	{
		var file = new DropZoneFile { Path = path, Name = name };

		file.GetFullPath(rootDir).Should().Be(expected);
	}

	/// <summary>The upload outcome carries a status on top of the file details.</summary>
	[Fact]
	public void UploadOutcome_RoundTrip()
	{
		var outcome = new DropZoneFileUploadOutcome { Name = "a.txt", Success = true, StatusCode = 201, Reason = "Created" };

		outcome.Success.Should().BeTrue();
		outcome.StatusCode.Should().Be(201);
		outcome.Reason.Should().Be("Created");
		outcome.GetFullPath().Should().Be("/a.txt");
		new DropZoneFileUploadOutcome().Reason.Should().BeEmpty();
	}

	/// <summary>The upload progress carries a percentage on top of the file details.</summary>
	[Fact]
	public void UploadProgress_RoundTrip()
	{
		var progress = new DropZoneFileUploadProgress { Name = "a.txt", Progress = 55.5, Skip = true, NewName = "b.txt", Key = "k", SessionId = "s", Size = 9 };

		progress.Progress.Should().Be(55.5);
		progress.Skip.Should().BeTrue();
		progress.NewName.Should().Be("b.txt");
		progress.Key.Should().Be("k");
		progress.SessionId.Should().Be("s");
		progress.Size.Should().Be(9);
	}
}
