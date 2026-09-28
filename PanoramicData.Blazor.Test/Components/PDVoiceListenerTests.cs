using AwesomeAssertions;
using Bunit;
using Microsoft.Extensions.DependencyInjection;
using PanoramicData.Blazor.Enums;
using PanoramicData.Blazor.Interfaces;
using PanoramicData.Blazor.Models;

namespace PanoramicData.Blazor.Test;

/// <summary>
/// Tests that <see cref="PDVoiceListener"/> configures its listener service from its parameters, drives
/// the browser speech module (initialise, start, stop, dispose), and forwards every speech callback from
/// JavaScript to the listener service.
/// </summary>
public class PDVoiceListenerTests : BunitContext
{
	private const string ModulePath = "./_content/PanoramicData.Blazor/PDVoiceListener.razor.js";

	private readonly BunitJSModuleInterop _module;
	private readonly RecordingListenerService _defaultService = new();

	/// <summary>Sets up the rendering context with a recording default listener service.</summary>
	public PDVoiceListenerTests()
	{
		JSInterop.Mode = JSRuntimeMode.Loose;
		_module = JSInterop.SetupModule(ModulePath);
		Services.AddSingleton<IListenerService>(_defaultService);
	}

	/// <summary>
	/// Verifies that the child content is rendered.
	/// </summary>
	[Fact]
	public void Renders_its_child_content()
	{
		var component = Render<PDVoiceListener>(parameters => parameters
			.AddChildContent("<p class=\"inside\">Speak</p>"));

		component.Find("p.inside").TextContent.Should().Be("Speak");
	}

	/// <summary>
	/// Verifies that the injected service is configured with every listener parameter.
	/// </summary>
	[Fact]
	public void Configures_the_default_service_from_its_parameters()
	{
		Render<PDVoiceListener>(parameters => parameters
			.Add(p => p.Mode, ListenerMode.KeywordActivation)
			.Add(p => p.Keyword, "merlin")
			.Add(p => p.KeywordSilenceTimeout, TimeSpan.FromSeconds(7))
			.Add(p => p.KeywordTimeoutToken, "[timeout]")
			.Add(p => p.ManualStartToken, "[start]")
			.Add(p => p.ManualStopToken, "[stop]"));

		var configuration = _defaultService.Configurations.Should().ContainSingle().Subject;
		configuration.Mode.Should().Be(ListenerMode.KeywordActivation);
		configuration.Keyword.Should().Be("merlin");
		configuration.KeywordSilenceTimeout.Should().Be(TimeSpan.FromSeconds(7));
		configuration.KeywordTimeoutToken.Should().Be("[timeout]");
		configuration.ManualStartToken.Should().Be("[start]");
		configuration.ManualStopToken.Should().Be("[stop]");
	}

	/// <summary>
	/// Verifies that an explicit listener service is used in place of the injected one.
	/// </summary>
	[Fact]
	public void An_explicit_service_is_used_instead_of_the_injected_one()
	{
		var explicitService = new RecordingListenerService();

		Render<PDVoiceListener>(parameters => parameters
			.Add(p => p.ListenerService, explicitService));

		explicitService.Configurations.Should().ContainSingle();
		_defaultService.Configurations.Should().BeEmpty();
	}

	/// <summary>
	/// Verifies that re-rendering with identical configuration does not reconfigure the service, while a
	/// changed value does.
	/// </summary>
	[Fact]
	public void The_service_is_reconfigured_only_when_the_configuration_changes()
	{
		var component = Render<PDVoiceListener>(parameters => parameters.Add(p => p.Keyword, "one"));

		component.Render(parameters => parameters.Add(p => p.Keyword, "one"));
		_defaultService.Configurations.Should().ContainSingle();

		component.Render(parameters => parameters.Add(p => p.Keyword, "two"));
		_defaultService.Configurations.Should().HaveCount(2);
		_defaultService.Configurations[^1].Keyword.Should().Be("two");
	}

	/// <summary>
	/// Verifies that the module is initialised with the mode and background setting, and that manual mode
	/// does not start listening on its own.
	/// </summary>
	[Fact]
	public void First_render_initialises_the_module_without_starting_in_manual_mode()
	{
		Render<PDVoiceListener>(parameters => parameters
			.Add(p => p.RunInBackground, false));

		var initialize = _module.VerifyInvoke("initialize");
		initialize.Arguments.Should().HaveCount(2);
		ReadProperty(initialize.Arguments[1], "mode").Should().Be("ManualActivation");
		ReadProperty(initialize.Arguments[1], "runInBackground").Should().Be(false);
		_module.Invocations["startListening"].Should().BeEmpty();
	}

	/// <summary>
	/// Verifies that an auto-starting continuous listener starts listening once the module is loaded.
	/// </summary>
	[Fact]
	public void An_auto_starting_continuous_listener_starts_listening()
	{
		Render<PDVoiceListener>(parameters => parameters
			.Add(p => p.Mode, ListenerMode.Continuous));

		_module.VerifyInvoke("startListening");
	}

	/// <summary>
	/// Verifies that a continuous listener with AutoStart off does not start listening.
	/// </summary>
	[Fact]
	public void A_listener_without_auto_start_does_not_start()
	{
		Render<PDVoiceListener>(parameters => parameters
			.Add(p => p.Mode, ListenerMode.Continuous)
			.Add(p => p.AutoStart, false));

		_module.Invocations["startListening"].Should().BeEmpty();
	}

	/// <summary>
	/// Verifies that changing mode after first render reconfigures the module and starts or stops it.
	/// </summary>
	[Fact]
	public void Changing_mode_after_first_render_reconfigures_and_starts_or_stops()
	{
		var component = Render<PDVoiceListener>();

		component.Render(parameters => parameters.Add(p => p.Mode, ListenerMode.Continuous));
		var configure = _module.Invocations["configure"].Should().ContainSingle().Subject;
		ReadProperty(configure.Arguments[0], "mode").Should().Be("Continuous");
		_module.Invocations["startListening"].Should().ContainSingle();

		component.Render(parameters => parameters.Add(p => p.Mode, ListenerMode.ManualActivation));
		_module.Invocations["stopListening"].Should().ContainSingle();
	}

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

	private static object? ReadProperty(object? source, string name)
		=> source!.GetType().GetProperty(name)!.GetValue(source);

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

		/// <inheritdoc />
		public event EventHandler<ListenerInput>? InputReceived
		{
			add { }
			remove { }
		}

		/// <inheritdoc />
		public event EventHandler<ListenerStatusChangedEventArgs>? StatusChanged
		{
			add { }
			remove { }
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
