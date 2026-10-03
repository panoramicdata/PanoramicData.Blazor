using AwesomeAssertions;
using Bunit;
using PanoramicData.Blazor.Enums;
using PanoramicData.Blazor.Interfaces;
using PanoramicData.Blazor.Models;

namespace PanoramicData.Blazor.Test;

/// <summary>
/// Tests for how <see cref="PDVoiceListener"/> drives its listener service, and the service double they use.
/// </summary>
public partial class PDVoiceListenerTests
{
	/// <summary>
	/// Verifies that StartAsync and StopAsync drive both the service and the module, and that repeated
	/// calls do not start or stop the module twice.
	/// </summary>
	[Fact]
	public async Task StartAsync_and_StopAsync_drive_the_service_and_the_module_once()
	{
		var component = Render<PDVoiceListener>();

		await component.InvokeAsync(component.Instance.StartAsync);
		await component.InvokeAsync(component.Instance.StartAsync);
		_defaultService.Calls.Should().Equal("StartListening", "StartListening");
		_module.Invocations["startListening"].Should().ContainSingle();

		await component.InvokeAsync(component.Instance.StopAsync);
		await component.InvokeAsync(component.Instance.StopAsync);
		_defaultService.Calls.Should().Equal("StartListening", "StartListening", "StopListening", "StopListening");
		_module.Invocations["stopListening"].Should().ContainSingle();
	}

	/// <summary>
	/// Verifies that recognised text is forwarded with its parsed timestamp.
	/// </summary>
	[Fact]
	public void Recognised_text_is_forwarded_with_its_timestamp()
	{
		var component = Render<PDVoiceListener>();

		component.Instance.OnRecognizedText("hello", "2026-09-28T10:15:00+00:00");

		_defaultService.Recognised.Should().ContainSingle()
			.Which.Should().Be(("hello", new DateTimeOffset(2026, 9, 28, 10, 15, 0, TimeSpan.Zero)));
	}

	/// <summary>
	/// Verifies that a missing or unparsable timestamp falls back to the current time.
	/// </summary>
	[Theory]
	[InlineData("")]
	[InlineData("not a date")]
	public void An_unusable_timestamp_falls_back_to_now(string timestamp)
	{
		var component = Render<PDVoiceListener>();
		var before = DateTimeOffset.UtcNow;

		component.Instance.OnRecognizedText("hello", timestamp);

		var (_, received) = _defaultService.Recognised.Should().ContainSingle().Subject;
		received.Should().BeOnOrAfter(before).And.BeOnOrBefore(DateTimeOffset.UtcNow);
	}

	/// <summary>
	/// Verifies that each state callback from JavaScript reaches the matching service method.
	/// </summary>
	[Fact]
	public void State_callbacks_are_forwarded_to_the_service()
	{
		var component = Render<PDVoiceListener>();

		component.Instance.OnListeningStarted();
		component.Instance.OnListeningStopped();
		component.Instance.OnUnsupported();
		component.Instance.OnPermissionDenied();
		component.Instance.OnListenerError("network", "Network down");

		_defaultService.Calls.Should().Equal(
			"HandleListeningStarted",
			"HandleListeningStopped",
			"HandleUnsupported",
			"HandlePermissionDenied",
			"HandleError:network:Network down");
	}

	/// <summary>
	/// Verifies that disposing tells the module to release the recogniser.
	/// </summary>
	[Fact]
	public async Task Disposing_disposes_the_module_recogniser()
	{
		var component = Render<PDVoiceListener>();

		await component.Instance.DisposeAsync();

		_module.VerifyInvoke("dispose");
	}

	/// <summary>
	/// Verifies that the component leaves no handler attached to the listener service's events once it has
	/// been disposed, so a long-lived service cannot keep a disposed component alive.
	/// </summary>
	[Fact]
	public async Task Disposing_leaves_no_handlers_on_the_service()
	{
		var component = Render<PDVoiceListener>();

		await component.Instance.DisposeAsync();

		_defaultService.InputReceivedSubscribers.Should().Be(0);
		_defaultService.StatusChangedSubscribers.Should().Be(0);
	}

	/// <summary>
	/// A listener service that records what the component asks of it.
	/// </summary>
	private sealed class RecordingListenerService : IListenerService
	{
		/// <summary>Gets the configurations applied, in order.</summary>
		public List<ListenerConfiguration> Configurations { get; } = [];

		/// <summary>Gets the names of the state methods called, in order.</summary>
		public List<string> Calls { get; } = [];

		/// <summary>Gets the recognised text received, in order.</summary>
		public List<(string Text, DateTimeOffset Timestamp)> Recognised { get; } = [];

		/// <summary>Gets the number of handlers currently attached to <see cref="InputReceived"/>.</summary>
		public int InputReceivedSubscribers => _inputReceived?.GetInvocationList().Length ?? 0;

		/// <summary>Gets the number of handlers currently attached to <see cref="StatusChanged"/>.</summary>
		public int StatusChangedSubscribers => _statusChanged?.GetInvocationList().Length ?? 0;

		private EventHandler<ListenerInput>? _inputReceived;
		private EventHandler<ListenerStatusChangedEventArgs>? _statusChanged;

		/// <inheritdoc />
		public event EventHandler<ListenerInput>? InputReceived
		{
			add => _inputReceived += value;
			remove => _inputReceived -= value;
		}

		/// <inheritdoc />
		public event EventHandler<ListenerStatusChangedEventArgs>? StatusChanged
		{
			add => _statusChanged += value;
			remove => _statusChanged -= value;
		}

		/// <inheritdoc />
		public ListenerMode Mode => Configurations.Count == 0 ? ListenerMode.ManualActivation : Configurations[^1].Mode;

		/// <inheritdoc />
		public ListenerState State => default;

		/// <inheritdoc />
		public void Configure(ListenerConfiguration configuration) => Configurations.Add(configuration);

		/// <inheritdoc />
		public void StartListening() => Calls.Add(nameof(StartListening));

		/// <inheritdoc />
		public void StopListening() => Calls.Add(nameof(StopListening));

		/// <inheritdoc />
		public void HandleRecognizedText(string text, DateTimeOffset timestamp) => Recognised.Add((text, timestamp));

		/// <inheritdoc />
		public void HandleListeningStarted() => Calls.Add(nameof(HandleListeningStarted));

		/// <inheritdoc />
		public void HandleListeningStopped() => Calls.Add(nameof(HandleListeningStopped));

		/// <inheritdoc />
		public void HandleUnsupported() => Calls.Add(nameof(HandleUnsupported));

		/// <inheritdoc />
		public void HandlePermissionDenied() => Calls.Add(nameof(HandlePermissionDenied));

		/// <inheritdoc />
		public void HandleError(string? errorCode, string? message) => Calls.Add($"{nameof(HandleError)}:{errorCode}:{message}");
	}
}
