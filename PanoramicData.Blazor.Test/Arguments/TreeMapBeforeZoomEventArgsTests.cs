using AwesomeAssertions;
using PanoramicData.Blazor.Arguments;

namespace PanoramicData.Blazor.Test.Arguments;

/// <summary>Tests for <see cref="TreeMapBeforeZoomEventArgs{TItem}"/>.</summary>
public class TreeMapBeforeZoomEventArgsTests
{
	/// <summary>The constructor captures the zoom origin and destination, and the zoom is not cancelled.</summary>
	[Fact]
	public void Constructor_CapturesFromAndTo()
	{
		var args = new TreeMapBeforeZoomEventArgs<string>("root", "child");

		args.From.Should().Be("root");
		args.To.Should().Be("child");
		args.Cancel.Should().BeFalse();
	}

	/// <summary>Zooming out to the root is represented by a null destination, and a handler can cancel it.</summary>
	[Fact]
	public void NullDestination_CanBeCancelled()
	{
		var args = new TreeMapBeforeZoomEventArgs<string>("child", null) { Cancel = true };

		args.To.Should().BeNull();
		args.Cancel.Should().BeTrue();
	}
}
