using AwesomeAssertions;
using Bunit;
using Microsoft.AspNetCore.Components;
using PanoramicData.Blazor.Interfaces;
using PanoramicData.Blazor.Models;

namespace PanoramicData.Blazor.Test;

/// <summary>
/// The Mic off listening choice, the answering model's label in the toolbar, and images added to a message.
/// </summary>
public partial class PDChatTests
{
	private const string TinyPng = "iVBORw0KGgoAAAANSUhEUgAAAAEAAAABCAYAAAAfFcSJAAAADUlEQVR42mNk+M9QDwADhgGAWjR9awAAAABJRU5ErkJggg==";

	/// <summary>Verifies that Mic off is the first choice and is shown while the microphone is off.</summary>
	[Fact]
	public void Mic_off_is_offered_and_selected_while_the_microphone_is_off()
	{
		var component = RenderChat(VoiceService());

		var options = component.FindAll(".pdchat-voice-mode-picker option");
		options[0].GetAttribute("value").Should().Be("MicOff");
		options[0].TextContent.Should().Be("Mic off");
		options.Where(option => option.HasAttribute("selected")).Select(option => option.GetAttribute("value"))
			.Should().Equal("MicOff");
	}

	/// <summary>Verifies that choosing Mic off stops speech recognition and releases the microphone.</summary>
	[Fact]
	public async Task Choosing_Mic_off_stops_listening()
	{
		var module = SetUpVoiceModule();
		var component = await RenderListeningAsync(VoiceService());

		await component.Find(".pdchat-voice-mode-picker select").ChangeAsync(new ChangeEventArgs { Value = "MicOff" });

		module.Invocations["stop"].Should().ContainSingle();
		component.Instance.VoiceState.Should().Be(PDChatVoiceState.Off);
		component.Find(".pdchat-voice-toggle").GetAttribute("aria-pressed").Should().Be("false");
	}

	/// <summary>Verifies that Mic off does not change the remembered mode, so the ear button resumes in it.</summary>
	[Fact]
	public async Task The_button_resumes_in_the_last_mode_after_Mic_off()
	{
		var module = SetUpVoiceModule();
		var service = VoiceService();
		var component = RenderChat(service);
		await component.Find(".pdchat-voice-mode-picker select").ChangeAsync(new ChangeEventArgs { Value = "Dictate" });
		await component.Find(".pdchat-voice-mode-picker select").ChangeAsync(new ChangeEventArgs { Value = "MicOff" });

		await component.Find(".pdchat-voice-toggle").ClickAsync(new());

		((IChatService)service).VoiceMode.Should().Be(PDChatVoiceMode.Dictate);
		module.Invocations["start"].Should().HaveCount(2);
		component.Instance.VoiceState.Should().Be(PDChatVoiceState.Paused);
	}

	/// <summary>Verifies that choosing Mic off while already off starts nothing.</summary>
	[Fact]
	public async Task Choosing_Mic_off_when_off_does_nothing()
	{
		var module = SetUpVoiceModule();
		var component = RenderChat(VoiceService());

		await component.Find(".pdchat-voice-mode-picker select").ChangeAsync(new ChangeEventArgs { Value = "MicOff" });

		module.Invocations.Should().BeEmpty();
		component.Instance.VoiceState.Should().Be(PDChatVoiceState.Off);
	}

	/// <summary>Verifies that the answering model is shown in the toolbar after the agent picker, with its tooltip.</summary>
	[Fact]
	public void The_answering_model_is_labelled_after_the_agent_picker()
	{
		var service = new FakeChatService
		{
			Agents = [new("merlin", "Merlin"), new("alice", "Alice")],
			AnsweringModelName = "gpt-6-luna",
			AnsweringModelDescription = "Answered on your organisation's own AI provider.",
		};

		var component = RenderChat(service);

		var label = component.Find(".pdchat-answering-model");
		label.TextContent.Should().Be("gpt-6-luna");
		label.GetAttribute("title").Should().Be("Answered on your organisation's own AI provider.");
		var html = component.Find(".chat-input-accessories").InnerHtml;
		html.IndexOf("pdchat-answering-model", StringComparison.Ordinal)
			.Should().BeGreaterThan(html.IndexOf("pdchat-agent-picker", StringComparison.Ordinal));
	}

