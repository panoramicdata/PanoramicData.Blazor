using AwesomeAssertions;
using Bunit;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;
using PanoramicData.Blazor.Interfaces;
using PanoramicData.Blazor.Models;

namespace PanoramicData.Blazor.Test;

/// <summary>
/// Issue #274: the input toolbar's icons, which a host may replace through <see cref="PDChat.Icons"/>.
/// </summary>
public partial class PDChatTests
{
	/// <summary>Verifies that a host's icon classes replace the defaults on every toolbar button.</summary>
	[Fact]
	public void A_host_s_icons_replace_the_defaults()
	{
		var service = VoiceService(readAloud: false);
		service.IsImageInputOffered = true;
		((IChatService)service).VoiceMode = PDChatVoiceMode.Dictate;
		var icons = new PDChatIcons
		{
			SoundsOn = "bi bi-bell",
			ReadAloudOff = "bi bi-volume-mute",
			Dictate = "bi bi-mic",
			Attach = "bi bi-paperclip",
			Screenshot = "bi bi-camera",
			Send = "bi bi-send"
		};

		var component = RenderChat(service, p => p.Add(x => x.Icons, icons));

		IconClass(component.Find(".pdchat-mute-toggle")).Should().Be("bi bi-bell");
		IconClass(component.Find(".pdchat-read-aloud-toggle")).Should().Be("bi bi-volume-mute");
		IconClass(component.Find(".pdchat-voice-toggle")).Should().Be("bi bi-mic");
		IconClass(component.Find(".pdchat-image-attach")).Should().Be("bi bi-paperclip");
		IconClass(component.Find(".pdchat-image-screenshot")).Should().Be("bi bi-camera");
		IconClass(component.Find(".chat-send")).Should().Be("bi bi-send");
	}

	/// <summary>Verifies that each header button shows its icon, and full screen swaps to the restore icon.</summary>
	[Fact]
	public async Task The_header_buttons_show_their_icons()
	{
		var service = new FakeChatService { IsCanvasUsePermitted = false };
		service.Store.Add(Message("Old"));
		var component = RenderChat(service);

		IconClass(HeaderButton(component, "Clear Chat")).Should().Be(_defaultIcons.Clear);
		IconClass(HeaderButton(component, "Dock to Side")).Should().Be(_defaultIcons.Pin);
		IconClass(HeaderButton(component, "Fullscreen")).Should().Be(_defaultIcons.FullScreen);
		IconClass(component.Find(".pdchat-close")).Should().Be(_defaultIcons.Minimize);

		await HeaderButton(component, "Fullscreen").ClickAsync(new MouseEventArgs());

		IconClass(HeaderButton(component, "Restore")).Should().Be(_defaultIcons.Restore);
	}

	/// <summary>Verifies that one pin docks a corner chat to the side, then shows pressed and unpins it.</summary>
	[Fact]
	public async Task The_pin_is_pressed_while_the_chat_is_docked_to_a_side()
	{
		var service = new FakeChatService { DockMode = PDChatDockMode.TopLeft };
		var component = RenderChat(service);
		component.FindAll(".pdchat-pin-toggle").Should().ContainSingle();
		HeaderButton(component, "Dock to Side").GetAttribute("aria-pressed").Should().Be("false");

		await HeaderButton(component, "Dock to Side").ClickAsync(new MouseEventArgs());

		service.DockMode.Should().Be(PDChatDockMode.Left);
		var pin = component.FindAll(".pdchat-pin-toggle").Should().ContainSingle().Subject;
		pin.GetAttribute("title").Should().Be("Unpin from Side");
		pin.GetAttribute("aria-pressed").Should().Be("true");
		pin.ClassList.Should().Contain("active");
		IconClass(pin).Should().Be(_defaultIcons.Pin);

		await pin.ClickAsync(new MouseEventArgs());

		service.DockMode.Should().Be(PDChatDockMode.TopLeft);
		HeaderButton(component, "Dock to Side").GetAttribute("aria-pressed").Should().Be("false");
	}

	/// <summary>Verifies that an agent with no icon of its own shows the default agent icon instead.</summary>
	[Fact]
	public async Task An_agent_without_its_own_icon_shows_the_default_agent_icon()
	{
		var component = RenderChat(new FakeChatService { Agents = [_merlin, _alice] });
		component.FindAll(".pdchat-agent-picker .pdchat-toolbar-picker-icon").Should().BeEmpty("Merlin has its own icon");

		await component.Find(".pdchat-agent-picker select").ChangeAsync(new ChangeEventArgs { Value = "alice" });

		component.WaitForAssertion(() => component.Find(".pdchat-agent-picker .pdchat-toolbar-picker-icon")
			.GetAttribute("class").Should().Be($"pdchat-toolbar-picker-icon {_defaultIcons.Agent}"), Patience);
	}

	/// <summary>Verifies that an empty icon class leaves the pickers without an icon.</summary>
	[Fact]
	public void An_empty_icon_class_shows_no_icon()
	{
		var component = RenderChat(
			new FakeChatService { Agents = [_alice, _merlin], Models = [_quick, _thorough] },
			p => p.Add(x => x.Icons, new PDChatIcons { Agent = string.Empty, Model = string.Empty }));

		component.FindAll(".pdchat-toolbar-picker-icon").Should().BeEmpty();
	}

	/// <summary>Verifies that paused dictation shows a pause badge on the microphone, gone once it listens.</summary>
	[Fact]
	public async Task Paused_dictation_shows_a_pause_badge()
	{
		SetUpVoiceModule();
		var component = await RenderInModeAsync(VoiceService(), PDChatVoiceMode.Dictate);
		component.Instance.VoiceState.Should().Be(PDChatVoiceState.Paused);
		component.FindAll(".pdchat-voice-toggle .pdchat-voice-paused-badge").Should().ContainSingle();

		await component.Find("textarea").FocusAsync(new FocusEventArgs());

		component.Instance.VoiceState.Should().Be(PDChatVoiceState.Listening);
		component.FindAll(".pdchat-voice-toggle .pdchat-voice-paused-badge").Should().BeEmpty();
	}

	/// <summary>Verifies that while an answer is read aloud the microphone's icon gives way to three bars.</summary>
	[Fact]
	public async Task A_spoken_answer_shows_bars_in_place_of_the_icon()
	{
		SetUpVoiceModule();
		var service = VoiceService();
		var component = await RenderInModeAsync(service, PDChatVoiceMode.Dictate);
		await component.Find("textarea").FocusAsync(new FocusEventArgs());
		await component.Find("textarea").InputAsync(new ChangeEventArgs { Value = "Is it done?" });
		await component.Find(".chat-input-accessories .chat-send").ClickAsync(new MouseEventArgs());

		await component.InvokeAsync(() => service.Receive(Message("Yes.")));

		component.Instance.VoiceState.Should().Be(PDChatVoiceState.Speaking);
		var toggle = component.Find(".pdchat-voice-toggle");
		toggle.QuerySelectorAll(".pdchat-voice-speaking-bars span").Should().HaveCount(3);
		IconClass(toggle).Should().BeNull();
	}
}
