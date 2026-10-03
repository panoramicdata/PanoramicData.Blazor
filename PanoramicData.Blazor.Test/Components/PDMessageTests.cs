using AwesomeAssertions;
using Bunit;
using PanoramicData.Blazor.Models;

namespace PanoramicData.Blazor.Test;

/// <summary>
/// Tests that <see cref="PDMessage"/> renders a chat message with its metadata on the configured side, as
/// plain text or HTML, with progress while it is being composed and any inline form it carries.
/// </summary>
public partial class PDMessageTests : BunitContext
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
}
