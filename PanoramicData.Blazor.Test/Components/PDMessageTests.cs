using System.Globalization;
using AwesomeAssertions;
using Bunit;
using PanoramicData.Blazor.Models;

namespace PanoramicData.Blazor.Test;

/// <summary>
/// Tests that <see cref="PDMessage"/> renders a chat message with its metadata on the configured side, as
/// plain text or HTML, with progress while it is being composed and any inline form it carries.
/// </summary>
public class PDMessageTests : BunitContext
{
	private static readonly DateTimeOffset Sent = new(2026, 9, 28, 14, 5, 6, TimeSpan.Zero);

	/// <summary>Sets up the rendering context.</summary>
	public PDMessageTests() => JSInterop.Mode = JSRuntimeMode.Loose;

	private static ChatMessage Message(bool fromUser, MessageType type = MessageType.Normal, string text = "Hello there") => new()
	{
		Id = Guid.NewGuid(),
		Sender = new ChatMessageSender { Name = fromUser ? "Ann" : "Bot", IsUser = fromUser },
		Message = text,
		Type = type,
		Timestamp = Sent
	};

	private IRenderedComponent<PDMessage> RenderMessage(ChatMessage message, Action<ComponentParameterCollectionBuilder<PDMessage>>? configure = null)
		=> Render<PDMessage>(parameters =>
		{
			parameters.Add(p => p.Message, message);
			configure?.Invoke(parameters);
		});

	private static List<string> TopLevelParts(IRenderedComponent<PDMessage> component)
		=> [.. component.Find("div.pdchat-message").Children.Select(c => c.ClassName ?? string.Empty)];

	/// <summary>
	/// Verifies where each display mode puts the metadata for user and other senders, in the class and in
	/// the order of the parts.
	/// </summary>
	[Theory]
	[InlineData(MessageMetadataDisplayMode.UserOnlyOnRightOthersOnLeft, true, true)]
	[InlineData(MessageMetadataDisplayMode.UserOnlyOnRightOthersOnLeft, false, false)]
	[InlineData(MessageMetadataDisplayMode.UserOnlyOnLeftOthersOnRight, true, false)]
	[InlineData(MessageMetadataDisplayMode.UserOnlyOnLeftOthersOnRight, false, true)]
	[InlineData(MessageMetadataDisplayMode.AlwaysOnLeft, true, false)]
	[InlineData(MessageMetadataDisplayMode.AlwaysOnRight, false, true)]
	[InlineData((MessageMetadataDisplayMode)99, true, true)]
	public void DisplayMode_PlacesTheMetadata(MessageMetadataDisplayMode mode, bool fromUser, bool onRight)
	{
		var component = RenderMessage(Message(fromUser), p => p.Add(x => x.MessageMetadataDisplayMode, mode));

		var root = component.Find("div.pdchat-message");
		root.ClassList.Should().Contain([onRight ? "meta-on-right" : "meta-on-left", fromUser ? "user" : "bot", "full-width"]);
		TopLevelParts(component).Should().Equal(onRight ? ["pdchat-content", "pdchat-meta"] : ["pdchat-meta", "pdchat-content"]);
	}

	/// <summary>
	/// Verifies that the metadata shows the icon, sender name and formatted local timestamp.
	/// </summary>
	[Fact]
	public void Metadata_ShowsIconNameAndTimestamp()
	{
		var component = RenderMessage(Message(false), p => p
			.Add(x => x.UserIconSelector, m => m.Sender.Name == "Bot" ? "B" : null)
			.Add(x => x.MessageTimestampFormat, "HH:mm"));

		component.Find(".pdchat-icon").TextContent.Should().Be("B");
		component.Find(".pdchat-username").TextContent.Should().Be("Bot");
		component.Find(".pdchat-timestamp").TextContent.Should().Be(Sent.ToLocalTime().ToString("HH:mm", CultureInfo.CurrentCulture));
	}

