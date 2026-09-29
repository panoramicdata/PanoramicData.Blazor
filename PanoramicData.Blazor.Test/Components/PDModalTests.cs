using AwesomeAssertions;
using Bunit;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.JSInterop;
using PanoramicData.Blazor.Extensions;
using PanoramicData.Blazor.Models;

namespace PanoramicData.Blazor.Test;

/// <summary>
/// Tests that <see cref="PDModal"/> renders its dialog, initialises its Bootstrap modal with the chosen
/// options, forwards show and hide, resolves a waiting caller with the button chosen, and hides itself on
/// navigation.
/// </summary>
public class PDModalTests : BunitContext
{
	/// <summary>
	/// How long to wait for a render that another thread or a timer brings about. Generous because a busy
	/// machine (the whole suite under coverage) can hold the renderer's dispatcher well past bUnit's default.
	/// </summary>
	private static readonly TimeSpan Patience = TimeSpan.FromSeconds(10);

	private const string ModulePath = "./_content/PanoramicData.Blazor/PDModal.razor.js";

	/// <summary>Sets up the rendering context.</summary>
	public PDModalTests()
	{
		JSInterop.Mode = JSRuntimeMode.Loose;
		Services.AddPanoramicDataBlazor();
	}

	/// <summary>Verifies that the dialog renders its title, body and the default Yes and No buttons.</summary>
	[Fact]
	public void The_dialog_renders_title_body_and_default_buttons()
	{
		var component = Render<PDModal>(parameters => parameters
			.Add(p => p.Id, "dialog")
			.Add(p => p.Title, "Confirm")
			.Add(p => p.CssClass, "fade")
			.Add(p => p.HeaderCssClass, "bg-light")
			.Add(p => p.BodyCssClass, "p-4")
			.AddChildContent("<p>Are you sure?</p>"));

		component.Find(".modal").Id.Should().Be("dialog");
		component.Find(".modal").ClassList.Should().Contain("fade");
		component.Find(".modal-header.bg-light .modal-title").TextContent.Should().Be("Confirm");
		component.Find(".modal-body.p-4 p").TextContent.Should().Be("Are you sure?");
		component.FindAll(".btn-close").Should().BeEmpty();
		FooterButtonTexts(component).Should().Equal("Yes", "No");
	}

	/// <summary>Verifies that the close button is shown on request and hides the dialog.</summary>
	[Fact]
	public async Task The_close_button_hides_the_dialog()
	{
		var modal = SetupModalObject();
		var component = Render<PDModal>(parameters => parameters.Add(p => p.ShowClose, true));

		await component.Find(".btn-close").ClickAsync(new MouseEventArgs());

		modal.VerifyInvoke("hide");
	}

	/// <summary>Verifies that header and footer templates replace the defaults.</summary>
	[Fact]
	public void Header_and_footer_templates_replace_the_defaults()
	{
		var component = Render<PDModal>(parameters => parameters
			.Add(p => p.Title, "Unused")
			.Add(p => p.Header, (RenderFragment)(b => b.AddMarkupContent(0, "<h2>Custom</h2>")))
			.Add(p => p.Footer, (RenderFragment)(b => b.AddMarkupContent(0, "<span class=\"custom-footer\">Done</span>"))));

		component.Find(".modal-header h2").TextContent.Should().Be("Custom");
		component.FindAll(".modal-title").Should().BeEmpty();
		component.Find(".modal-footer .custom-footer").TextContent.Should().Be("Done");
		component.FindAll(".modal-footer button").Should().BeEmpty();
	}

	/// <summary>Verifies that the footer can be left out entirely.</summary>
	[Fact]
	public void The_footer_can_be_left_out()
	{
		var component = Render<PDModal>(parameters => parameters.Add(p => p.ShowFooter, false));

		component.FindAll(".modal-footer").Should().BeEmpty();
	}

	/// <summary>Verifies that each size maps to its Bootstrap dialog class, and centring adds its class.</summary>
	[Theory]
	[InlineData(ModalSizes.Small, false, "modal-dialog modal-sm")]
	[InlineData(ModalSizes.Medium, false, "modal-dialog")]
	[InlineData(ModalSizes.Large, false, "modal-dialog modal-lg")]
	[InlineData(ModalSizes.ExtraLarge, true, "modal-dialog modal-xl modal-dialog-centered")]
	[InlineData(ModalSizes.Medium, true, "modal-dialog modal-dialog-centered")]
	public void The_size_and_centring_map_to_dialog_classes(ModalSizes size, bool centred, string expected)
	{
		var component = Render<PDModal>(parameters => parameters
			.Add(p => p.Size, size)
			.Add(p => p.CenterVertically, centred));

		component.Find(".modal-dialog").ClassName!.Trim().Should().Be(expected);
	}

	/// <summary>Verifies that the Bootstrap modal is initialised with the id, backdrop, keyboard and callback reference.</summary>
	[Theory]
	[InlineData(false, true, "static", true)]
	[InlineData(true, false, true, false)]
	public void The_modal_is_initialised_with_its_options(bool hideOnBackgroundClick, bool closeOnEscape, object backdrop, bool keyboard)
	{
		var module = JSInterop.SetupModule(ModulePath);

		var component = Render<PDModal>(parameters => parameters
			.Add(p => p.Id, "dialog")
			.Add(p => p.HideOnBackgroundClick, hideOnBackgroundClick)
			.Add(p => p.CloseOnEscape, closeOnEscape));

		var init = module.VerifyInvoke("initialize");
		init.Arguments[0].Should().Be("dialog");
		var options = init.Arguments[1]!;
		options.GetType().GetProperty("backdrop")!.GetValue(options).Should().Be(backdrop);
		options.GetType().GetProperty("keyboard")!.GetValue(options).Should().Be(keyboard);
		options.GetType().GetProperty("focus")!.GetValue(options).Should().Be(true);
		init.Arguments[2].Should().BeOfType<DotNetObjectReference<PDModal>>().Which.Value.Should().BeSameAs(component.Instance);
	}

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

	private BunitJSModuleInterop SetupModalObject()
		=> JSInterop.SetupModule(ModulePath).SetupModule("initialize", _ => true);

	private static AngleSharp.Dom.IElement FooterButton(IRenderedComponent<PDModal> component, string text)
		=> component.FindAll(".modal-footer button").Single(b => b.TextContent.Trim() == text);

	private static List<string> FooterButtonTexts(IRenderedComponent<PDModal> component)
		=> [.. component.FindAll(".modal-footer button").Select(b => b.TextContent.Trim())];

	/// <summary>A runtime that cannot import anything.</summary>
	private sealed class OfflineRuntime : IJSRuntime
	{
		public ValueTask<TValue> InvokeAsync<TValue>(string identifier, object?[]? args)
			=> throw new JSException($"{identifier} ({args?.Length ?? 0} args) failed: offline");


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


	}
}
