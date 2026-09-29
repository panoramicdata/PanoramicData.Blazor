using AwesomeAssertions;
using Bunit;
using Microsoft.AspNetCore.Components.Web;

namespace PanoramicData.Blazor.Test;

/// <summary>
/// Tests that <see cref="PDClipboard"/> writes its text to the clipboard when clicked and briefly shows
/// the "copied" icon before returning to the "ready" icon.
/// </summary>
public class PDClipboardTests : BunitContext
{
	/// <summary>Sets up the rendering context.</summary>
	public PDClipboardTests() => JSInterop.Mode = JSRuntimeMode.Loose;

	/// <summary>
	/// Verifies the default markup: the ready icon, the default tooltip and no button text.
	/// </summary>
	[Fact]
	public void Renders_the_ready_icon_and_default_tooltip()
	{
		var component = Render<PDClipboard>();

		component.Find("a").GetAttribute("title").Should().Be("Copy to clipboard");
		var icon = component.Find("i.pdClipboard-button");
		icon.ClassList.Should().Contain(["far", "fa-copy"]);
		component.FindAll(".pdClipboard-button-text").Should().BeEmpty();
	}

	/// <summary>
	/// Verifies that button text, its CSS class, the general CSS class and the tooltip are all applied.
	/// </summary>
	[Fact]
	public void Renders_button_text_and_custom_classes()
	{
		var component = Render<PDClipboard>(parameters => parameters
			.Add(p => p.ButtonText, "Copy id")
			.Add(p => p.ButtonTextCssClass, "text-muted")
			.Add(p => p.CssClass, "extra")
			.Add(p => p.ReadyToCopyCssClass, "ready-icon")
			.Add(p => p.ToolTip, "Copy the id"));

		component.Find("a").GetAttribute("title").Should().Be("Copy the id");
		component.Find("i.pdClipboard-button").ClassList.Should().Contain(["ready-icon", "extra"]);
		var text = component.Find(".pdClipboard-button-text");
		text.TextContent.Should().Be("Copy id");
		text.ClassList.Should().Contain("text-muted");
	}

	/// <summary>
	/// Verifies that whitespace-only button text is treated as no text.
	/// </summary>
	[Fact]
	public void Whitespace_button_text_is_not_rendered()
	{
		var component = Render<PDClipboard>(parameters => parameters
			.Add(p => p.ButtonText, "   "));

		component.FindAll(".pdClipboard-button-text").Should().BeEmpty();
	}

	/// <summary>
	/// Verifies that clicking writes the text to the clipboard, shows the copied icon, then reverts.
	/// </summary>
	[Fact]
	public async Task Clicking_copies_the_text_and_shows_then_clears_the_copied_icon()
	{
		var component = Render<PDClipboard>(parameters => parameters
			.Add(p => p.Text, "secret-value")
			.Add(p => p.TextCopiedCssClass, "copied-icon"));

		var iconStates = new List<string>();
		component.OnMarkupUpdated += (_, _) => iconStates.Add(component.Find("i.pdClipboard-button").ClassName ?? string.Empty);

		// Awaited: the handler shows the copied icon, holds it for a second, then reverts.
		await component.InvokeAsync(() => component.Find("a").ClickAsync(new MouseEventArgs()));

		var invocation = JSInterop.VerifyInvoke("navigator.clipboard.writeText");
		invocation.Arguments.Should().ContainSingle().Which.Should().Be("secret-value");
		iconStates.Should().HaveCountGreaterThanOrEqualTo(2);
		iconStates[0].Should().Contain("copied-icon");
		iconStates[^1].Should().Contain("fa-copy").And.NotContain("copied-icon");
		component.Find("i.pdClipboard-button").ClassList.Should().Contain("fa-copy").And.NotContain("copied-icon");
	}
}
