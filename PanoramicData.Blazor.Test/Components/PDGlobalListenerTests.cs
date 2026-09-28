using AwesomeAssertions;
using Bunit;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.JSInterop;
using PanoramicData.Blazor.Extensions;
using PanoramicData.Blazor.Interfaces;
using PanoramicData.Blazor.Models;

namespace PanoramicData.Blazor.Test.Components;

/// <summary>
/// Tests that <see cref="PDGlobalListener"/> wires its JavaScript module to the <see cref="IGlobalEventService"/>:
/// it initialises the module, keeps the module's shortcut list in step with the service, forwards key events
/// from JavaScript to the service, and tears the module down on disposal.
/// </summary>
public class PDGlobalListenerTests : BunitContext
{
	private const string ModulePath = "./_content/PanoramicData.Blazor/PDGlobalListener.razor.js";

	/// <summary>Sets up the rendering context.</summary>
	public PDGlobalListenerTests()
	{
		JSInterop.Mode = JSRuntimeMode.Loose;
		Services.AddPanoramicDataBlazor();
	}

	private IGlobalEventService EventService => Services.GetRequiredService<IGlobalEventService>();

	/// <summary>On first render the module is initialised with a reference back to the component.</summary>
	[Fact]
	public void FirstRender_InitialisesModuleWithDotNetReference()
	{
		var module = JSInterop.SetupModule(ModulePath);

		var cut = Render<PDGlobalListener>();

		var initialize = module.VerifyInvoke("initialize");
		initialize.Arguments.Should().ContainSingle()
			.Which.Should().BeOfType<DotNetObjectReference<PDGlobalListener>>()
			.Which.Value.Should().BeSameAs(cut.Instance);
	}

	/// <summary>With no shortcuts registered, nothing is pushed to the module on first render.</summary>
	[Fact]
	public void FirstRender_WithNoShortcuts_DoesNotPushShortcuts()
	{
		var module = JSInterop.SetupModule(ModulePath);

		Render<PDGlobalListener>();

		module.Invocations["registerShortcutKeys"].Should().BeEmpty();
	}

	/// <summary>Shortcuts registered before the listener renders are pushed to the module during initialisation.</summary>
	[Fact]
	public void FirstRender_WithExistingShortcuts_PushesThem()
	{
		var module = JSInterop.SetupModule(ModulePath);
		EventService.RegisterShortcutKey(new ShortcutKey { Key = "s", CtrlKey = true });

		Render<PDGlobalListener>();

		var push = module.VerifyInvoke("registerShortcutKeys");
		push.Arguments[0].Should().BeAssignableTo<IEnumerable<ShortcutKey>>()
			.Which.Should().ContainSingle().Which.Key.Should().Be("s");
	}

	/// <summary>A shortcut registered after render is pushed to the module with the full current list.</summary>
	[Fact]
	public void ShortcutRegisteredAfterRender_IsPushedToModule()
	{
		var module = JSInterop.SetupModule(ModulePath);
		EventService.RegisterShortcutKey(new ShortcutKey { Key = "a", CtrlKey = true });
		Render<PDGlobalListener>();

		EventService.RegisterShortcutKey(new ShortcutKey { Key = "b", AltKey = true });

		var pushes = module.Invocations["registerShortcutKeys"];
		pushes.Should().HaveCount(2);
		pushes[1].Arguments[0].Should().BeAssignableTo<IEnumerable<ShortcutKey>>()
			.Which.Select(k => k.Key).Should().BeEquivalentTo("a", "b");
	}

	/// <summary>Key-down and key-up callbacks from JavaScript are forwarded to the service's events.</summary>
	[Fact]
	public void KeyCallbacks_AreForwardedToService()
	{
		JSInterop.SetupModule(ModulePath);
		var cut = Render<PDGlobalListener>();
		KeyboardInfo? down = null;
		KeyboardInfo? up = null;
		EventService.KeyDownEvent += (_, info) => down = info;
		EventService.KeyUpEvent += (_, info) => up = info;
		var pressed = new KeyboardInfo { Key = "x", Code = "KeyX" };
		var released = new KeyboardInfo { Key = "y", Code = "KeyY" };

		cut.Instance.OnKeyDown(pressed);
		cut.Instance.OnKeyUp(released);

		down.Should().BeSameAs(pressed);
		up.Should().BeSameAs(released);
	}

	/// <summary>Disposal tells the module to dispose and stops later shortcut changes reaching it.</summary>
	[Fact]
	public async Task DisposeAsync_DisposesModule_AndUnsubscribesFromShortcutChanges()
	{
		var module = JSInterop.SetupModule(ModulePath);
		var cut = Render<PDGlobalListener>();

		await cut.Instance.DisposeAsync();
		EventService.RegisterShortcutKey(new ShortcutKey { Key = "z", CtrlKey = true });

		module.VerifyInvoke("dispose");
		module.Invocations["registerShortcutKeys"].Should().BeEmpty();
	}

	/// <summary>
	/// When the module cannot be imported the failure is swallowed, nothing is initialised, and later
	/// shortcut changes and disposal are harmless.
	/// </summary>
	[Fact]
	public async Task ImportFailure_IsSwallowed_AndLeavesNoModule()
	{
		JSInterop.Mode = JSRuntimeMode.Strict;

		var cut = Render<PDGlobalListener>();
		EventService.RegisterShortcutKey(new ShortcutKey { Key = "q", CtrlKey = true });
		await cut.Instance.DisposeAsync();

		JSInterop.Invocations.Should().ContainSingle().Which.Identifier.Should().Be("import");
	}

	/// <summary>A JavaScript failure while pushing changed shortcuts is swallowed rather than escaping the event handler.</summary>
	[Fact]
	public void ShortcutPushFailure_IsSwallowed()
	{
		var module = JSInterop.SetupModule(ModulePath);
		module.SetupVoid("registerShortcutKeys", _ => true).SetException(new JSException("gone"));
		Render<PDGlobalListener>();

		var register = () => EventService.RegisterShortcutKey(new ShortcutKey { Key = "k", CtrlKey = true });

		register.Should().NotThrow();
		module.Invocations["registerShortcutKeys"].Should().ContainSingle();
	}

	/// <summary>A JavaScript failure while disposing the module is swallowed, and the service subscription is still removed.</summary>
	[Fact]
	public async Task DisposeFailure_IsSwallowed_AndStillUnsubscribes()
	{
		var module = JSInterop.SetupModule(ModulePath);
		module.SetupVoid("dispose").SetException(new JSException("gone"));
		var cut = Render<PDGlobalListener>();

		var dispose = async () => await cut.Instance.DisposeAsync();

		await dispose.Should().NotThrowAsync();
		EventService.RegisterShortcutKey(new ShortcutKey { Key = "m", CtrlKey = true });
		module.Invocations["registerShortcutKeys"].Should().BeEmpty();
	}
}