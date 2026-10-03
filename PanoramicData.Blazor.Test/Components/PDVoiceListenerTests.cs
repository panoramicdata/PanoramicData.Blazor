using AwesomeAssertions;
using Bunit;
using Microsoft.Extensions.DependencyInjection;
using PanoramicData.Blazor.Enums;
using PanoramicData.Blazor.Interfaces;

namespace PanoramicData.Blazor.Test;

/// <summary>
/// Tests that <see cref="PDVoiceListener"/> configures its listener service from its parameters, drives
/// the browser speech module (initialise, start, stop, dispose), and forwards every speech callback from
/// JavaScript to the listener service.
/// </summary>
public partial class PDVoiceListenerTests : BunitContext
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

	private static object? ReadProperty(object? source, string name)
		=> source!.GetType().GetProperty(name)!.GetValue(source);
}
