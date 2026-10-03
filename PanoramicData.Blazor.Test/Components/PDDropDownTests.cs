using AwesomeAssertions;
using Bunit;
using Microsoft.AspNetCore.Components.Web;
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

	private BunitJSModuleInterop SetupDropdownObject()
	{
		var module = JSInterop.SetupModule(ModulePath);
		return module.SetupModule("initialize", _ => true);
	}
}
