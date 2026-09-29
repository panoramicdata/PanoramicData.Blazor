using AwesomeAssertions;
using Bunit;
using Microsoft.Extensions.DependencyInjection;
using PanoramicData.Blazor.Extensions;
using PanoramicData.Blazor.Interfaces;
using PanoramicData.Blazor.Models;
using PanoramicData.Blazor.Services;

namespace PanoramicData.Blazor.Test.Components;

/// <summary>
/// Tests that <see cref="PDLinkButton"/> renders a styled anchor, tracks its enabled state and clicks itself
/// when its shortcut key is pressed.
/// </summary>
public class PDLinkButtonTests : BunitContext
{
	/// <summary>Sets up the rendering context.</summary>
	public PDLinkButtonTests()
	{
		JSInterop.Mode = JSRuntimeMode.Loose;
		Services.AddPanoramicDataBlazor();
	}

	private GlobalEventService Events => (GlobalEventService)Services.GetRequiredService<IGlobalEventService>();

	/// <summary>The anchor carries the id, classes, url, target and tooltip it is given.</summary>
	[Fact]
	public void Parameters_AreRenderedOntoTheAnchor()
	{
		var component = Render<PDLinkButton>(parameters => parameters
			.Add(p => p.Id, "home-link")
			.Add(p => p.CssClass, "btn-primary")
			.Add(p => p.Url, "/home")
			.Add(p => p.Target, "_blank")
			.Add(p => p.ToolTip, "Go home")
			.Add(p => p.Text, "Home")
			.Add(p => p.TextCssClass, "fw-bold")
			.Add(p => p.Attributes, new Dictionary<string, object> { ["data-test"] = "x" }));

		var anchor = component.Find("a");
		anchor.Id.Should().Be("home-link");
		anchor.ClassList.Should().Contain(["pd-button", "btn", "btn-primary"]);
		anchor.GetAttribute("href").Should().Be("/home");
		anchor.GetAttribute("target").Should().Be("_blank");
		anchor.GetAttribute("title").Should().Be("Go home");
		anchor.GetAttribute("data-test").Should().Be("x");
		anchor.HasAttribute("disabled").Should().BeFalse();
		var text = anchor.QuerySelector("span.fw-bold")!;
		text.TextContent.Should().Be("Home");
	}

	/// <summary>The defaults are a same-window link to '#', and each instance gets its own id.</summary>
	[Fact]
	public void Defaults_AreASelfTargetedHashLink_WithAUniqueId()
	{
		var first = Render<PDLinkButton>();
		var second = Render<PDLinkButton>();

		first.Find("a").GetAttribute("href").Should().Be("#");
		first.Find("a").GetAttribute("target").Should().Be("_self");
		first.Find("a").Id.Should().StartWith("pdlb-");
		first.Find("a").Id.Should().NotBe(second.Find("a").Id);
	}

	/// <summary>An icon span is rendered only when an icon class is given.</summary>
	[Theory]
	[InlineData("fas fa-home", true)]
	[InlineData("", false)]
	[InlineData("  ", false)]
	public void IconCssClass_RendersAnIconOnlyWhenGiven(string iconCssClass, bool expectIcon)
	{
		var component = Render<PDLinkButton>(parameters => parameters
			.Add(p => p.IconCssClass, iconCssClass));

		component.FindAll("span.pe-1").Count.Should().Be(expectIcon ? 1 : 0);
	}

	/// <summary>The size maps onto the Bootstrap button size classes.</summary>
	[Theory]
	[InlineData(ButtonSizes.Small, "btn-sm")]
	[InlineData(ButtonSizes.Large, "btn-lg")]
	public void Size_AddsTheButtonSizeClass(ButtonSizes size, string expectedClass)
	{
		var component = Render<PDLinkButton>(parameters => parameters.Add(p => p.Size, size));

		component.Find("a").ClassList.Should().Contain(expectedClass);
	}

	/// <summary>A medium or unset size adds no size class.</summary>
	[Fact]
	public void MediumSize_AddsNoSizeClass()
	{
		var component = Render<PDLinkButton>(parameters => parameters.Add(p => p.Size, ButtonSizes.Medium));

		component.Find("a").ClassList.Should().NotContain(["btn-sm", "btn-lg"]);
	}

	/// <summary>Disable, Enable and SetEnabled toggle the disabled attribute.</summary>
	[Fact]
	public async Task EnableDisable_ToggleTheDisabledAttribute()
	{
		var component = Render<PDLinkButton>();

		await component.InvokeAsync(component.Instance.Disable);
		component.Find("a").GetAttribute("disabled").Should().Be("true");

		await component.InvokeAsync(component.Instance.Enable);
		component.Find("a").HasAttribute("disabled").Should().BeFalse();

		await component.InvokeAsync(() => component.Instance.SetEnabled(false));
		component.Find("a").GetAttribute("disabled").Should().Be("true");
	}

