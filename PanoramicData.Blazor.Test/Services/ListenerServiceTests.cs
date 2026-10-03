using AwesomeAssertions;
using PanoramicData.Blazor.Enums;
using PanoramicData.Blazor.Models;
using PanoramicData.Blazor.Services;

namespace PanoramicData.Blazor.Test.Services;

/// <summary>Tests for <see cref="ListenerService"/>.</summary>
public sealed class ListenerServiceTests : IDisposable
{
	private readonly ListenerService _service = new();
	private readonly List<ListenerInput> _inputs = [];
	private readonly List<ListenerStatusChangedEventArgs> _statuses = [];
	private static readonly DateTimeOffset _stamp = new(2024, 1, 1, 0, 0, 0, TimeSpan.Zero);

	/// <summary>Subscribes to the service's events.</summary>
	public ListenerServiceTests()
	{
		_service.InputReceived += (_, input) => _inputs.Add(input);
		_service.StatusChanged += (_, status) => _statuses.Add(status);
	}

	/// <inheritdoc />
	public void Dispose() => _service.Dispose();

	private void Configure(ListenerMode mode, TimeSpan? timeout = null) => _service.Configure(new ListenerConfiguration
	{
		Mode = mode,
		Keyword = "Merlin",
		KeywordSilenceTimeout = timeout ?? TimeSpan.FromMinutes(5),
		KeywordTimeoutToken = "[timeout]",
		ManualStartToken = "[start]",
		ManualStopToken = "[stop]"
	});

	/// <summary>A new service is idle in manual activation mode.</summary>
	[Fact]
	public void New_IsIdleManual()
	{
		_service.Mode.Should().Be(ListenerMode.ManualActivation);
		_service.State.Should().Be(ListenerState.Idle);
	}

	/// <summary>Configuring keyword activation waits for the keyword; a null configuration restores the defaults.</summary>
	[Fact]
	public void Configure_SetsModeAndInitialState()
	{
		Configure(ListenerMode.KeywordActivation);
		_service.State.Should().Be(ListenerState.ActiveAwaitingKeyword);

		_service.Configure(null!);

		_service.Mode.Should().Be(ListenerMode.ManualActivation);
		_service.State.Should().Be(ListenerState.Idle);
		_statuses.Select(s => s.State).Should().Equal(ListenerState.ActiveAwaitingKeyword, ListenerState.Idle);
	}

	/// <summary>In manual mode, text is only passed on between start and stop, which inject their tokens.</summary>
	[Fact]
	public void ManualMode_EmitsOnlyWhileListening()
	{
		Configure(ListenerMode.ManualActivation);

		_service.HandleRecognizedText("ignored", _stamp);
		_service.StartListening();
		_service.HandleRecognizedText("  hello  ", _stamp);
		_service.StopListening();
		_service.HandleRecognizedText("ignored too", _stamp);

		_inputs.Select(i => i.Text).Should().Equal("[start]", "hello", "[stop]");
		_inputs.Select(i => i.IsInjected).Should().Equal(true, false, true);
		_inputs[1].Timestamp.Should().Be(_stamp);
		_service.State.Should().Be(ListenerState.Idle);
	}

	/// <summary>Blank recognised text is ignored.</summary>
	[Fact]
	public void HandleRecognizedText_Blank_IsIgnored()
	{
		Configure(ListenerMode.Continuous);

		_service.HandleRecognizedText("   ", _stamp);

		_inputs.Should().BeEmpty();
	}

	/// <summary>Start and stop do nothing outside manual mode.</summary>
	[Fact]
	public void StartAndStop_OutsideManualMode_DoNothing()
	{
		Configure(ListenerMode.Continuous);
		_statuses.Clear();

		_service.StartListening();
		_service.StopListening();

		_inputs.Should().BeEmpty();
		_statuses.Should().BeEmpty();
	}

	/// <summary>Manual mode without tokens configured injects nothing on start and stop.</summary>
	[Fact]
	public void ManualMode_WithoutTokens_InjectsNothing()
	{
		_service.Configure(new ListenerConfiguration());

		_service.StartListening();
		_service.StopListening();

		_inputs.Should().BeEmpty();
	}

	/// <summary>Continuous mode passes every utterance on and reports that it is listening.</summary>
	[Fact]
	public void ContinuousMode_EmitsEverything()
	{
		Configure(ListenerMode.Continuous);

		_service.HandleRecognizedText("one", _stamp);
		_service.HandleRecognizedText("two", _stamp);

		_inputs.Select(i => i.Text).Should().Equal("one", "two");
		_service.State.Should().Be(ListenerState.Listening);
	}

	/// <summary>In keyword mode, only the keyword starts listening, after which text is passed on.</summary>
	[Fact]
	public void KeywordMode_KeywordStartsListening()
	{
		Configure(ListenerMode.KeywordActivation);

		_service.HandleRecognizedText("hello", _stamp);
		_service.State.Should().Be(ListenerState.ActiveAwaitingKeyword);

		_service.HandleRecognizedText("merlin", _stamp);
		_service.State.Should().Be(ListenerState.Listening);

		_service.HandleRecognizedText("what time is it", _stamp);

		_inputs.Select(i => i.Text).Should().Equal("what time is it");
	}

	/// <summary>After the silence timeout, keyword mode returns to awaiting the keyword and injects the timeout token.</summary>
	[Fact]
	public async Task KeywordMode_SilenceTimeout_ReturnsToAwaitingKeyword()
	{
		Configure(ListenerMode.KeywordActivation, TimeSpan.FromMilliseconds(20));
		var timedOut = new TaskCompletionSource();
		_service.InputReceived += (_, input) =>
		{
			if (input.IsInjected)
			{
				timedOut.TrySetResult();
			}
		};

		_service.HandleRecognizedText("Merlin", _stamp);
		await timedOut.Task.WaitAsync(TimeSpan.FromSeconds(10), TestContext.Current.CancellationToken);

		_service.State.Should().Be(ListenerState.ActiveAwaitingKeyword);
		_inputs.Should().ContainSingle().Which.Text.Should().Be("[timeout]");
	}

	/// <summary>
	/// A silence timer that fires after the listener has already gone back to awaiting the keyword (or left
	/// keyword mode) changes nothing and injects nothing.
	/// </summary>
	[Theory]
	[InlineData(ListenerMode.KeywordActivation)]
	[InlineData(ListenerMode.Continuous)]
	public void KeywordTimeout_WhenNotListeningForKeywordFollowUp_DoesNothing(ListenerMode mode)
	{
		Configure(mode);
		var stateBefore = _service.State;
		_statuses.Clear();

		_service.OnKeywordTimeout();

		_service.State.Should().Be(stateBefore);
		_statuses.Should().BeEmpty();
		_inputs.Should().BeEmpty();
	}

	/// <summary>A zero silence timeout keeps keyword mode listening indefinitely.</summary>
	[Fact]
	public void KeywordMode_ZeroTimeout_KeepsListening()
	{
		Configure(ListenerMode.KeywordActivation, TimeSpan.Zero);

		_service.HandleRecognizedText("Merlin", _stamp);
		_service.HandleRecognizedText("still here", _stamp);

		_service.State.Should().Be(ListenerState.Listening);
		_inputs.Should().ContainSingle().Which.Text.Should().Be("still here");
	}

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
