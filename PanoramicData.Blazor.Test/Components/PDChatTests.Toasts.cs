using AwesomeAssertions;
using Bunit;
using Microsoft.AspNetCore.Components.Web;
using PanoramicData.Blazor.Models;

namespace PanoramicData.Blazor.Test;

/// <summary>
/// Toast notification tests for <see cref="PDChat"/>.
/// </summary>
public partial class PDChatTests
{
	/// <summary>Verifies that a toast shows the sender, title and message with the type's scheme and ARIA.</summary>
	[Theory]
	[InlineData(MessageType.Normal, "preview-normal", "status", "polite")]
	[InlineData(MessageType.Warning, "preview-warning", "status", "polite")]
	[InlineData(MessageType.Error, "preview-error", "alert", "assertive")]
	[InlineData(MessageType.Critical, "preview-critical", "alert", "assertive")]
	[InlineData(MessageType.Success, "preview-success", "status", "polite")]
	public async Task A_toast_shows_the_message_with_its_type_scheme(MessageType type, string scheme, string role, string live)
	{
		var service = Toasting();
		var component = RenderChat(service);
		var message = Message("Body", type);
		message.Title = "Heading";

		await component.InvokeAsync(() => service.Receive(message));

		var toast = component.Find(".pdchat-toast");
		toast.ClassList.Should().Contain(scheme).And.Contain("toast-enter-grow");
		toast.GetAttribute("role").Should().Be(role);
		toast.GetAttribute("aria-live").Should().Be(live);
		toast.QuerySelector(".pdchat-preview-sender")!.TextContent.Should().Be("Merlin");
		toast.QuerySelector(".pdchat-preview-title")!.TextContent.Trim().Should().Be("Heading");
		toast.QuerySelector(".pdchat-preview-content")!.TextContent.Trim().Should().Be("Body");
	}

	/// <summary>Verifies that HTML titles and messages are rendered as markup in a toast.</summary>
	[Fact]
	public async Task A_toast_renders_html_when_flagged()
	{
		var service = Toasting();
		var component = RenderChat(service);
		var message = Message("<b>bold</b>");
		message.Title = "<i>italic</i>";
		message.IsTitleHtml = true;
		message.IsMessageHtml = true;

		await component.InvokeAsync(() => service.Receive(message));

		component.Find(".pdchat-preview-title i").TextContent.Should().Be("italic");
		component.Find(".pdchat-preview-content b").TextContent.Should().Be("bold");
	}

	/// <summary>Verifies that per-message toast options override the service defaults.</summary>
	[Fact]
	public async Task Per_message_options_override_the_defaults()
	{
		var service = Toasting();
		var component = RenderChat(service);
		var message = Message("Sized");
		message.Title = "Hidden title";
		message.ToastOptions = new ChatToastOptions
		{
			EntryAnimation = PDChatToastAnimation.Slide,
			ShowTitle = false,
			AnimationDurationMs = 400,
			MinWidth = "10px",
			MaxWidth = "20px",
			MinHeight = "30px",
			MaxHeight = "40px"
		};

		await component.InvokeAsync(() => service.Receive(message));

		var toast = component.Find(".pdchat-toast");
		toast.ClassList.Should().Contain("toast-enter-slide");
		toast.GetAttribute("style").Should().Be("--pdchat-toast-anim-ms:400ms;min-width:10px;max-width:20px;min-height:30px;max-height:40px;");
		toast.QuerySelectorAll(".pdchat-preview-title").Should().BeEmpty();
	}

	/// <summary>Verifies that each entry animation maps to its class name.</summary>
	[Theory]
	[InlineData(PDChatToastAnimation.None, "toast-enter-none")]
	[InlineData(PDChatToastAnimation.Fade, "toast-enter-fade")]
	[InlineData(PDChatToastAnimation.Shrink, "toast-enter-shrink")]
	public async Task Each_entry_animation_maps_to_a_class(PDChatToastAnimation animation, string expected)
	{
		var service = Toasting();
		service.ToastEntryAnimation = animation;
		var component = RenderChat(service);

		await component.InvokeAsync(() => service.Receive(Message("Anim")));

		component.Find(".pdchat-toast").ClassList.Should().Contain(expected);
	}

