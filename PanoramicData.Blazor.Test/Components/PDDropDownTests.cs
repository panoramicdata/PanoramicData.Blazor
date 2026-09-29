using AwesomeAssertions;
using Bunit;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.JSInterop;
using PanoramicData.Blazor.Models;

namespace PanoramicData.Blazor.Test;

/// <summary>
/// Tests that <see cref="PDDropDown"/> renders its toggle button, initialises its JavaScript dropdown with
/// the chosen close behaviour, forwards show/hide/toggle to it, and reflects the shown state it is told of.
/// </summary>
public partial class PDDropDownTests : BunitContext
{
	private const string ModulePath = "./_content/PanoramicData.Blazor/PDDropDown.razor.js";

	/// <summary>Sets up the rendering context.</summary>
	public PDDropDownTests() => JSInterop.Mode = JSRuntimeMode.Loose;

	/// <summary>Verifies that the button carries the ids, classes, tooltip, icon, text and a closed caret.</summary>
	[Fact]
	public void The_button_renders_its_configuration()
	{
		var component = Render<PDDropDown>(parameters => parameters
			.Add(p => p.Id, "menu")
			.Add(p => p.CssClass, "btn-primary")
			.Add(p => p.IconCssClass, "fas fa-cog")
			.Add(p => p.Text, "Options")
			.Add(p => p.TextCssClass, "fw-bold")
			.Add(p => p.ToolTip, "More options"));

		component.Find(".pd-dropdown").Id.Should().Be("menu");
		var button = component.Find("button");
		button.Id.Should().Be("menu-toggle");
		button.ClassList.Should().Contain("btn-primary").And.Contain("dropdown-toggle");
		button.GetAttribute("title").Should().Be("More options");
		button.HasAttribute("disabled").Should().BeFalse();
		component.Find("button .icon i").ClassName.Should().Be("fas fa-cog");
		component.Find("button span.fw-bold").TextContent.Should().Be("Options");
		component.Find("button i.fa-angle-down").Should().NotBeNull();
		component.Find(".dropdown-menu").Id.Should().Be("menu-dropdown");
	}

	/// <summary>Verifies that the size maps to Bootstrap's button size classes.</summary>
	[Theory]
	[InlineData(ButtonSizes.Small, "btn-sm")]
	[InlineData(ButtonSizes.Large, "btn-lg")]
	public void The_size_maps_to_a_button_size_class(ButtonSizes size, string expected)
	{
		var component = Render<PDDropDown>(parameters => parameters.Add(p => p.Size, size));

		component.Find("button").ClassList.Should().Contain(expected);
	}

	/// <summary>Verifies that a medium button carries no size class.</summary>
	[Fact]
	public void A_medium_button_has_no_size_class()
	{
		var component = Render<PDDropDown>();

		component.Find("button").ClassList.Should().NotContain("btn-sm").And.NotContain("btn-lg");
	}

	/// <summary>Verifies that no icon, text or caret is rendered when none is wanted, and hidden hides.</summary>
	[Fact]
	public void Optional_parts_can_be_left_out_and_the_dropdown_hidden()
	{
		var component = Render<PDDropDown>(parameters => parameters
			.Add(p => p.ShowCaret, false)
			.Add(p => p.Visible, false));

		component.Find("button").Children.Should().BeEmpty();
		component.Find(".pd-dropdown").ClassList.Should().Contain("d-none");
	}

	/// <summary>Verifies that the child content is rendered in the menu.</summary>
	[Fact]
	public void Child_content_renders_in_the_menu()
	{
		var component = Render<PDDropDown>(parameters => parameters
			.AddChildContent("<a class=\"dropdown-item\">One</a>"));

		component.Find(".dropdown-menu .dropdown-item").TextContent.Should().Be("One");
	}

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

