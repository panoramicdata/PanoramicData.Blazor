using AwesomeAssertions;
using Bunit;
using Microsoft.AspNetCore.Components;
using Microsoft.Extensions.DependencyInjection;
using PanoramicData.Blazor.Extensions;
using PanoramicData.Blazor.Interfaces;
using PanoramicData.Blazor.Models;

namespace PanoramicData.Blazor.Test.Components;

/// <summary>
/// Tests that <see cref="PDButton"/> renders its content, honours its enabled state, raises its callbacks,
/// runs async operations and responds to its shortcut key.
/// </summary>
public partial class PDButtonTests : BunitContext
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
}
