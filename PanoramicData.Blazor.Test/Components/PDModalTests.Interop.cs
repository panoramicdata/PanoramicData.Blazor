using AwesomeAssertions;
using Bunit;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.JSInterop;
using PanoramicData.Blazor.Models;

namespace PanoramicData.Blazor.Test;

/// <summary>
/// JavaScript interop tests for <see cref="PDModal"/>: showing, hiding, button clicks and waiting callers.
/// </summary>
public partial class PDModalTests
{
	/// <summary>Verifies that Show and Hide are forwarded to the Bootstrap modal.</summary>
	[Fact]
	public async Task Show_and_hide_are_forwarded()
	{
		var modal = SetupModalObject();
		var component = Render<PDModal>();

		await component.InvokeAsync(component.Instance.ShowAsync);
		await component.InvokeAsync(component.Instance.HideAsync);

		modal.Invocations.Select(i => i.Identifier).Should().Equal("show", "hide");
	}

	/// <summary>Verifies that a footer button click is forwarded with its key when no caller is waiting.</summary>
	[Fact]
	public async Task A_button_click_is_forwarded_with_its_key()
	{
		var keys = new List<string>();
		var component = Render<PDModal>(parameters => parameters
			.Add(p => p.ButtonClick, (string key) => keys.Add(key)));

		await FooterButton(component, "No").ClickAsync(new MouseEventArgs());

		keys.Should().Equal(ModalResults.NO);
	}

	/// <summary>
	/// Verifies that a caller awaiting the dialog receives the key of the button chosen, that the primary
	/// button is focused, and that the dialog is hidden afterwards.
	/// </summary>
	[Fact]
	public async Task A_waiting_caller_receives_the_chosen_button()
	{
		var common = JSInterop.SetupModule(JSInteropVersionHelper.CommonJsUrl);
		var modal = SetupModalObject();
		var forwarded = new List<string>();
		var component = Render<PDModal>(parameters => parameters
			.Add(p => p.ButtonClick, (string key) => forwarded.Add(key)));

		var choice = component.InvokeAsync(component.Instance.ShowAndWaitResultAsync);
		component.WaitForAssertion(() => common.VerifyInvoke("focus").Arguments[0].Should().Be("pd-tbr-btn-Yes"), Patience);
		await FooterButton(component, "Yes").ClickAsync(new());

		(await choice).Should().Be(ModalResults.YES);
		forwarded.Should().BeEmpty();
		modal.Invocations.Select(i => i.Identifier).Should().Equal("show", "hide");
	}

	/// <summary>Verifies that a cancelled wait resolves to an empty choice and hides the dialog.</summary>
	[Fact]
	public async Task A_cancelled_wait_resolves_empty_and_hides()
	{
		var modal = SetupModalObject();
		var component = Render<PDModal>(parameters => parameters
			.Add(p => p.Buttons, [new ToolbarButton { Key = "Ok", Text = "Ok" }]));
		using var cancellation = new CancellationTokenSource();

		var choice = component.InvokeAsync(() => component.Instance.ShowAndWaitResultAsync(cancellation.Token));
		component.WaitForAssertion(() => modal.VerifyInvoke("show"), Patience);
		await cancellation.CancelAsync();

		(await choice).Should().BeEmpty();
		modal.VerifyInvoke("hide");
	}

	/// <summary>Verifies that no button is focused when none is a keyed primary button.</summary>
	[Fact]
	public async Task No_button_is_focused_without_a_primary_button()
	{
		var common = JSInterop.SetupModule(JSInteropVersionHelper.CommonJsUrl);
		SetupModalObject();
		var component = Render<PDModal>(parameters => parameters
			.Add(p => p.Buttons, [new ToolbarSeparator(), new ToolbarButton { Key = "Ok", Text = "Ok" }]));

		var choice = component.InvokeAsync(component.Instance.ShowAndWaitResultAsync);
		await FooterButton(component, "Ok").ClickAsync(new());

		(await choice).Should().Be("Ok");
		common.Invocations.Should().NotContain(i => i.Identifier == "focus");
	}

	/// <summary>Verifies that the shown and hidden notifications from JavaScript raise Shown and Hidden.</summary>
	[Fact]
	public async Task Shown_and_hidden_notifications_raise_their_events()
	{
		var events = new List<string>();
		var component = Render<PDModal>(parameters => parameters
			.Add(p => p.Shown, () => events.Add("shown"))
			.Add(p => p.Hidden, () => events.Add("hidden")));

		await component.InvokeAsync(component.Instance.OnModalShown);
		await component.InvokeAsync(component.Instance.OnModalHidden);

		events.Should().Equal("shown", "hidden");
	}
}
