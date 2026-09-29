using AwesomeAssertions;
using Bunit;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.Extensions.DependencyInjection;
using PanoramicData.Blazor.Extensions;
using PanoramicData.Blazor.Interfaces;
using PanoramicData.Blazor.Models;

namespace PanoramicData.Blazor.Test.Components;

/// <summary>
/// Tests that <see cref="PDButton"/> renders its content, honours its enabled state, raises its callbacks,
/// runs async operations and responds to its shortcut key.
/// </summary>
public class PDButtonTests : BunitContext
{
	/// <summary>Sets up the rendering context.</summary>
	public PDButtonTests()
	{
		JSInterop.Mode = JSRuntimeMode.Loose;
		Services.AddPanoramicDataBlazor();
	}

	private IGlobalEventService GlobalEvents => Services.GetRequiredService<IGlobalEventService>();

	/// <summary>A button with text and an icon renders both, with padding before the text.</summary>
	[Fact]
	public void Text_and_icon_render_with_padding()
	{
		var component = Render<PDButton>(parameters => parameters
			.Add(p => p.Id, "b1")
			.Add(p => p.Text, "Save")
			.Add(p => p.IconCssClass, "fas fa-save")
			.Add(p => p.TextCssClass, "txt")
			.Add(p => p.CssClass, "btn-primary"));

		var button = component.Find("button");
		button.Id.Should().Be("b1");
		button.ClassList.Should().Contain("pd-button").And.Contain("btn-primary");
		button.HasAttribute("disabled").Should().BeFalse();
		component.Find("span.icon").ClassList.Should().Contain("fa-save");
		var text = component.Find("span.txt");
		text.ClassList.Should().Contain("ps-1");
		text.TextContent.Should().Be("Save");
	}

	/// <summary>A text-only button has no icon and no padding class.</summary>
	[Fact]
	public void Text_only_has_no_icon_or_padding()
	{
		var component = Render<PDButton>(parameters => parameters.Add(p => p.Text, "Plain"));

		component.FindAll("span.icon").Should().BeEmpty();
		component.Find("span").ClassList.Should().NotContain("ps-1");
	}

	/// <summary>A double ampersand in the text underlines the following character.</summary>
	[Fact]
	public void Shortcut_markup_underlines_the_marked_character()
	{
		var component = Render<PDButton>(parameters => parameters.Add(p => p.Text, "&&Open"));

		component.Find("u").TextContent.Should().Be("O");
	}

	/// <summary>Child content replaces the text and icon.</summary>
	[Fact]
	public void Child_content_replaces_the_text_and_icon()
	{
		var component = Render<PDButton>(parameters => parameters
			.Add(p => p.Text, "Ignored")
			.Add(p => p.IconCssClass, "fas fa-x")
			.Add(p => p.ChildContent, (RenderFragment)(b => b.AddMarkupContent(0, "<b>Custom</b>"))));

		component.Find("button b").TextContent.Should().Be("Custom");
		component.FindAll("span.icon").Should().BeEmpty();
		component.Markup.Should().NotContain("Ignored");
	}

	/// <summary>The size maps to the Bootstrap size classes.</summary>
	[Theory]
	[InlineData(ButtonSizes.Small, "btn-sm")]
	[InlineData(ButtonSizes.Large, "btn-lg")]
	public void Size_maps_to_bootstrap_class(ButtonSizes size, string expected)
	{
		var component = Render<PDButton>(parameters => parameters.Add(p => p.Size, size));

		component.Find("button").ClassList.Should().Contain(expected);
	}

	/// <summary>A medium button has neither size class.</summary>
	[Fact]
	public void Medium_size_has_no_size_class()
	{
		var component = Render<PDButton>(parameters => parameters.Add(p => p.Size, ButtonSizes.Medium));

		component.Find("button").ClassList.Should().NotContain("btn-sm").And.NotContain("btn-lg");
	}

	/// <summary>The tooltip carries the shortcut key when one is set.</summary>
	[Fact]
	public void Tooltip_includes_the_shortcut()
	{
		var component = Render<PDButton>(parameters => parameters
			.Add(p => p.ToolTip, "Save file")
			.Add(p => p.ShortcutKey, ShortcutKey.Create("ctrl-KeyS")));

		component.Find("button").GetAttribute("title").Should().Be("Save file (Ctrl-S)");
	}

