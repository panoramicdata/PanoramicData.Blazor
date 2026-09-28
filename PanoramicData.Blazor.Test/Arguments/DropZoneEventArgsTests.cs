using AwesomeAssertions;
using PanoramicData.Blazor.Arguments;
using PanoramicData.Blazor.Models;

namespace PanoramicData.Blazor.Test.Arguments;

/// <summary>Tests for <see cref="DropZoneEventArgs"/>.</summary>
public class DropZoneEventArgsTests
{
	/// <summary>The constructor captures sender and files, and the remaining members have neutral defaults.</summary>
	[Fact]
	public void Constructor_CapturesSenderAndFiles()
	{
		var sender = new object();
		DropZoneFile[] files = [new DropZoneFile { Name = "a.txt" }];

		var args = new DropZoneEventArgs(sender, files);

		args.Sender.Should().BeSameAs(sender);
		args.Files.Should().BeSameAs(files);
		args.Cancel.Should().BeFalse();
		args.CancelReason.Should().BeEmpty();
		args.BaseFolder.Should().BeEmpty();
		args.State.Should().BeNull();
	}

	/// <summary>A handler can cancel the drop, give a reason, redirect it and attach state.</summary>
	[Fact]
	public void SettableMembers_RoundTrip()
	{
		var state = new object();

		var args = new DropZoneEventArgs(this, [])
		{
			Cancel = true,
			CancelReason = "No space",
			BaseFolder = "/uploads",
			State = state
		};

		args.Cancel.Should().BeTrue();
		args.CancelReason.Should().Be("No space");
		args.BaseFolder.Should().Be("/uploads");
		args.State.Should().BeSameAs(state);
	}
}