	/// <summary>Verifies that being told the dropdown is shown flips the caret and raises DropDownShown.</summary>
	[Fact]
	public async Task Being_shown_flips_the_caret_and_raises_the_event()
	{
		var shown = 0;
		var component = Render<PDDropDown>(parameters => parameters
			.Add(p => p.DropDownShown, () => shown++));

		await component.InvokeAsync(component.Instance.OnDropDownShown);

		shown.Should().Be(1);
		component.Find("button i.fa-angle-up").Should().NotBeNull();
	}

	/// <summary>Verifies that being told the dropdown is hidden restores the caret and raises DropDownHidden.</summary>
	[Fact]
	public async Task Being_hidden_restores_the_caret_and_raises_the_event()
	{
		var hidden = 0;
		var component = Render<PDDropDown>(parameters => parameters
			.Add(p => p.DropDownHidden, () => hidden++));
		await component.InvokeAsync(component.Instance.OnDropDownShown);

		await component.InvokeAsync(component.Instance.OnDropDownHidden);

		hidden.Should().Be(1);
		component.Find("button i.fa-angle-down").Should().NotBeNull();
	}

	/// <summary>Verifies that a key press reported by JavaScript is raised with its key code.</summary>
	[Fact]
	public async Task A_key_press_is_raised_with_its_code()
	{
		var codes = new List<int>();
		var component = Render<PDDropDown>(parameters => parameters
			.Add(p => p.KeyPress, (int code) => codes.Add(code)));

		await component.InvokeAsync(() => component.Instance.OnKeyPressed(27));

		codes.Should().Equal(27);
	}

	/// <summary>Verifies that clicking the button raises Click.</summary>
	[Fact]
	public async Task Clicking_the_button_raises_click()
	{
		var clicks = 0;
		var component = Render<PDDropDown>(parameters => parameters
			.Add(p => p.Click, (MouseEventArgs _) => clicks++));

		await component.Find("button").ClickAsync(new MouseEventArgs());

		clicks.Should().Be(1);
	}

	/// <summary>Verifies that hovering opens the dropdown and leaving closes it when ShowOnMouseEnter is set.</summary>
	[Fact]
	public async Task Hovering_opens_and_leaving_closes_when_enabled()
	{
		var dropdown = SetupDropdownObject();
		var component = Render<PDDropDown>(parameters => parameters.Add(p => p.ShowOnMouseEnter, true));

		await component.Find("button").MouseEnterAsync(new MouseEventArgs());
		await component.InvokeAsync(component.Instance.OnMouseLeave);

		dropdown.Invocations.Select(i => i.Identifier).Should().Equal("show", "hide");
	}

	/// <summary>Verifies that hovering does nothing without ShowOnMouseEnter, or while disabled.</summary>
	[Theory]
	[InlineData(false, true)]
	[InlineData(true, false)]
	public async Task Hovering_does_nothing_otherwise(bool showOnMouseEnter, bool isEnabled)
	{
		var dropdown = SetupDropdownObject();
		var component = Render<PDDropDown>(parameters => parameters
			.Add(p => p.ShowOnMouseEnter, showOnMouseEnter)
			.Add(p => p.IsEnabled, isEnabled));

		await component.Find("button").MouseEnterAsync(new MouseEventArgs());
		await component.InvokeAsync(component.Instance.OnMouseLeave);

		dropdown.Invocations.Should().BeEmpty();
	}

	/// <summary>Verifies that Disable, Enable and SetEnabled change whether the button can be used.</summary>
	[Fact]
	public async Task The_enabled_state_can_be_changed()
	{
		var component = Render<PDDropDown>();

		await component.InvokeAsync(component.Instance.Disable);
		component.Find("button").HasAttribute("disabled").Should().BeTrue();

		await component.InvokeAsync(component.Instance.Enable);
		component.Find("button").HasAttribute("disabled").Should().BeFalse();

		await component.InvokeAsync(() => component.Instance.SetEnabled(false));
		component.Find("button").HasAttribute("disabled").Should().BeTrue();
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

	private BunitJSModuleInterop SetupDropdownObject()
	{
		var module = JSInterop.SetupModule(ModulePath);
		return module.SetupModule("initialize", _ => true);
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
