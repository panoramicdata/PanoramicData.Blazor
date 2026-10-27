using System.Text;
using AwesomeAssertions;
using Bunit;
using PanoramicData.Blazor.Models;
using PanoramicData.Blazor.Services;

namespace PanoramicData.Blazor.Test;

/// <summary>
/// Tests the optional author image shown beside the sender's name.
/// </summary>
public partial class PDMessageTests
{
	private const string Svg = "<svg xmlns=\"http://www.w3.org/2000/svg\" viewBox=\"0 0 1 1\"><script>alert(1)</script></svg>";

	/// <summary>
	/// Verifies that a message without an author image renders its name exactly as before, in every layout.
	/// </summary>
	[Theory]
	[InlineData(true, true)]
	[InlineData(true, false)]
	[InlineData(false, true)]
	[InlineData(false, false)]
	public void NoAuthorImage_RendersTheNameAsBefore(bool fullWidth, bool fromUser)
	{
		var component = RenderMessage(Message(fromUser), p => p.Add(x => x.UseFullWidthMessages, fullWidth));

		component.FindAll(".pdchat-author, .pdchat-author-image, img").Should().BeEmpty();
		var name = component.Find(".pdchat-username");
		name.TextContent.Should().Be(fromUser ? "Ann" : "Bot");
		name.ParentElement!.ClassList.Should().NotContain("pdchat-author");
	}

	/// <summary>
	/// Verifies that an image URL is shown beside the name, in every layout.
	/// </summary>
	[Theory]
	[InlineData(true, true)]
	[InlineData(true, false)]
	[InlineData(false, true)]
	[InlineData(false, false)]
	public void AuthorImageUrl_IsShownBesideTheName(bool fullWidth, bool fromUser)
	{
		var message = Message(fromUser);
		message.AuthorImageUrl = "/faces/bot.png";

		var component = RenderMessage(message, p => p.Add(x => x.UseFullWidthMessages, fullWidth));

		var author = component.Find(".pdchat-author");
		author.QuerySelector("img.pdchat-author-image")!.GetAttribute("src").Should().Be("/faces/bot.png");
		author.QuerySelector(".pdchat-username")!.TextContent.Should().Be(fromUser ? "Ann" : "Bot");
	}

	/// <summary>
	/// Verifies that an SVG is shown as an image data URI, never as markup, and wins over a URL.
	/// </summary>
	[Fact]
	public void AuthorImageSvg_IsAnImageDataUri_NotMarkup_AndWinsOverTheUrl()
	{
		var message = Message(fromUser: false);
		message.AuthorImageSvg = Svg;
		message.AuthorImageUrl = "/faces/bot.png";

		var component = RenderMessage(message);

		var source = component.Find("img.pdchat-author-image").GetAttribute("src")!;
		const string prefix = "data:image/svg+xml;base64,";
		source.Should().StartWith(prefix);
		Encoding.UTF8.GetString(Convert.FromBase64String(source[prefix.Length..])).Should().Be(Svg);
		component.FindAll("svg, script").Should().BeEmpty();
	}

	/// <summary>
	/// Verifies that the image belongs with the name: hiding names hides it too.
	/// </summary>
	[Fact]
	public void AuthorImage_IsHidden_WhenNamesAreHidden()
	{
		var message = Message(fromUser: false);
		message.AuthorImageUrl = "/faces/bot.png";

		var component = RenderMessage(message, p => p.Add(x => x.ShowMessageUserName, false));

		component.FindAll(".pdchat-author-image").Should().BeEmpty();
	}

	/// <summary>
	/// Verifies that a blank image is treated as no image.
	/// </summary>
	[Fact]
	public void BlankAuthorImage_IsNoImage()
	{
		var message = Message(fromUser: false);
		message.AuthorImageSvg = " ";
		message.AuthorImageUrl = string.Empty;

		var component = RenderMessage(message);

		component.FindAll(".pdchat-author-image").Should().BeEmpty();
	}

	/// <summary>
	/// Verifies that a sender's image is shown when the message has none of its own, in every layout.
	/// </summary>
	[Theory]
	[InlineData(true)]
	[InlineData(false)]
	public void SenderImageUrl_IsShown_WhenTheMessageHasNoImage(bool fullWidth)
	{
		var message = Message(fromUser: true);
		var withSenderImage = new ChatMessage
		{
			Id = message.Id,
			Sender = new ChatMessageSender { Name = "Ann", IsUser = true, ImageUrl = "/faces/ann.png" },
			Message = message.Message,
			Type = message.Type,
			Timestamp = message.Timestamp
		};

		var component = RenderMessage(withSenderImage, p => p.Add(x => x.UseFullWidthMessages, fullWidth));

		component.Find("img.pdchat-author-image").GetAttribute("src").Should().Be("/faces/ann.png");
	}

	/// <summary>
	/// Verifies that the message's own image wins over its sender's.
	/// </summary>
	[Fact]
	public void MessageImage_WinsOverTheSenderImage()
	{
		var message = new ChatMessage
		{
			Id = Guid.NewGuid(),
			Sender = new ChatMessageSender { Name = "Ann", IsUser = true, ImageUrl = "/faces/ann.png" },
			Message = "Hello there",
			Type = MessageType.Normal,
			AuthorImageUrl = "/faces/message.png"
		};

		var component = RenderMessage(message);

		component.Find("img.pdchat-author-image").GetAttribute("src").Should().Be("/faces/message.png");
	}
	/// <summary>
	/// Verifies that the demo bot's face is a well-formed SVG document.
	/// </summary>
	[Fact]
	public void DumbBotFace_IsAnSvgDocument()
		=> System.Xml.Linq.XDocument.Parse(DumbChatService.DumbBotFaceSvg).Root!.Name.LocalName.Should().Be("svg");
}
