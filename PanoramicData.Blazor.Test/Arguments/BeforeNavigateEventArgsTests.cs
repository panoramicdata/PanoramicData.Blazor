using AwesomeAssertions;
using PanoramicData.Blazor.Arguments;

namespace PanoramicData.Blazor.Test.Arguments;

/// <summary>Tests for <see cref="BeforeNavigateEventArgs"/>.</summary>
public class BeforeNavigateEventArgsTests
{
	/// <summary>A new instance targets nowhere and is not cancelled.</summary>
	[Fact]
	public void New_HasEmptyTargetAndIsNotCancelled()
	{
		var args = new BeforeNavigateEventArgs();

		args.Target.Should().BeEmpty();
		args.Cancel.Should().BeFalse();
	}

	/// <summary>The target and cancel flag round-trip, and the type is a cancellable event argument.</summary>
	[Fact]
	public void TargetAndCancel_RoundTrip()
	{
		var args = new BeforeNavigateEventArgs { Target = "/home", Cancel = true };

		args.Target.Should().Be("/home");
		args.Cancel.Should().BeTrue();
		args.Should().BeAssignableTo<CancelEventArgs>();
	}
}
