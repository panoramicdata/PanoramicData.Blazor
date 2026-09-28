using AwesomeAssertions;
using PanoramicData.Blazor.Arguments;

namespace PanoramicData.Blazor.Test.Arguments;

/// <summary>Tests for <see cref="DropZoneUploadProgressEventArgs"/>.</summary>
public class DropZoneUploadProgressEventArgsTests
{
	/// <summary>The constructor captures the progress along with the base upload details.</summary>
	[Fact]
	public void Constructor_CapturesProgress()
	{
		var args = new DropZoneUploadProgressEventArgs("/f", "a.txt", 50, "k", "s", 42.5);

		args.Progress.Should().Be(42.5);
		args.FullPath.Should().Be("/f/a.txt");
		args.Cancel.Should().BeFalse();
		args.CancelReason.Should().BeEmpty();
	}

	/// <summary>A handler can update progress and cancel the upload with a reason.</summary>
	[Fact]
	public void SettableMembers_RoundTrip()
	{
		var args = new DropZoneUploadProgressEventArgs("/f", "a.txt", 50, "k", "s", 0)
		{
			Progress = 100,
			Cancel = true,
			CancelReason = "User aborted"
		};

		args.Progress.Should().Be(100);
		args.Cancel.Should().BeTrue();
		args.CancelReason.Should().Be("User aborted");
	}
}
