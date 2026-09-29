using AwesomeAssertions;
using Bunit;
using Microsoft.AspNetCore.Components;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.JSInterop;
using PanoramicData.Blazor.Exceptions;
using PanoramicData.Blazor.Interfaces;

namespace PanoramicData.Blazor.Test;

/// <summary>
/// Tests that <see cref="PDLocalStorageStateManager"/> round-trips state through its local storage module,
/// cascades itself to its children, and reports every failure as a <see cref="StateException"/>.
/// </summary>
public class PDLocalStorageStateManagerTests : BunitContext
{
	private const string ModulePath = "./_content/PanoramicData.Blazor/PDLocalStorageStateManager.razor.js";

	/// <summary>Verifies that saving serialises the state to JSON and hands it to setItem under the key.</summary>
	[Fact]
	public async Task Saving_serialises_the_state_to_setItem()
	{
		var module = JSInterop.SetupModule(ModulePath);
		module.SetupVoid("setItem", _ => true).SetVoidResult();
		var component = Render<PDLocalStorageStateManager>();

		await component.InvokeAsync(() => component.Instance.SaveStateAsync("grid", new SampleState { Page = 3 }));

		var call = module.VerifyInvoke("setItem");
		call.Arguments[0].Should().Be("grid");
		call.Arguments[1].Should().Be("{\"Page\":3}");
	}

	/// <summary>Verifies that loading deserialises what getItem returns.</summary>
	[Fact]
	public async Task Loading_deserialises_what_getItem_returns()
	{
		var module = JSInterop.SetupModule(ModulePath);
		module.Setup<string>("getItem", "grid").SetResult("{\"Page\":7}");
		var component = Render<PDLocalStorageStateManager>();

		var state = await component.InvokeAsync(() => component.Instance.LoadStateAsync<SampleState>("grid"));

		state.Should().NotBeNull();
		state!.Page.Should().Be(7);
	}

	/// <summary>Verifies that a key with nothing stored loads as the default rather than failing.</summary>
	[Fact]
	public async Task A_missing_key_loads_as_default()
	{
		var module = JSInterop.SetupModule(ModulePath);
		module.Setup<string>("getItem", "absent").SetResult(null!);
		var component = Render<PDLocalStorageStateManager>();

		var state = await component.InvokeAsync(() => component.Instance.LoadStateAsync<SampleState>("absent"));

		state.Should().BeNull();
	}

	/// <summary>Verifies that removing state calls removeItem with the key.</summary>
	[Fact]
	public async Task Removing_calls_removeItem_with_the_key()
	{
		var module = JSInterop.SetupModule(ModulePath);
		module.SetupVoid("removeItem", "grid").SetVoidResult();
		var component = Render<PDLocalStorageStateManager>();

		await component.InvokeAsync(() => component.Instance.RemoveStateAsync("grid"));

		module.VerifyInvoke("removeItem").Arguments[0].Should().Be("grid");
	}

	/// <summary>Verifies that stored text that is not valid JSON fails as a StateException wrapping the cause.</summary>
	[Fact]
	public async Task Unreadable_stored_state_fails_as_a_StateException()
	{
		var module = JSInterop.SetupModule(ModulePath);
		module.Setup<string>("getItem", "grid").SetResult("not json");
		var component = Render<PDLocalStorageStateManager>();

		var act = () => component.InvokeAsync(() => component.Instance.LoadStateAsync<SampleState>("grid"));

		(await act.Should().ThrowAsync<StateException>()).Which.InnerException.Should().NotBeNull();
	}

	/// <summary>
	/// Verifies that every operation fails as a StateException when the module was never imported, which is
	/// the state of a manager used before it has rendered.
	/// </summary>
	[Fact]
	public async Task Every_operation_fails_before_initialisation()
	{
		var manager = new PDLocalStorageStateManager();

		await FluentActions.Awaiting(() => manager.LoadStateAsync<SampleState>("k"))
			.Should().ThrowAsync<StateException>().WithInnerException(typeof(InvalidOperationException));
		await FluentActions.Awaiting(() => manager.SaveStateAsync("k", new SampleState()))
			.Should().ThrowAsync<StateException>().WithInnerException(typeof(InvalidOperationException));
		await FluentActions.Awaiting(() => manager.RemoveStateAsync("k"))
			.Should().ThrowAsync<StateException>().WithInnerException(typeof(InvalidOperationException));
	}