	/// <summary>Extra attributes are passed through to the element.</summary>
	[Fact]
	public void Attributes_are_splatted_onto_the_element()
	{
		var component = Render<PDButton>(parameters => parameters
			.Add(p => p.Attributes, new Dictionary<string, object> { ["data-test"] = "yes" }));

		component.Find("button").GetAttribute("data-test").Should().Be("yes");
	}

	/// <summary>Clicking raises Click with the mouse arguments.</summary>
	[Fact]
	public void Click_raises_the_click_callback()
	{
		MouseEventArgs? received = null;
		var component = Render<PDButton>(parameters => parameters
			.Add(p => p.Click, (MouseEventArgs args) => received = args));

		component.Find("button").Click(new MouseEventArgs { Button = 2 });

		received.Should().NotBeNull();
		received!.Button.Should().Be(2);
	}

	/// <summary>Mouse down and mouse enter raise their callbacks.</summary>
	[Fact]
	public void Mouse_down_and_enter_raise_their_callbacks()
	{
		var downs = 0;
		var enters = 0;
		var component = Render<PDButton>(parameters => parameters
			.Add(p => p.MouseDown, (MouseEventArgs _) => downs++)
			.Add(p => p.MouseEnter, (MouseEventArgs _) => enters++));

		component.Find("button").MouseDown();
		component.Find("button").MouseEnter();

		downs.Should().Be(1);
		enters.Should().Be(1);
	}

	/// <summary>
	/// While an operation runs the button is disabled and shows the operation icon; when it completes the
	/// button is enabled again and Click is not raised.
	/// </summary>
	[Fact]
	public async Task Operation_disables_the_button_while_it_runs()
	{
		var gate = new TaskCompletionSource();
		var clicks = 0;
		var component = Render<PDButton>(parameters => parameters
			.Add(p => p.IconCssClass, "fas fa-play")
			.Add(p => p.OperationIconCssClass, "fas fa-spinner")
			.Add(p => p.Click, () => clicks++)
			.Add(p => p.Operation, _ => gate.Task));

		var click = component.Find("button").ClickAsync(new MouseEventArgs());

		component.WaitForAssertion(() => component.Find("button").HasAttribute("disabled").Should().BeTrue());
		component.Find("span.icon").ClassList.Should().Contain("fa-spinner");

		gate.SetResult();
		await click;

		component.WaitForAssertion(() => component.Find("button").HasAttribute("disabled").Should().BeFalse());
		component.Find("span.icon").ClassList.Should().Contain("fa-play");
		clicks.Should().Be(0);
	}

	/// <summary>Without an operation icon the normal icon stays in place while an operation runs.</summary>
	[Fact]
	public void Operation_without_operation_icon_keeps_the_icon()
	{
		var gate = new TaskCompletionSource();
		var component = Render<PDButton>(parameters => parameters
			.Add(p => p.IconCssClass, "fas fa-play")
			.Add(p => p.Operation, _ => gate.Task));

		_ = component.Find("button").ClickAsync(new MouseEventArgs());

		component.WaitForAssertion(() => component.Find("button").HasAttribute("disabled").Should().BeTrue());
		component.Find("span.icon").ClassList.Should().Contain("fa-play");
		gate.SetResult();
	}

	/// <summary>A disabled button renders the disabled attribute.</summary>
	[Fact]
	public void IsEnabled_false_renders_disabled()
	{
		var component = Render<PDButton>(parameters => parameters.Add(p => p.IsEnabled, false));

		component.Find("button").HasAttribute("disabled").Should().BeTrue();
	}

	/// <summary>Enable, Disable and SetEnabled change the rendered state.</summary>
	[Fact]
	public async Task Enable_disable_and_set_enabled_update_the_markup()
	{
		var component = Render<PDButton>();

		await component.InvokeAsync(component.Instance.Disable);
		component.Find("button").HasAttribute("disabled").Should().BeTrue();

		await component.InvokeAsync(component.Instance.Enable);
		component.Find("button").HasAttribute("disabled").Should().BeFalse();

		await component.InvokeAsync(() => component.Instance.SetEnabled(false));
		component.Find("button").HasAttribute("disabled").Should().BeTrue();
		component.Instance.IsEnabled.Should().BeFalse();
	}