	/// <summary>Verifies that the toast stack anchors to the button, or to the headless anchor with no button.</summary>
	[Theory]
	[InlineData(PDChatButtonPosition.TopLeft, PDChatButtonPosition.BottomRight, "toast-anchor-top-left")]
	[InlineData(PDChatButtonPosition.TopRight, PDChatButtonPosition.BottomRight, "toast-anchor-top-right")]
	[InlineData(PDChatButtonPosition.BottomLeft, PDChatButtonPosition.BottomRight, "toast-anchor-bottom-left")]
	[InlineData(PDChatButtonPosition.BottomRight, PDChatButtonPosition.TopLeft, "toast-anchor-bottom-right")]
	[InlineData(PDChatButtonPosition.None, PDChatButtonPosition.TopRight, "toast-anchor-top-right")]
	[InlineData(PDChatButtonPosition.None, PDChatButtonPosition.None, "toast-anchor-bottom-right")]
	public async Task The_toast_stack_anchors_to_the_button_or_the_headless_anchor(PDChatButtonPosition button, PDChatButtonPosition anchor, string expected)
	{
		var service = Toasting(button);
		service.ToastAnchor = anchor;
		var component = RenderChat(service);

		await component.InvokeAsync(() => service.Receive(Message("Where")));

		component.Find(".pdchat-toast-stack").ClassList.Should().Contain(expected);
	}

	/// <summary>Verifies that clicking a toast opens the chat and clears the stack.</summary>
	[Fact]
	public async Task Clicking_a_toast_opens_the_chat()
	{
		var service = Toasting();
		var component = RenderChat(service);
		await component.InvokeAsync(() => service.Receive(Message("Open me")));

		await component.Find(".pdchat-toast").ClickAsync(new());

		service.DockMode.Should().Be(PDChatDockMode.BottomRight);
		component.FindAll(".pdchat-toast").Should().BeEmpty();
	}

	/// <summary>Verifies that dismissing the only toast plays its own exit animation.</summary>
	/// <remarks>
	/// The exit lasts as long as the animation, after which a timer removes the toast. The animation here is
	/// a minute long so that the exit state is still on screen when it is asserted, however busy the machine.
	/// </remarks>
	[Fact]
	public async Task Dismissing_the_only_toast_plays_its_exit_animation()
	{
		var service = Toasting();
		service.ToastExitAnimation = PDChatToastAnimation.Fade;
		service.ToastAnimationDurationMs = 60_000;
		var component = RenderChat(service);
		await component.InvokeAsync(() => service.Receive(Message("Bye")));

		await component.Find(".pdchat-toast-close").ClickAsync(new());

		var toast = component.Find(".pdchat-toast");
		toast.ClassList.Should().Contain("toast-exit-fade");
		toast.GetAttribute("style").Should().StartWith("--pdchat-toast-anim-ms:60000ms;");
	}

	/// <summary>Verifies that a dismissed toast is removed once its exit animation has finished.</summary>
	[Fact]
	public async Task A_dismissed_toast_is_removed_after_its_exit_animation()
	{
		var service = Toasting();
		var component = RenderChat(service);
		await component.InvokeAsync(() => service.Receive(Message("Bye")));

		await component.Find(".pdchat-toast-close").ClickAsync(new());

		component.WaitForAssertion(() => component.FindAll(".pdchat-toast").Should().BeEmpty(), Patience);
	}