	/// <summary>
	/// Verifies that initialising twice imports the module only once, so the first-render import and an
	/// explicit call do not race.
	/// </summary>
	[Fact]
	public async Task Initialising_again_does_not_import_again()
	{
		JSInterop.SetupModule(ModulePath);
		var component = Render<PDLocalStorageStateManager>();

		await component.InvokeAsync(() => component.Instance.InitializeAsync());

		JSInterop.Invocations["import"].Should().ContainSingle();
	}

	/// <summary>Verifies that the manager cascades itself to its children as an IAsyncStateManager.</summary>
	[Fact]
	public void The_manager_cascades_itself_to_its_children()
	{
		JSInterop.SetupModule(ModulePath);

		var component = Render<PDLocalStorageStateManager>(parameters => parameters
			.Add(p => p.ChildContent, (RenderFragment)(b =>
			{
				b.OpenComponent<StateConsumer>(0);
				b.CloseComponent();
			})));

		component.FindComponent<StateConsumer>().Instance.StateManager.Should().BeSameAs(component.Instance);
		component.Find(".pd-state-manager").Should().NotBeNull();
	}

	/// <summary>Verifies that disposing tolerates a JS runtime that has already disconnected.</summary>
	[Fact]
	public async Task Disposing_tolerates_a_disconnected_runtime()
	{
		var module = new DisconnectedModule();
		Services.AddSingleton<IJSRuntime>(new ModuleOnlyRuntime(module));
		var component = Render<PDLocalStorageStateManager>();

		var act = async () => await component.Instance.DisposeAsync();

		await act.Should().NotThrowAsync();
		module.DisposeAttempts.Should().Be(1);
	}

	/// <summary>Verifies that operations after disposal fail, since the module reference has been released.</summary>
	[Fact]
	public async Task Operations_after_disposal_fail()
	{
		JSInterop.SetupModule(ModulePath);
		var component = Render<PDLocalStorageStateManager>();

		await component.Instance.DisposeAsync();

		await FluentActions.Awaiting(() => component.Instance.RemoveStateAsync("k"))
			.Should().ThrowAsync<StateException>();
	}

	/// <summary>A state shape small enough to assert its exact JSON.</summary>
	private sealed class SampleState
	{
		public int Page { get; set; }
	}

	/// <summary>A JS runtime whose only job is to hand out one module on import.</summary>
	private sealed class ModuleOnlyRuntime(IJSObjectReference module) : IJSRuntime
	{
		public ValueTask<TValue> InvokeAsync<TValue>(string identifier, object?[]? args)
			=> ValueTask.FromResult((TValue)module);

		public ValueTask<TValue> InvokeAsync<TValue>(string identifier, CancellationToken cancellationToken, object?[]? args)
		{
			cancellationToken.ThrowIfCancellationRequested();
			return ValueTask.FromResult((TValue)module);
		}
	}

	/// <summary>A module whose disposal fails the way it does once the circuit has gone.</summary>
	private sealed class DisconnectedModule : IJSObjectReference
	{
		public int DisposeAttempts { get; private set; }

		public ValueTask DisposeAsync()
		{
			DisposeAttempts++;
			throw new JSDisconnectedException("The circuit has disconnected.");
		}

		public ValueTask<TValue> InvokeAsync<TValue>(string identifier, object?[]? args)
			=> throw new JSDisconnectedException($"{identifier} with {args?.Length ?? 0} argument(s): the circuit has disconnected.");

		public ValueTask<TValue> InvokeAsync<TValue>(string identifier, CancellationToken cancellationToken, object?[]? args)
		{
			cancellationToken.ThrowIfCancellationRequested();
			return InvokeAsync<TValue>(identifier, args);
		}
	}

	/// <summary>Captures the cascaded state manager.</summary>
	private sealed class StateConsumer : ComponentBase
	{
		[CascadingParameter]
		public IAsyncStateManager? StateManager { get; set; }
	}
}