	/// <summary>Text marked with a double ampersand underlines the shortcut character.</summary>
	[Fact]
	public void TextWithDoubleAmpersand_UnderlinesTheShortcutCharacter()
	{
		var component = Render<PDLinkButton>(parameters => parameters.Add(p => p.Text, "&&Save"));

		component.Find("a u").TextContent.Should().Be("S");
	}

	/// <summary>A shortcut key is appended to the tooltip and registered with the global event service.</summary>
	[Fact]
	public void ShortcutKey_IsAppendedToTheTooltip_AndRegistered()
	{
		var shortcut = ShortcutKey.Create("ctrl-s");

		var component = Render<PDLinkButton>(parameters => parameters
			.Add(p => p.ToolTip, "Save")
			.Add(p => p.ShortcutKey, shortcut));

		component.Find("a").GetAttribute("title").Should().Be($"Save ({shortcut})");
		Events.GetRegisteredShortcuts().Should().ContainSingle().Which.ToString().Should().Be(shortcut.ToString());
	}

	/// <summary>Pressing the shortcut clicks the button through the common JS module.</summary>
	[Fact]
	public void PressingTheShortcut_ClicksTheButton()
	{
		var module = JSInterop.SetupModule(JSInteropVersionHelper.CommonJsUrl);
		module.SetupVoid("click", "save-btn").SetVoidResult();
		Render<PDLinkButton>(parameters => parameters
			.Add(p => p.Id, "save-btn")
			.Add(p => p.ShortcutKey, ShortcutKey.Create("ctrl-s")));

		Events.KeyUp(new KeyboardInfo { Key = "s", Code = "KeyS", CtrlKey = true });

		module.VerifyInvoke("click").Arguments.Should().Equal("save-btn");
	}

	/// <summary>A key that does not match the shortcut does not click the button.</summary>
	[Fact]
	public void PressingAnotherKey_DoesNotClickTheButton()
	{
		var module = JSInterop.SetupModule(JSInteropVersionHelper.CommonJsUrl);
		module.Mode = JSRuntimeMode.Loose;
		Render<PDLinkButton>(parameters => parameters
			.Add(p => p.Id, "save-btn")
			.Add(p => p.ShortcutKey, ShortcutKey.Create("ctrl-s")));

		Events.KeyUp(new KeyboardInfo { Key = "s", Code = "KeyS", CtrlKey = false });
		Events.KeyUp(new KeyboardInfo { Key = "x", Code = "KeyX", CtrlKey = true });

		module.Invocations["click"].Should().BeEmpty();
	}

	/// <summary>Without a shortcut key, key presses never click the button.</summary>
	[Fact]
	public void NoShortcutKey_KeyPressesNeverClick()
	{
		var module = JSInterop.SetupModule(JSInteropVersionHelper.CommonJsUrl);
		module.Mode = JSRuntimeMode.Loose;
		Render<PDLinkButton>();

		Events.KeyUp(new KeyboardInfo { Key = "s", Code = "KeyS", CtrlKey = true });

		module.Invocations["click"].Should().BeEmpty();
		Events.GetRegisteredShortcuts().Should().BeEmpty();
	}

	/// <summary>Disposing the button unregisters its shortcut and stops it listening.</summary>
	[Fact]
	public async Task Dispose_UnregistersTheShortcut_AndStopsListening()
	{
		var module = JSInterop.SetupModule(JSInteropVersionHelper.CommonJsUrl);
		module.Mode = JSRuntimeMode.Loose;
		var component = Render<PDLinkButton>(parameters => parameters
			.Add(p => p.ShortcutKey, ShortcutKey.Create("ctrl-s")));

		await component.InvokeAsync(() => component.Instance.DisposeAsync().AsTask());

		Events.GetRegisteredShortcuts().Should().BeEmpty();
		Events.KeyUp(new KeyboardInfo { Key = "s", Code = "KeyS", CtrlKey = true });
		module.Invocations["click"].Should().BeEmpty();
	}

	/// <summary>Calling ClickAsync when the JS module failed to load does nothing rather than throwing.</summary>
	[Fact]
	public async Task ClickAsync_WithoutAModule_DoesNothing()
	{
		JSInterop.Mode = JSRuntimeMode.Strict;
		var component = Render<PDLinkButton>();

		var act = () => component.InvokeAsync(component.Instance.ClickAsync);

		await act.Should().NotThrowAsync();
	}
}
