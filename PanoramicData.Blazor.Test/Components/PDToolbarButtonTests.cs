using AwesomeAssertions;
using Bunit;
using Microsoft.AspNetCore.Components.Web;
using PanoramicData.Blazor.Arguments;
using PanoramicData.Blazor.Extensions;
using PanoramicData.Blazor.Models;

namespace PanoramicData.Blazor.Test.Components;

/// <summary>
/// Tests for <see cref="PDToolbarButton"/>: how its parameters reach the rendered button and how clicks are reported.
/// </summary>
public class PDToolbarButtonTests : BunitContext
{
	/// <summary>Sets up the rendering context.</summary>
	public PDToolbarButtonTests()
	{
		JSInterop.Mode = JSRuntimeMode.Loose;
		Services.AddPanoramicDataBlazor();
	}

	/// <summary>A button with default parameters is visible, enabled and carries the default secondary class.</summary>
	[Fact]
	public void Defaults_RenderAVisibleEnabledSecondaryButton()
	{
		var cut = Render<PDToolbarButton>(p => p.Add(x => x.Text, "Save"));

		var item = cut.Find("div.pdtoolbaritem");
		item.ClassList.Should().NotContain("pd-hidden").And.NotContain("align-right");
		var button = cut.Find("button");
		button.ClassList.Should().Contain("pdtoolbarbutton").And.Contain("btn-secondary");
		button.HasAttribute("disabled").Should().BeFalse();
		button.TextContent.Should().Contain("Save");
	}

	/// <summary>Visibility, right shift and item CSS parameters are applied to the toolbar item container.</summary>
	[Fact]
	public void HiddenShiftedButton_AppliesItemClasses()
	{
		var cut = Render<PDToolbarButton>(p => p
			.Add(x => x.IsVisible, false)
			.Add(x => x.ShiftRight, true)
			.Add(x => x.ItemCssClass, "extra-item"));

		cut.Find("div.pdtoolbaritem").ClassList.Should().Contain("pd-hidden").And.Contain("align-right").And.Contain("extra-item");
	}

	/// <summary>With no explicit tooltip the button text is used as the title.</summary>
	[Fact]
	public void NoToolTip_UsesTextAsTitle()
	{
		var cut = Render<PDToolbarButton>(p => p.Add(x => x.Text, "Refresh"));

		cut.Find("button").GetAttribute("title").Should().Be("Refresh");
	}

	/// <summary>An explicit tooltip takes precedence over the text.</summary>
	[Fact]
	public void ExplicitToolTip_IsUsedAsTitle()
	{
		var cut = Render<PDToolbarButton>(p => p
			.Add(x => x.Text, "Refresh")
			.Add(x => x.ToolTip, "Reload everything"));

		cut.Find("button").GetAttribute("title").Should().Be("Reload everything");
	}

	/// <summary>A key produces a predictable element id.</summary>
	[Fact]
	public void Key_SetsTheButtonId()
	{
		var cut = Render<PDToolbarButton>(p => p.Add(x => x.Key, "save"));

		cut.Find("button").GetAttribute("id").Should().Be("pd-tbr-btn-save");
	}

	/// <summary>Without a key no id attribute is emitted from the attributes dictionary.</summary>
	[Fact]
	public void NoKey_DoesNotEmitAnId()
	{
		var cut = Render<PDToolbarButton>(p => p.Add(x => x.Text, "Save"));

		cut.Find("button").GetAttribute("id").Should().BeNullOrEmpty();
	}

	/// <summary>Clicking raises Click with the button key and the mouse arguments.</summary>
	[Fact]
	public void Click_RaisesClickWithKey()
	{
		KeyedEventArgs<MouseEventArgs>? raised = null;
		var cut = Render<PDToolbarButton>(p => p
			.Add(x => x.Key, "save")
			.Add(x => x.Click, (KeyedEventArgs<MouseEventArgs> a) => raised = a));

		cut.Find("button").Click(new MouseEventArgs { CtrlKey = true });

		raised.Should().NotBeNull();
		raised!.Key.Should().Be("save");
		raised.Args.CtrlKey.Should().BeTrue();
	}

	/// <summary>A URL renders an anchor with the target instead of a button.</summary>
	[Fact]
	public void Url_RendersAnAnchor()
	{
		var cut = Render<PDToolbarButton>(p => p
			.Add(x => x.Url, "https://example.com/")
			.Add(x => x.Target, "_blank"));

		var anchor = cut.Find("a");
		anchor.GetAttribute("href").Should().Be("https://example.com/");
		anchor.GetAttribute("target").Should().Be("_blank");
		cut.FindAll("button").Should().BeEmpty();
	}

	/// <summary>Disable, Enable and SetEnabled change the rendered disabled state.</summary>
	[Fact]
	public async Task EnableDisableMethods_ToggleTheDisabledAttribute()
	{
		var cut = Render<PDToolbarButton>(p => p.Add(x => x.Text, "Save"));

		await cut.InvokeAsync(cut.Instance.Disable);
		cut.Find("button").HasAttribute("disabled").Should().BeTrue();
		cut.Instance.IsEnabled.Should().BeFalse();

		await cut.InvokeAsync(cut.Instance.Enable);
		cut.Find("button").HasAttribute("disabled").Should().BeFalse();

		await cut.InvokeAsync(() => cut.Instance.SetEnabled(false));
		cut.Find("button").HasAttribute("disabled").Should().BeTrue();
		await cut.InvokeAsync(() => cut.Instance.SetEnabled(true));
		cut.Instance.IsEnabled.Should().BeTrue();
	}

	/// <summary>An operation is run on click in place of the Click callback.</summary>
	[Fact]
	public void Operation_IsInvokedOnClick()
	{
		var operationRuns = 0;
		var clicks = 0;
		var cut = Render<PDToolbarButton>(p => p
			.Add(x => x.Operation, _ => { operationRuns++; return Task.CompletedTask; })
			.Add(x => x.Click, (KeyedEventArgs<MouseEventArgs> _) => clicks++));

		cut.Find("button").Click();

		operationRuns.Should().Be(1);
		clicks.Should().Be(0);
	}

	/// <summary>Size, icon and text CSS are passed through to the inner button.</summary>
	[Fact]
	public void SizeAndIcon_ArePassedThrough()
	{
		var cut = Render<PDToolbarButton>(p => p
			.Add(x => x.Text, "Go")
			.Add(x => x.Size, ButtonSizes.Small)
			.Add(x => x.IconCssClass, "fas fa-play")
			.Add(x => x.TextCssClass, "txt")
			.Add(x => x.CssClass, "btn-primary"));

		var button = cut.Find("button");
		button.ClassList.Should().Contain("btn-sm").And.Contain("btn-primary").And.NotContain("btn-secondary");
		cut.Find("span.icon").ClassList.Should().Contain("fa-play");
		cut.Find("span.txt").TextContent.Should().Contain("Go");
	}
}
