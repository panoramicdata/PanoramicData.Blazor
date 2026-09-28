using AwesomeAssertions;
using Bunit;
using Microsoft.JSInterop;

namespace PanoramicData.Blazor.Test.Components;

/// <summary>
/// Tests that <see cref="JSModuleComponentBase"/> imports the module named by its subclass once, calls the
/// load and per-render hooks in the right order, tolerates JavaScript failures, and releases the module on
/// disposal.
/// </summary>
public class JSModuleComponentBaseTests : BunitContext
{
	private const string ModulePath = "./test-module.js";

	/// <summary>Sets up the rendering context.</summary>
	public JSModuleComponentBaseTests() => JSInterop.Mode = JSRuntimeMode.Loose;

	/// <summary>First render imports the subclass's module path once and hands the module to the load hook.</summary>
	[Fact]
	public void FirstRender_ImportsModuleAndCallsLoadHook()
	{
		JSInterop.SetupModule(ModulePath);

		var cut = Render<ProbeComponent>();

		JSInterop.VerifyInvoke("import").Arguments.Should().Equal(ModulePath);
		cut.Instance.ModuleValue.Should().NotBeNull();
		cut.Instance.Calls.Should().Equal("loaded:True", "after:True");
	}

	/// <summary>Later renders call only the per-render hook, and do not import the module again.</summary>
	[Fact]
	public void SubsequentRenders_CallOnlyThePerRenderHook()
	{
		JSInterop.SetupModule(ModulePath);
		var cut = Render<ProbeComponent>();

		cut.Render();
		cut.Render();

		JSInterop.Invocations["import"].Should().ContainSingle();
		cut.Instance.Calls.Should().Equal("loaded:True", "after:True", "after:False", "after:False");
	}

	/// <summary>An exception from the per-render hook is swallowed and later renders still reach the hook.</summary>
	[Fact]
	public void PerRenderHookFailure_IsSwallowed()
	{
		JSInterop.SetupModule(ModulePath);
		var cut = Render<ProbeComponent>(p => p.Add(x => x.ThrowAfterRender, true));

		cut.Render();

		cut.Instance.Calls.Should().Equal("loaded:True", "after:True", "after:False");
	}

	/// <summary>When the import fails, the module stays null and neither hook is called on any render.</summary>
	[Fact]
	public void ImportFailure_LeavesNoModule_AndSkipsHooks()
	{
		// Strict mode with no module set up makes the import throw.
		JSInterop.Mode = JSRuntimeMode.Strict;

		var cut = Render<ProbeComponent>();
		cut.Render();

		cut.Instance.ModuleValue.Should().BeNull();
		cut.Instance.Calls.Should().BeEmpty();
	}

	/// <summary>An exception from the load hook is swallowed, but the module is kept and the per-render hook still runs.</summary>
	[Fact]
	public void LoadHookFailure_IsSwallowed_AndModuleIsKept()
	{
		JSInterop.SetupModule(ModulePath);

		var cut = Render<ProbeComponent>(p => p.Add(x => x.ThrowOnLoad, true));

		cut.Instance.ModuleValue.Should().NotBeNull();
		cut.Instance.Calls.Should().Equal("loaded:True", "after:True");
	}

	/// <summary>Disposal releases the module reference, and a second disposal is harmless.</summary>
	[Fact]
	public async Task DisposeAsync_ReleasesModule()
	{
		JSInterop.SetupModule(ModulePath);
		var cut = Render<ProbeComponent>();

		await cut.Instance.DisposeAsync();
		await cut.Instance.DisposeAsync();

		cut.Instance.ModuleValue.Should().BeNull();
	}

	/// <summary>Disposal before any module was loaded completes without error.</summary>
	[Fact]
	public async Task DisposeAsync_WithoutModule_Completes()
	{
		var probe = new ProbeComponent();

		await probe.DisposeAsync();

		probe.ModuleValue.Should().BeNull();
	}

	/// <summary>A minimal concrete subclass that records which hooks ran.</summary>
	private sealed class ProbeComponent : JSModuleComponentBase
	{
		[Microsoft.AspNetCore.Components.Parameter]
		public bool ThrowOnLoad { get; set; }

		[Microsoft.AspNetCore.Components.Parameter]
		public bool ThrowAfterRender { get; set; }

		public List<string> Calls { get; } = [];

		public IJSObjectReference? ModuleValue => Module;

		protected override string ModulePath => JSModuleComponentBaseTests.ModulePath;

		protected override Task OnModuleLoadedAsync(bool firstRender)
		{
			Calls.Add($"loaded:{firstRender}");
			return ThrowOnLoad ? throw new InvalidOperationException("load failed") : Task.CompletedTask;
		}

		protected override Task OnAfterRenderWithModuleAsync(bool firstRender)
		{
			Calls.Add($"after:{firstRender}");
			return ThrowAfterRender ? throw new InvalidOperationException("render failed") : Task.CompletedTask;
		}
	}
}
