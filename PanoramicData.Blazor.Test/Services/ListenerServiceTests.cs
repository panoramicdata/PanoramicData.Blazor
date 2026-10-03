using AwesomeAssertions;
using PanoramicData.Blazor.Enums;
using PanoramicData.Blazor.Models;
using PanoramicData.Blazor.Services;

namespace PanoramicData.Blazor.Test.Services;

/// <summary>Tests for <see cref="ListenerService"/>.</summary>
public sealed partial class ListenerServiceTests : IDisposable
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
}
