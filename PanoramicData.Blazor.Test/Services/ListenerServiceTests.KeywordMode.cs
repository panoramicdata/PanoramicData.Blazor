using AwesomeAssertions;
using PanoramicData.Blazor.Enums;
using PanoramicData.Blazor.Services;

namespace PanoramicData.Blazor.Test.Services;

/// <summary>
/// Keyword mode tests for <see cref="ListenerService"/>.
/// </summary>
public partial class ListenerServiceTests
{
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
}
