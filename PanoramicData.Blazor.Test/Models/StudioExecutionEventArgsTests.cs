using AwesomeAssertions;
using Microsoft.Extensions.Logging;
using PanoramicData.Blazor.Models;

namespace PanoramicData.Blazor.Test.Models;

/// <summary>Tests for <see cref="StudioExecutionEventArgs"/>.</summary>
public class StudioExecutionEventArgsTests
{
	/// <summary>A new event is an incomplete informational start event, stamped with the current time.</summary>
	[Fact]
	public void New_HasDocumentedDefaults()
	{
		var before = DateTime.Now;

		var args = new StudioExecutionEventArgs();

		args.EventType.Should().Be(StudioExecutionEventType.Started);
		args.Output.Should().BeEmpty();
		args.Status.Should().BeEmpty();
		args.Progress.Should().Be(0);
		args.LogLevel.Should().Be(LogLevel.Information);
		args.Exception.Should().BeNull();
		args.Timestamp.Should().BeOnOrAfter(before).And.BeOnOrBefore(DateTime.Now);
		args.IsComplete.Should().BeFalse();
	}

	/// <summary>All members round-trip.</summary>
	[Fact]
	public void SettableMembers_RoundTrip()
	{
		var error = new InvalidOperationException("x");
		var stamp = new DateTime(2024, 1, 1, 0, 0, 0, DateTimeKind.Local);

		var args = new StudioExecutionEventArgs
		{
			EventType = StudioExecutionEventType.Error,
			Output = "out",
			Status = "failed",
			Progress = 0.5,
			LogLevel = LogLevel.Error,
			Exception = error,
			Timestamp = stamp,
			IsComplete = true
		};

		args.EventType.Should().Be(StudioExecutionEventType.Error);
		args.Output.Should().Be("out");
		args.Status.Should().Be("failed");
		args.Progress.Should().Be(0.5);
		args.LogLevel.Should().Be(LogLevel.Error);
		args.Exception.Should().BeSameAs(error);
		args.Timestamp.Should().Be(stamp);
		args.IsComplete.Should().BeTrue();
	}
}