	/// <summary>A URL renders an anchor with href, target, icon and padded text.</summary>
	[Fact]
	public void Url_renders_an_anchor()
	{
		var component = Render<PDButton>(parameters => parameters
			.Add(p => p.Url, "https://example.com/")
			.Add(p => p.Target, "_blank")
			.Add(p => p.Text, "Go")
			.Add(p => p.IconCssClass, "fas fa-link")
			.Add(p => p.IsEnabled, false));

		component.FindAll("button").Should().BeEmpty();
		var anchor = component.Find("a");
		anchor.GetAttribute("href").Should().Be("https://example.com/");
		anchor.GetAttribute("target").Should().Be("_blank");
		anchor.HasAttribute("disabled").Should().BeTrue();
		component.Find("a span.icon").ClassList.Should().Contain("fa-link");
		component.FindAll("a span")[1].ClassList.Should().Contain("ps-1");
	}

	/// <summary>An anchor without an icon has no padding, and child content replaces its text.</summary>
	[Fact]
	public void Url_anchor_without_icon_and_with_child_content()
	{
		var plain = Render<PDButton>(parameters => parameters
			.Add(p => p.Url, "/x")
			.Add(p => p.Text, "Go"));
		plain.Find("a span").ClassList.Should().NotContain("ps-1");
		plain.Find("a").GetAttribute("target").Should().Be("_self");

		var custom = Render<PDButton>(parameters => parameters
			.Add(p => p.Url, "/x")
			.Add(p => p.ChildContent, (RenderFragment)(b => b.AddContent(0, "Inner"))));
		custom.Find("a").TextContent.Should().Be("Inner");
		custom.FindAll("a span").Should().BeEmpty();
	}

	/// <summary>Clicking an anchor raises Click.</summary>
	[Fact]
	public void Url_anchor_click_raises_click()
	{
		var clicks = 0;
		var component = Render<PDButton>(parameters => parameters
			.Add(p => p.Url, "/x")
			.Add(p => p.Click, () => clicks++));

		component.Find("a").Click();

		clicks.Should().Be(1);
	}

	/// <summary>A shortcut key is registered while the button lives and unregistered when disposed.</summary>
	[Fact]
	public void Shortcut_is_registered_and_unregistered()
	{
		var component = Render<PDButton>(parameters => parameters
			.Add(p => p.ShortcutKey, ShortcutKey.Create("alt-KeyQ")));

		GlobalEvents.GetRegisteredShortcuts().Select(s => s.ToString()).Should().Contain("Alt-Q");

		component.Instance.Dispose();

		GlobalEvents.GetRegisteredShortcuts().Should().BeEmpty();
	}

	/// <summary>A matching key up raises Click; a non-matching one does not.</summary>
	[Fact]
	public void Matching_key_up_raises_click()
	{
		var clicks = 0;
		var component = Render<PDButton>(parameters => parameters
			.Add(p => p.ShortcutKey, ShortcutKey.Create("ctrl-KeyS"))
			.Add(p => p.Click, () => clicks++));

		GlobalEvents.KeyUp(new KeyboardInfo { Key = "x", Code = "KeyX", CtrlKey = true });
		clicks.Should().Be(0);

		GlobalEvents.KeyUp(new KeyboardInfo { Key = "s", Code = "KeyS", CtrlKey = true });
		component.WaitForAssertion(() => clicks.Should().Be(1));
	}

	/// <summary>Without a shortcut key, key presses never raise Click and nothing is registered.</summary>
	[Fact]
	public void No_shortcut_ignores_key_up()
	{
		var clicks = 0;
		var component = Render<PDButton>(parameters => parameters.Add(p => p.Click, () => clicks++));

		GlobalEvents.GetRegisteredShortcuts().Should().BeEmpty();
		GlobalEvents.KeyUp(new KeyboardInfo { Key = "s", Code = "KeyS" });

		clicks.Should().Be(0);
		component.Instance.Dispose();
	}
}