	/// <summary>
	/// Verifies that dismissing a toast while others remain de-stacks it with the fixed duration, and that
	/// dismissing it again while it is leaving changes nothing.
	/// </summary>
	/// <remarks>
	/// The de-stack lasts a fixed 250ms that no setting changes, and its removal timer can only take effect
	/// through the renderer's dispatcher. Clicking and reading the markup inside one synchronous dispatcher
	/// call therefore sees the de-stack state before that timer can possibly remove it.
	/// </remarks>
	[Fact]
	public async Task Dismissing_one_of_several_toasts_de_stacks_it()
	{
		var service = Toasting();
		var component = RenderChat(service);
		await component.InvokeAsync(() => service.Receive(Message("First")));
		await component.InvokeAsync(() => service.Receive(Message("Second")));
		string[] firstClasses = [];
		string[] secondClasses = [];
		string? firstStyle = null;

		await component.InvokeAsync(() =>
		{
			component.FindAll(".pdchat-toast-close")[0].Click();
			component.FindAll(".pdchat-toast-close")[0].Click();
			var toasts = component.FindAll(".pdchat-toast");
			firstClasses = [.. toasts[0].ClassList];
			firstStyle = toasts[0].GetAttribute("style");
			secondClasses = [.. toasts[1].ClassList];
		});

		firstClasses.Should().Contain("toast-destack");
		firstStyle.Should().StartWith("--pdchat-toast-anim-ms:250ms;");
		secondClasses.Should().Contain("toast-enter-grow").And.NotContain("toast-destack");
		component.WaitForAssertion(() => component.FindAll(".pdchat-toast").Should().ContainSingle(), Patience);
	}

	/// <summary>Verifies that the visible-toast cap dismisses the oldest toast when a new one arrives.</summary>
	/// <remarks>
	/// The animation is a minute long so the dismissed toast is still on screen, leaving, when it is asserted:
	/// the check does not depend on a removal timer firing in time on a busy machine.
	/// </remarks>
	[Fact]
	public async Task The_visible_cap_dismisses_the_oldest()
	{
		var service = Toasting();
		service.ToastMaxVisible = 1;
		service.ToastAnimationDurationMs = 60_000;
		var component = RenderChat(service);

		await component.InvokeAsync(() => service.Receive(Message("Old")));
		await component.InvokeAsync(() => service.Receive(Message("New")));

		var toasts = component.FindAll(".pdchat-toast");
		toasts.Select(t => t.QuerySelector(".pdchat-preview-content")!.TextContent.Trim()).Should().Equal("Old", "New");
		toasts[0].ClassList.Should().Contain("toast-exit-grow");
		toasts[1].ClassList.Should().Contain("toast-enter-grow");
	}

	/// <summary>Verifies that an auto-dismissing toast leaves on its own once its display time is up.</summary>
	[Fact]
	public async Task An_auto_dismissing_toast_leaves_on_its_own()
	{
		var service = Toasting();
		service.ToastAutoDismiss = true;
		service.ToastDisplayDurationSeconds = 0.05;
		var component = RenderChat(service);

		await component.InvokeAsync(() => service.Receive(Message("Brief")));

		component.WaitForAssertion(() => component.FindAll(".pdchat-toast").Should().BeEmpty(), Patience);
	}

	/// <summary>Verifies that hovering pauses a toast's countdown and leaving resumes it to dismissal.</summary>
	[Fact]
	public async Task Hovering_pauses_and_leaving_resumes_the_countdown()
	{
		var service = Toasting();
		service.ToastAutoDismiss = true;
		service.ToastDisplayDurationSeconds = 30;
		var component = RenderChat(service);
		await component.InvokeAsync(() => service.Receive(Message("Hover")));

		await component.Find(".pdchat-toast").MouseEnterAsync(new MouseEventArgs());
		await component.Find(".pdchat-toast").MouseEnterAsync(new MouseEventArgs());
		await component.Find(".pdchat-toast").MouseLeaveAsync(new MouseEventArgs());
		await component.Find(".pdchat-toast").MouseLeaveAsync(new MouseEventArgs());

		component.Find(".pdchat-toast").ClassList.Should().Contain("toast-enter-grow");
	}

	/// <summary>Verifies that toasts are not shown when toasting is disabled.</summary>
	[Fact]
	public async Task No_toasts_when_disabled()
	{
		var service = Minimised();
		service.ToastEnabled = false;
		var component = RenderChat(service);

		await component.InvokeAsync(() => service.Receive(Message("Quiet")));

		component.FindAll(".pdchat-toast").Should().BeEmpty();
	}
}
