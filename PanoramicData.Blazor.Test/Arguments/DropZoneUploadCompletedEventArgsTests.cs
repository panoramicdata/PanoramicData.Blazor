using AwesomeAssertions;
using PanoramicData.Blazor.Arguments;

namespace PanoramicData.Blazor.Test.Arguments;

/// <summary>Tests for <see cref="DropZoneUploadCompletedEventArgs"/>.</summary>
public class DropZoneUploadCompletedEventArgsTests
{
	/// <summary>The constructor without a reason reports a successful upload.</summary>
	[Fact]
	public void ConstructorWithoutReason_IsSuccess()
	{
		var args = new DropZoneUploadCompletedEventArgs("/f", "a.txt", 10, "k", "s");

		args.Success.Should().BeTrue();
		args.Reason.Should().BeEmpty();
		args.FullPath.Should().Be("/f/a.txt");
		args.Size.Should().Be(10);
	}

	/// <summary>The constructor with a reason reports a failed upload and carries the reason.</summary>
	[Fact]
	public void ConstructorWithReason_IsFailure()
	{
		var args = new DropZoneUploadCompletedEventArgs("/f", "a.txt", 10, "k", "s", "Too large");

		args.Success.Should().BeFalse();
		args.Reason.Should().Be("Too large");
		args.Key.Should().Be("k");
		args.SessionId.Should().Be("s");
	}

	/// <summary>Success and reason can be changed after construction.</summary>
	[Fact]
	public void SuccessAndReason_AreSettable()
	{
		var args = new DropZoneUploadCompletedEventArgs("/f", "a.txt", 10, "k", "s")
		{
			Success = false,
			Reason = "Cancelled"
		};

		args.Success.Should().BeFalse();
		args.Reason.Should().Be("Cancelled");
	}
}
