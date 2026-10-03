using AwesomeAssertions;
using Bunit;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.JSInterop;

namespace PanoramicData.Blazor.Test;

/// <summary>
/// Tests of how <see cref="PDDropDown"/> drives its JavaScript dropdown, including after disposal and when the
/// browser is gone.
/// </summary>
public partial class PDDropDownTests
{
	/// <summary>Verifies that the close option is passed to the JavaScript dropdown in its expected form.</summary>
	[Theory]
	[InlineData(PDDropDown.CloseOptions.Inside, "inside")]
	[InlineData(PDDropDown.CloseOptions.Outside, "outside")]
	[InlineData(PDDropDown.CloseOptions.InsideOrOutside, true)]
	[InlineData(PDDropDown.CloseOptions.Manual, false)]
	public void The_close_option_is_passed_on_initialisation(PDDropDown.CloseOptions option, object expected)
	{
		var module = JSInterop.SetupModule(ModulePath);

		var component = Render<PDDropDown>(parameters => parameters
			.Add(p => p.Id, "menu")
			.Add(p => p.CloseOption, option));

		var init = module.VerifyInvoke("initialize");
		init.Arguments.Take(3).Should().Equal("menu", "menu-toggle", "menu-dropdown");
		init.Arguments[3].Should().BeOfType<DotNetObjectReference<PDDropDown>>()
			.Which.Value.Should().BeSameAs(component.Instance);
		var options = init.Arguments[4]!;
		options.GetType().GetProperty("autoClose")!.GetValue(options).Should().Be(expected);
	}

	/// <summary>Verifies that Show, Hide and Toggle are forwarded to the JavaScript dropdown.</summary>
	[Fact]
	public async Task Show_hide_and_toggle_are_forwarded()
	{
		var dropdown = SetupDropdownObject();
		var component = Render<PDDropDown>();

		await component.InvokeAsync(component.Instance.ShowAsync);
		await component.InvokeAsync(component.Instance.HideAsync);
		await component.InvokeAsync(component.Instance.ToggleAsync);

		dropdown.Invocations.Select(i => i.Identifier).Should().Equal("show", "hide", "toggle");
	}

	/// <summary>Verifies that nothing is forwarded after disposal, since the JavaScript objects are released.</summary>
	[Fact]
	public async Task Nothing_is_forwarded_after_disposal()
	{
		var dropdown = SetupDropdownObject();
		var component = Render<PDDropDown>();

		await component.Instance.DisposeAsync();
		await component.InvokeAsync(component.Instance.ShowAsync);

		dropdown.Invocations.Should().BeEmpty();
	}

	/// <summary>
	/// Verifies that a dropdown whose JavaScript side has gone (a disconnected circuit) does not throw from
	/// show, hide, toggle or disposal.
	/// </summary>
	[Fact]
	public async Task A_disconnected_dropdown_does_not_throw()
	{
		Services.AddSingleton<IJSRuntime>(new DisconnectedRuntime());
		var component = Render<PDDropDown>();

		var act = async () =>
		{
			await component.InvokeAsync(component.Instance.ShowAsync);
			await component.InvokeAsync(component.Instance.HideAsync);
			await component.InvokeAsync(component.Instance.ToggleAsync);
			await component.Instance.DisposeAsync();
		};

		await act.Should().NotThrowAsync();
	}

	/// <summary>Verifies that a failed module import leaves a dropdown that renders and ignores show requests.</summary>
	[Fact]
	public async Task A_failed_import_leaves_a_working_button()
	{
		Services.AddSingleton<IJSRuntime>(new OfflineRuntime());
		var component = Render<PDDropDown>(parameters => parameters.Add(p => p.Text, "Menu"));

		await component.InvokeAsync(component.Instance.ShowAsync);

		component.Find("button").TextContent.Should().Contain("Menu");
	}

	/// <summary>
	/// A runtime whose module initialises but whose dropdown object then fails every call, as it does once
	/// the circuit has gone.
	/// </summary>
	private sealed class DisconnectedRuntime : IJSRuntime
	{
		private readonly DisconnectedObject _module = new();

		public ValueTask<TValue> InvokeAsync<TValue>(string identifier, object?[]? args)
			=> identifier == "import"
				? ValueTask.FromResult((TValue)(object)_module)
				: throw new InvalidOperationException($"Unexpected runtime call '{identifier}' with {args?.Length ?? 0} args.");

		public ValueTask<TValue> InvokeAsync<TValue>(string identifier, CancellationToken cancellationToken, object?[]? args)
		{
			cancellationToken.ThrowIfCancellationRequested();
			return InvokeAsync<TValue>(identifier, args);
		}
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

	/// <summary>A JS object that hands itself back on initialise and fails everything else.</summary>
	private sealed class DisconnectedObject : IJSObjectReference
	{
		public ValueTask DisposeAsync() => throw new JSDisconnectedException("gone");

		public ValueTask<TValue> InvokeAsync<TValue>(string identifier, object?[]? args)
			=> identifier == "initialize"
				? ValueTask.FromResult((TValue)(object)this)
				: throw new JSDisconnectedException($"{identifier} ({args?.Length ?? 0} args)");

		public ValueTask<TValue> InvokeAsync<TValue>(string identifier, CancellationToken cancellationToken, object?[]? args)
		{
			cancellationToken.ThrowIfCancellationRequested();
			return InvokeAsync<TValue>(identifier, args);
		}
	}
}