	/// <summary>Verifies that no label is shown when the host names no model.</summary>
	[Fact]
	public void No_model_label_is_shown_without_a_model_name()
		=> RenderChat(new FakeChatService()).FindAll(".pdchat-answering-model").Should().BeEmpty();

	/// <summary>Verifies that the image controls are absent unless the host offers images.</summary>
	[Fact]
	public void Image_controls_are_not_offered_by_default()
	{
		var component = RenderChat(new FakeChatService());

		component.FindAll(".pdchat-image-attach").Should().BeEmpty();
		component.FindAll(".pdchat-image-screenshot").Should().BeEmpty();
	}

	/// <summary>Verifies that the attach and screenshot buttons are offered when the host offers images.</summary>
	[Fact]
	public void Image_controls_are_offered_when_the_host_offers_images()
	{
		var component = RenderChat(new FakeChatService { IsImageInputOffered = true });

		component.FindAll(".pdchat-image-attach").Should().ContainSingle();
		component.FindAll(".pdchat-image-screenshot").Should().ContainSingle();
	}

	/// <summary>Verifies that added images stack, numbered in order, above the text box, each removable.</summary>
	[Fact]
	public async Task Added_images_stack_numbered_above_the_text_box_and_can_be_removed()
	{
		var component = RenderChat(new FakeChatService { IsImageInputOffered = true });

		await AddImagesAsync(component, 2);

		component.FindAll(".pdchat-image-thumb figcaption").Select(caption => caption.TextContent).Should().Equal("1", "2");
		var html = component.Find(".pdchat-panel-flex").InnerHtml;
		html.IndexOf("pdchat-image-strip", StringComparison.Ordinal).Should().BeLessThan(html.IndexOf("<textarea", StringComparison.Ordinal));
		component.FindAll(".chat-input-container .pdchat-image-strip").Should().BeEmpty("the strip is above the text box row, not beside the text box");

		await component.FindAll(".pdchat-image-remove")[0].ClickAsync(new());

		component.FindAll(".pdchat-image-thumb figcaption").Select(caption => caption.TextContent).Should().Equal("1");
	}

	/// <summary>Verifies that Send carries the images in the user's message and then empties the strip.</summary>
	[Fact]
	public async Task Sending_carries_the_images_in_the_users_message()
	{
		var service = new FakeChatService { IsImageInputOffered = true };
		var component = RenderChat(service);
		await AddImagesAsync(component, 2);
		await component.Find("textarea").InputAsync(new ChangeEventArgs { Value = "What changed?" });

		await component.Find(".chat-send").ClickAsync(new());

		var sent = service.Sent.Should().ContainSingle().Subject;
		sent.Message.Should().Be("What changed?");
		sent.Images.Should().HaveCount(2);
		sent.Images![0].ContentType.Should().Be("image/png");
		component.FindAll(".pdchat-image-thumb").Should().BeEmpty();
	}

	/// <summary>Verifies that images alone, with no text, can be sent.</summary>
	[Fact]
	public async Task Images_alone_can_be_sent()
	{
		var service = new FakeChatService { IsImageInputOffered = true };
		var component = RenderChat(service);
		await AddImagesAsync(component, 1);

		component.Find(".chat-send").HasAttribute("disabled").Should().BeFalse();
		await component.Find(".chat-send").ClickAsync(new());

		service.Sent.Should().ContainSingle().Which.Images.Should().ContainSingle();
	}

	/// <summary>Verifies that a blocked reason keeps the images, disables Send, shows the reason and sends nothing.</summary>
	[Fact]
	public async Task A_blocked_reason_disables_Send_and_is_shown()
	{
		var service = new FakeChatService { IsImageInputOffered = true, ImageInputBlockedReason = "This model cannot take images." };
		var component = RenderChat(service);
		await AddImagesAsync(component, 1);
		await component.Find("textarea").InputAsync(new ChangeEventArgs { Value = "Look" });

		component.Find(".pdchat-image-notice").TextContent.Should().Be("This model cannot take images.");
		component.Find(".chat-send").HasAttribute("disabled").Should().BeTrue();
		await component.InvokeAsync(() => component.FindComponent<PDMessages>().Instance.OnEnterPressed());

		service.Sent.Should().BeEmpty();
		component.FindAll(".pdchat-image-thumb").Should().ContainSingle();
	}