	/// <summary>
	/// Verifies that with every piece of metadata switched off, no metadata area is rendered in either layout.
	/// </summary>
	[Theory]
	[InlineData(true, true)]
	[InlineData(true, false)]
	[InlineData(false, true)]
	public void MetadataOff_RendersNoMetadataArea(bool fullWidth, bool fromUser)
	{
		var component = RenderMessage(Message(fromUser), p => p
			.Add(x => x.UseFullWidthMessages, fullWidth)
			.Add(x => x.ShowMessageUserIcon, false)
			.Add(x => x.ShowMessageUserName, false)
			.Add(x => x.ShowMessageTimestamp, false));

		component.FindAll(".pdchat-meta, .pdchat-bubble-header").Should().BeEmpty();
		component.Find(".pdchat-text").TextContent.Should().Contain("Hello there");
	}

	/// <summary>
	/// Verifies that individual metadata pieces can be switched off in each layout, and the default icon is used
	/// when no selector gives one.
	/// </summary>
	[Theory]
	[InlineData(true, true)]
	[InlineData(true, false)]
	[InlineData(false, false)]
	public void PartialMetadata_ShowsOnlyWhatIsSwitchedOn(bool fullWidth, bool fromUser)
	{
		var component = RenderMessage(Message(fromUser), p => p
			.Add(x => x.UseFullWidthMessages, fullWidth)
			.Add(x => x.ShowMessageUserName, false)
			.Add(x => x.ShowMessageTimestamp, false));

		component.FindAll(".pdchat-username, .pdchat-timestamp").Should().BeEmpty();
		component.Find(".pdchat-icon").TextContent.Should().Be("\U0001F464");
	}

	/// <summary>
	/// Verifies that a plain title and message are rendered as text in every layout, never as markup.
	/// </summary>
	[Theory]
	[InlineData(true, true)]
	[InlineData(true, false)]
	[InlineData(false, true)]
	[InlineData(false, false)]
	public void PlainTitleAndMessage_AreRenderedAsText(bool fullWidth, bool fromUser)
	{
		var message = Message(fromUser, text: "<b>bold</b>");
		message.Title = "<i>Title</i>";

		var component = RenderMessage(message, p => p.Add(x => x.UseFullWidthMessages, fullWidth));

		component.Find(".pdchat-title-bar").TextContent.Should().Be("<i>Title</i>");
		component.Find(".pdchat-text span").TextContent.Should().Be("<b>bold</b>");
		component.FindAll(".pdchat-text b, .pdchat-title-bar i").Should().BeEmpty();
	}

	/// <summary>
	/// Verifies that an HTML title and message are rendered as markup in every layout, and that bubbles are
	/// aligned by sender.
	/// </summary>
	[Theory]
	[InlineData(true, true, null)]
	[InlineData(true, false, null)]
	[InlineData(false, true, "right")]
	[InlineData(false, false, "left")]
	public void HtmlTitleAndMessage_AreRenderedAsMarkup(bool fullWidth, bool fromUser, string? bubbleSide)
	{
		var message = Message(fromUser, text: "<b>bold</b>");
		message.Title = "<i>Title</i>";
		message.IsTitleHtml = true;
		message.IsMessageHtml = true;

		var component = RenderMessage(message, p => p.Add(x => x.UseFullWidthMessages, fullWidth));

		component.Find(".pdchat-title-bar i").TextContent.Should().Be("Title");
		component.Find(".pdchat-text b").TextContent.Should().Be("bold");
		if (bubbleSide is not null)
		{
			component.Find(".pdchat-text span").ClassList.Should().Contain(bubbleSide);
			component.Find("div.pdchat-message").ClassList.Should().Contain("bubble");
		}
	}

	/// <summary>
	/// Verifies that the bubble layout puts the metadata in a header, and that the message type becomes a
	/// priority class.
	/// </summary>
	[Fact]
	public void BubbleLayout_HasAHeader_AndAPriorityClass()
	{
		var component = RenderMessage(Message(true, MessageType.Warning), p => p.Add(x => x.UseFullWidthMessages, false));

		component.Find(".pdchat-bubble-header .pdchat-username").TextContent.Should().Be("Ann");
		component.Find("div.pdchat-message").ClassList.Should().Contain("priority-warning");
		component.Find(".pdchat-text span").ClassList.Should().Contain("right");
	}

