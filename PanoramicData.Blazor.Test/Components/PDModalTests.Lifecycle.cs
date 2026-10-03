using AwesomeAssertions;
using Bunit;
using Microsoft.AspNetCore.Components;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.JSInterop;

namespace PanoramicData.Blazor.Test;

/// <summary>
/// Navigation, disposal and failed-interop tests for <see cref="PDModal"/>.
/// </summary>
public partial class PDModalTests
{
	/// <summary>Verifies that navigating away hides the dialog and sweeps any orphaned backdrop.</summary>
	[Fact]
	public void Navigating_hides_the_dialog_and_sweeps_backdrops()
	{
		var module = JSInterop.SetupModule(ModulePath);
		var modal = module.SetupModule("initialize", _ => true);
		Render<PDModal>();

		Services.GetRequiredService<NavigationManager>().NavigateTo("/elsewhere");

		modal.VerifyInvoke("hide");
		module.VerifyInvoke("cleanupBackdrops");
	}

	/// <summary>Verifies that HideOnNavigation false leaves the dialog alone on navigation.</summary>
	[Fact]
	public void Navigation_can_leave_the_dialog_alone()
	{
		var module = JSInterop.SetupModule(ModulePath);
		var modal = module.SetupModule("initialize", _ => true);
		Render<PDModal>(parameters => parameters.Add(p => p.HideOnNavigation, false));

		Services.GetRequiredService<NavigationManager>().NavigateTo("/elsewhere");

		modal.Invocations.Should().BeEmpty();
		module.Invocations.Should().NotContain(i => i.Identifier == "cleanupBackdrops");
	}

	/// <summary>
	/// Verifies that a disposed dialog ignores show, hide and navigation, and that disposing twice is harmless.
	/// </summary>
	[Fact]
	public async Task A_disposed_dialog_ignores_everything()
	{
		var modal = SetupModalObject();
		var component = Render<PDModal>();

		await component.Instance.DisposeAsync();
		await component.Instance.DisposeAsync();
		await component.InvokeAsync(component.Instance.ShowAsync);
		await component.InvokeAsync(component.Instance.HideAsync);
		Services.GetRequiredService<NavigationManager>().NavigateTo("/elsewhere");

		modal.Invocations.Should().BeEmpty();
	}

	/// <summary>
	/// Verifies that show, hide and disposal tolerate a JavaScript side that has gone, whether the circuit
	/// disconnected or the reference was already disposed.
	/// </summary>
	[Theory]
	[InlineData(true)]
	[InlineData(false)]
	public async Task A_departed_javascript_side_is_tolerated(bool disconnected)
	{
		Exception failure = disconnected ? new JSDisconnectedException("gone") : new ObjectDisposedException("modal");
		var runtime = new FailingRuntime(failure);
		Services.AddSingleton<IJSRuntime>(runtime);
		var component = Render<PDModal>();

		var act = async () =>
		{
			await component.InvokeAsync(component.Instance.ShowAsync);
			await component.InvokeAsync(component.Instance.HideAsync);
			Services.GetRequiredService<NavigationManager>().NavigateTo("/elsewhere");
			await component.Instance.DisposeAsync();
		};

		await act.Should().NotThrowAsync();
		runtime.Module.Calls.Select(call => call.Identifier).Should().Contain(["initialize", "show"]);
	}

	/// <summary>Verifies that a failed module import leaves a dialog that renders and ignores show requests.</summary>
	[Fact]
	public async Task A_failed_import_leaves_an_inert_dialog()
	{
		Services.AddSingleton<IJSRuntime>(new OfflineRuntime());
		var component = Render<PDModal>(parameters => parameters.Add(p => p.Title, "Still here"));

		await component.InvokeAsync(component.Instance.ShowAsync);

		component.Find(".modal-title").TextContent.Should().Be("Still here");
	}

	/// <summary>A runtime that cannot import anything.</summary>
	private sealed class OfflineRuntime : IJSRuntime
	{
		public ValueTask<TValue> InvokeAsync<TValue>(string identifier, object?[]? args)
			=> throw new JSException($"{identifier} ({args?.Length ?? 0} args) failed: offline");

		public ValueTask<TValue> InvokeAsync<TValue>(string identifier, CancellationToken cancellationToken, object?[]? args)
		{
			cancellationToken.ThrowIfCancellationRequested();
			return InvokeAsync<TValue>(identifier, args);
		}
	}

	/// <summary>A runtime whose modules initialise but then fail every call with the given exception.</summary>
	private sealed class FailingRuntime(Exception failure) : IJSRuntime
	{
		/// <summary>Gets the module handed out on every import.</summary>
		public FailingObject Module { get; } = new(failure);

		public ValueTask<TValue> InvokeAsync<TValue>(string identifier, object?[]? args)
			=> identifier == "import"
				? ValueTask.FromResult((TValue)(object)Module)
				: throw new InvalidOperationException($"Unexpected JS call '{identifier}' with {args?.Length ?? 0} args.");

		public ValueTask<TValue> InvokeAsync<TValue>(string identifier, CancellationToken cancellationToken, object?[]? args)
		{
			cancellationToken.ThrowIfCancellationRequested();
			return InvokeAsync<TValue>(identifier, args);
		}
	}

	/// <summary>A JS object that hands itself back on initialise and fails everything else.</summary>
	private sealed class FailingObject(Exception failure) : IJSObjectReference
	{
		/// <summary>Gets each call made on this object, with the number of arguments it was given.</summary>
		public List<(string Identifier, int ArgumentCount)> Calls { get; } = [];

		public ValueTask DisposeAsync() => ValueTask.FromException(failure);

		public ValueTask<TValue> InvokeAsync<TValue>(string identifier, object?[]? args)
		{
			Calls.Add((identifier, args?.Length ?? 0));
			return identifier == "initialize"
				? ValueTask.FromResult((TValue)(object)this)
				: ValueTask.FromException<TValue>(failure);
		}

		public ValueTask<TValue> InvokeAsync<TValue>(string identifier, CancellationToken cancellationToken, object?[]? args)
		{
			cancellationToken.ThrowIfCancellationRequested();
			return InvokeAsync<TValue>(identifier, args);
		}
	}
}