	/// <summary>Verifies that a message carries at most five images and says so.</summary>
	[Fact]
	public async Task A_sixth_image_is_refused_with_a_message()
	{
		var component = RenderChat(new FakeChatService { IsImageInputOffered = true });

		await AddImagesAsync(component, PDMessages.MaxImages + 1);

		component.FindAll(".pdchat-image-thumb").Should().HaveCount(PDMessages.MaxImages);
		component.Find(".pdchat-image-notice").TextContent.Should().Contain("at most 5 images");
	}

	/// <summary>Verifies that images in a message are rendered full width in that message.</summary>
	[Fact]
	public void A_message_with_images_renders_them()
	{
		var message = new ChatMessage
		{
			Id = Guid.NewGuid(),
			Sender = _user,
			Message = "Look at this",
			Type = MessageType.Normal,
			Images = [new ChatImage { ContentType = "image/png", Base64Data = TinyPng }],
		};

		var component = Render<PDMessage>(parameters => parameters.Add(p => p.Message, message));

		var image = component.Find(".pdchat-message-images img.pdchat-message-image");
		image.GetAttribute("src").Should().Be($"data:image/png;base64,{TinyPng}");
	}

	/// <summary>Verifies that an image sent in pieces is added once, when the last piece arrives, with its pieces joined in order.</summary>
	[Fact]
	public async Task An_image_sent_in_pieces_is_added_when_the_last_piece_arrives()
	{
		var component = RenderChat(new FakeChatService { IsImageInputOffered = true });
		var messages = component.FindComponent<PDMessages>();

		await component.InvokeAsync(() => messages.Instance.OnImageChunk("a", TinyPng[..20], false, "image/png", "big"));
		component.FindAll(".pdchat-image-thumb").Should().BeEmpty("the image is not whole yet");
		await component.InvokeAsync(() => messages.Instance.OnImageChunk("a", TinyPng[20..], true, "image/png", "big"));

		component.FindAll(".pdchat-image-thumb").Should().ContainSingle();
		component.Find(".pdchat-image-thumb img").GetAttribute("src").Should().Be($"data:image/png;base64,{TinyPng}");
	}

	/// <summary>Verifies that pieces of two images arriving interleaved are not mixed up.</summary>
	[Fact]
	public async Task Pieces_of_two_images_are_kept_apart()
	{
		var component = RenderChat(new FakeChatService { IsImageInputOffered = true });
		var messages = component.FindComponent<PDMessages>();

		await component.InvokeAsync(() => messages.Instance.OnImageChunk("a", "AAAA", false, "image/png", null));
		await component.InvokeAsync(() => messages.Instance.OnImageChunk("b", TinyPng, true, "image/png", null));
		await component.InvokeAsync(() => messages.Instance.OnImageChunk("a", "BBBB", true, "image/png", null));

		component.FindAll(".pdchat-image-thumb img").Select(image => image.GetAttribute("src"))
			.Should().Equal($"data:image/png;base64,{TinyPng}", "data:image/png;base64,AAAABBBB");
	}

	/// <summary>Verifies that an image whose pieces add up to too much is refused with a message, and nothing is added.</summary>
	[Fact]
	public async Task An_image_that_grows_past_the_limit_is_refused()
	{
		var component = RenderChat(new FakeChatService { IsImageInputOffered = true });
		var messages = component.FindComponent<PDMessages>();
		var piece = new string('A', 1024 * 1024);

		for (var i = 0; i < 9; i++)
		{
			await component.InvokeAsync(() => messages.Instance.OnImageChunk("big", piece, false, "image/png", null));
		}

		component.FindAll(".pdchat-image-thumb").Should().BeEmpty();
		component.Find(".pdchat-image-notice").TextContent.Should().Contain("too large");
	}

	private static async Task AddImagesAsync(IRenderedComponent<PDChat> component, int count)
	{
		var messages = component.FindComponent<PDMessages>();
		for (var i = 0; i < count; i++)
		{
			var name = $"shot{i}";
			await component.InvokeAsync(() => messages.Instance.OnImageAdded("image/png", TinyPng, name));
		}

		// PDMessages renders again about 10 ms later, and that render retires the handlers a click would be aimed at.
		await Task.Delay(250, Xunit.TestContext.Current.CancellationToken);
	}
}