	/// <summary>
	/// Verifies that a message being composed shows its progress steps, thoughts and the partial reply,
	/// naming what is still being written, in every layout.
	/// </summary>
	[Theory]
	[InlineData(true, true)]
	[InlineData(true, false)]
	[InlineData(false, false)]
	public void TypingMessage_ShowsProgress(bool fullWidth, bool fromUser)
	{
		var message = Message(fromUser, MessageType.Typing, "ignored while typing");
		message.ProgressSteps = ["Searching", "Reading"];
		message.Thoughts = [new ChatThought("Plan", "Look it up")];
		message.PartialMessage = "Findings:\n\n```json\n{\"ssid\": \"Panoram";

		var component = RenderMessage(message, p => p.Add(x => x.UseFullWidthMessages, fullWidth));

		component.FindAll(".pdchat-progress-steps li").Select(li => li.TextContent).Should().Equal("Searching", "Reading");
		component.Find(".pdchat-thought summary").TextContent.Should().Be("Plan");
		component.Find(".pdchat-thought-text").TextContent.Should().Be("Look it up");
		component.Find(".pdchat-partial").TextContent.Should().Contain("Findings:");
		component.Find(".pdchat-partial-writing").TextContent.Should().Contain("writing code block");
		component.Markup.Should().NotContain("ignored while typing");
	}

	/// <summary>
	/// Verifies that a message being composed with nothing to report shows only the typing indicator.
	/// </summary>
	[Fact]
	public void TypingMessage_WithNoProgress_ShowsOnlyTheIndicator()
	{
		var component = RenderMessage(Message(false, MessageType.Typing));

		component.Find(".pdchat-progress .typing-ellipsis").Should().NotBeNull();
		component.FindAll(".pdchat-progress-steps, .pdchat-thought, .pdchat-partial, .pdchat-partial-writing").Should().BeEmpty();
	}

	/// <summary>
	/// Verifies that a message carrying a form renders it, and that dismissing and submitting it are reported
	/// to the enclosing chat's form context.
	/// </summary>
	[Fact]
	public async Task Form_IsRendered_AndItsOutcomeReachesTheContext()
	{
		Guid? dismissed = null;
		ChatFormSubmission? submitted = null;
		var message = MessageWithForm();
		var context = new ChatFormContext
		{
			OnDismissed = id => { dismissed = id; return Task.CompletedTask; },
			OnSubmitted = s => { submitted = s; return Task.CompletedTask; }
		};
		var component = RenderMessage(message, p => p.AddCascadingValue(context));
		var submission = new ChatFormSubmission { FormId = message.Form!.Id, Answers = [] };

		await component.InvokeAsync(() => component.FindComponent<PDFormMessage>().Instance.OnSubmitted.InvokeAsync(submission));
		component.Find("button.pdchat-form-dismiss").Click();

		submitted.Should().BeSameAs(submission);
		dismissed.Should().Be(message.Form.Id);
	}

	/// <summary>
	/// Verifies that outside a chat the form still renders and its outcome goes nowhere without error.
	/// </summary>
	[Fact]
	public async Task Form_WithoutAContext_StillRenders()
	{
		var message = MessageWithForm();
		var component = RenderMessage(message, p => p.Add(x => x.UseFullWidthMessages, false));

		component.FindAll(".pdchat-form").Should().ContainSingle();
		await component.InvokeAsync(() => component.FindComponent<PDFormMessage>().Instance.OnSubmitted.InvokeAsync(
			new ChatFormSubmission { FormId = message.Form!.Id, Answers = [] }));
		component.Find("button.pdchat-form-dismiss").Click();

		component.FindAll(".pdchat-form").Should().ContainSingle();
	}

	private static ChatMessage MessageWithForm()
	{
		var message = Message(false, MessageType.Form, "A question for you");
		message.Form = new ChatForm
		{
			Id = Guid.NewGuid(),
			Questions =
			[
				new ChatFormQuestion { Id = "q1", Header = "Colour", Question = "Favourite colour?", Kind = ChatFormAnswerKind.Text }
			]
		};
		return message;
	}
}
