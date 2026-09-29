using AwesomeAssertions;
using Bunit;
using PanoramicData.Blazor.Extensions;
using PanoramicData.Blazor.Models;

namespace PanoramicData.Blazor.Test.Components;

/// <summary>
/// Tests that <see cref="PDToolbarLinkButton"/> wraps a <see cref="PDLinkButton"/> in a toolbar item and
/// passes its parameters through.
/// </summary>
public class PDToolbarLinkButtonTests : BunitContext
{
	/// <summary>Sets up the rendering context.</summary>
	public PDToolbarLinkButtonTests()
	{
		JSInterop.Mode = JSRuntimeMode.Loose;
		Services.AddPanoramicDataBlazor();
	}

	/// <summary>The inner link button receives the url, target, text, icon and CSS classes.</summary>
	[Fact]
	public void Parameters_ArePassedToTheLinkButton()
	{
		var component = Render<PDToolbarLinkButton>(parameters => parameters
			.Add(p => p.Url, "/docs")
			.Add(p => p.Target, "_blank")
			.Add(p => p.Text, "Docs")
			.Add(p => p.IconCssClass, "fas fa-book")
			.Add(p => p.CssClass, "btn-info")
			.Add(p => p.ItemCssClass, "item-extra")
			.Add(p => p.Size, ButtonSizes.Small));

		component.Find("div.pdtoolbaritem").ClassList.Should().Contain("item-extra");
		var anchor = component.Find("a");
		anchor.GetAttribute("href").Should().Be("/docs");
		anchor.GetAttribute("target").Should().Be("_blank");
		anchor.ClassList.Should().Contain(["pdtoolbarlinkbutton", "btn-info", "btn-sm"]);
		anchor.QuerySelector("span.fas.fa-book").Should().NotBeNull();
		anchor.TextContent.Trim().Should().Be("Docs");
	}

	/// <summary>The tooltip falls back to the text when no tooltip is given.</summary>
	[Fact]
	public void Tooltip_FallsBackToTheText()
	{
		var component = Render<PDToolbarLinkButton>(parameters => parameters.Add(p => p.Text, "Docs"));

		component.Find("a").GetAttribute("title").Should().Be("Docs");
	}

	/// <summary>An explicit tooltip is used in preference to the text.</summary>
	[Fact]
	public void Tooltip_WhenGiven_IsUsed()
	{
		var component = Render<PDToolbarLinkButton>(parameters => parameters
			.Add(p => p.Text, "Docs")
			.Add(p => p.ToolTip, "Open the documentation"));

		component.Find("a").GetAttribute("title").Should().Be("Open the documentation");
	}

	/// <summary>A key gives the link a toolbar id; without one no toolbar id is set.</summary>
	[Fact]
	public void Key_SetsTheToolbarId()
	{
		var keyed = Render<PDToolbarLinkButton>(parameters => parameters.Add(p => p.Key, "docs"));
		var unkeyed = Render<PDToolbarLinkButton>();

		keyed.Find("a").GetAttribute("Id").Should().Be("pd-tbr-btn-docs");
		unkeyed.Find("a").Id.Should().StartWith("pdlb-");
	}

	/// <summary>Hidden and shifted-right states are shown by classes on the toolbar item.</summary>
	[Fact]
	public void VisibilityAndShiftRight_AreShownAsClasses()
	{
		var component = Render<PDToolbarLinkButton>(parameters => parameters
			.Add(p => p.IsVisible, false)
			.Add(p => p.ShiftRight, true));

		component.Find("div.pdtoolbaritem").ClassList.Should().Contain(["pd-hidden", "align-right"]);
	}

	/// <summary>A visible, unshifted item has neither class.</summary>
	[Fact]
	public void DefaultItem_IsVisibleAndNotShifted()
	{
		var component = Render<PDToolbarLinkButton>();

		component.Find("div.pdtoolbaritem").ClassList.Should().NotContain(["pd-hidden", "align-right"]);
		component.Find("a").ClassList.Should().Contain("btn-secondary");
	}

	/// <summary>Disable, Enable and SetEnabled change the inner link's disabled state.</summary>
	[Fact]
	public async Task EnableDisable_ChangeTheLinkState()
	{
		var component = Render<PDToolbarLinkButton>();

		await component.InvokeAsync(component.Instance.Disable);
		component.Instance.IsEnabled.Should().BeFalse();
		component.Find("a").GetAttribute("disabled").Should().Be("true");

		await component.InvokeAsync(component.Instance.Enable);
		component.Instance.IsEnabled.Should().BeTrue();
		component.Find("a").HasAttribute("disabled").Should().BeFalse();

		await component.InvokeAsync(() => component.Instance.SetEnabled(false));
		component.Find("a").GetAttribute("disabled").Should().Be("true");
	}

	/// <summary>A disabled item renders its link disabled from the start.</summary>
	[Fact]
	public void IsEnabledFalse_RendersADisabledLink()
	{
		var component = Render<PDToolbarLinkButton>(parameters => parameters.Add(p => p.IsEnabled, false));

		component.Find("a").GetAttribute("disabled").Should().Be("true");
	}
}
