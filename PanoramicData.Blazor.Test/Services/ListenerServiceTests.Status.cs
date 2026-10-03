using AwesomeAssertions;
using PanoramicData.Blazor.Enums;
using PanoramicData.Blazor.Services;

namespace PanoramicData.Blazor.Test.Services;

/// <summary>
/// Listening state and status change tests for <see cref="ListenerService"/>.
/// </summary>
public partial class ListenerServiceTests
{
	/// <summary>When the source starts listening, the state depends on the mode and on manual activation.</summary>
	[Theory]
	[InlineData(ListenerMode.KeywordActivation, false, ListenerState.ActiveAwaitingKeyword)]
	[InlineData(ListenerMode.ManualActivation, false, ListenerState.Idle)]
	[InlineData(ListenerMode.ManualActivation, true, ListenerState.Listening)]
	[InlineData(ListenerMode.Continuous, false, ListenerState.Listening)]
	public void HandleListeningStarted_SetsStateForMode(ListenerMode mode, bool started, ListenerState expected)
	{
		Configure(mode);
		if (started)
		{
			_service.StartListening();
		}

		_service.HandleListeningStarted();

		_service.State.Should().Be(expected);
	}

	/// <summary>When the source stops listening, the service is idle.</summary>
	[Fact]
	public void HandleListeningStopped_IsIdle()
	{
		Configure(ListenerMode.Continuous);
		_service.HandleListeningStarted();

		_service.HandleListeningStopped();

		_service.State.Should().Be(ListenerState.Idle);
	}

	/// <summary>Failure states are reported with their error code and message.</summary>
	[Fact]
	public void FailureStates_AreReportedWithDetails()
	{
		_service.HandleUnsupported();
		_statuses[^1].Should().BeEquivalentTo(new { State = ListenerState.Unsupported, ErrorCode = "unsupported" });

		_service.HandlePermissionDenied();
		_statuses[^1].Should().BeEquivalentTo(new { State = ListenerState.PermissionDenied, ErrorCode = "not-allowed", Message = "Microphone permission denied." });

		_service.HandleError("network", "Offline");
		_statuses[^1].Should().BeEquivalentTo(new { State = ListenerState.Error, ErrorCode = "network", Message = "Offline" });
		_service.State.Should().Be(ListenerState.Error);
	}

	/// <summary>A repeated state is announced once, but the same state with a different error is announced again.</summary>
	[Fact]
	public void StatusChanged_OnlyRaisedOnChange()
	{
		_service.HandleError("a", "first");
		_service.HandleError("a", "first");
		_service.HandleError("a", "second");

		_statuses.Should().HaveCount(2);
	}
}
