using AwesomeAssertions;
using Bunit;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;
using PanoramicData.Blazor.Interfaces;
using PanoramicData.Blazor.Models;

namespace PanoramicData.Blazor.Test;

/// <summary>
/// The voice controls' state: the picker shows what the microphone is doing, the voice button marks Mic off, and a host's
/// shortcut (here Alt+M) cycles the choices.
/// </summary>
public partial class PDChatTests
{
	private static readonly KeyboardEventArgs _altM = new() { Code = "KeyM", Key = "m", AltKey = true };

	/// <summary>Verifies that the picker reads Mic off after a load, whatever mode was last chosen, because nothing listens yet.</summary>
	[Theory]
	[InlineData(PDChatVoiceMode.Hal)]
	[InlineData(PDChatVoiceMode.Dictate)]
	[InlineData(PDChatVoiceMode.Wake)]
	public void The_picker_reads_Mic_off_after_a_load(PDChatVoiceMode lastMode)
	{
		var service = WakeService();
		((IChatService)service).VoiceMode = lastMode;

		PickerValue(RenderChat(service)).Should().Be("MicOff");
	}

	/// <summary>Verifies that once the ear button resumes listening after Mic off, the picker reads the mode, not Mic off.</summary>
	[Theory]
	[InlineData(PDChatVoiceMode.Hal)]
	[InlineData(PDChatVoiceMode.Dictate)]
	[InlineData(PDChatVoiceMode.Wake)]
	public async Task The_picker_reads_the_mode_when_the_ear_button_resumes_listening(PDChatVoiceMode mode)
	{
		SetUpVoiceModule();
		var component = RenderChat(WakeService());
		await component.Find(".pdchat-voice-mode-picker select").ChangeAsync(new ChangeEventArgs { Value = mode.ToString() });
		await component.Find(".pdchat-voice-mode-picker select").ChangeAsync(new ChangeEventArgs { Value = "MicOff" });
		PickerValue(component).Should().Be("MicOff");

		await component.Find(".pdchat-voice-toggle").ClickAsync(new());

		PickerValue(component).Should().Be(mode.ToString());
	}

	/// <summary>Verifies that the voice button shows a Mic off badge while the microphone is off, in every mode.</summary>
	[Theory]
	[InlineData(PDChatVoiceMode.Hal)]
	[InlineData(PDChatVoiceMode.Dictate)]
	[InlineData(PDChatVoiceMode.Wake)]
	public void The_voice_button_shows_a_Mic_off_badge_while_the_microphone_is_off(PDChatVoiceMode mode)
	{
		var service = WakeService();
		((IChatService)service).VoiceMode = mode;

		var toggle = RenderChat(service).Find(".pdchat-voice-toggle");

		IconClass(toggle.QuerySelector(".pdchat-voice-off-badge")!).Should().Be("fas fa-microphone-slash");
	}

	/// <summary>Verifies that the voice button carries no Mic off badge, and keeps its mode's glyph, while listening.</summary>
	[Theory]
	[InlineData(PDChatVoiceMode.Hal, ".pdchat-hal-eye")]
	[InlineData(PDChatVoiceMode.Dictate, "i.fa-microphone")]
	[InlineData(PDChatVoiceMode.Wake, "i.fa-assistive-listening-systems")]
	public async Task The_voice_button_has_no_Mic_off_badge_while_listening(PDChatVoiceMode mode, string glyph)
	{
		SetUpVoiceModule();
		var component = await RenderInModeAsync(WakeService(), mode);

		var toggle = component.Find(".pdchat-voice-toggle");

		toggle.QuerySelector(".pdchat-voice-off-badge").Should().BeNull();
		toggle.QuerySelector(glyph).Should().NotBeNull();
	}

	/// <summary>Verifies that a host's Mic off icon replaces the default badge icon.</summary>
	[Fact]
	public void A_host_s_Mic_off_icon_replaces_the_default()
	{
		var component = RenderChat(VoiceService(), p => p.Add(x => x.Icons, new PDChatIcons { MicOff = "bi bi-mic-mute" }));

		IconClass(component.Find(".pdchat-voice-off-badge")).Should().Be("bi bi-mic-mute");
	}

	/// <summary>Verifies that a host hides the Mic off badge by giving it no icon.</summary>
	[Fact]
	public void An_empty_Mic_off_icon_shows_no_badge()
		=> RenderChat(VoiceService(), p => p.Add(x => x.Icons, new PDChatIcons { MicOff = string.Empty }))
			.FindAll(".pdchat-voice-off-badge").Should().BeEmpty();

	/// <summary>Verifies that Alt+M in the text box cycles Mic off, HAL, Dictate and back to Mic off.</summary>
	[Fact]
	public async Task Alt_M_cycles_Mic_off_HAL_and_Dictate()
	{
		var module = SetUpVoiceModule();
		var component = RenderWithShortcut(VoiceService());

		var seen = new List<string?>();
		for (var press = 0; press < 3; press++)
		{
			await component.Find("textarea").KeyDownAsync(_altM);
			seen.Add(PickerValue(component));
		}

		seen.Should().Equal("Hal", "Dictate", "MicOff");
		component.Instance.VoiceState.Should().Be(PDChatVoiceState.Off);
		module.Invocations["stop"].Should().ContainSingle();
	}

	/// <summary>Verifies that Alt+M includes the wake phrase mode when the host offers it.</summary>
	[Fact]
	public async Task Alt_M_includes_the_wake_phrase_mode_when_offered()
	{
		SetUpVoiceModule();
		var component = RenderWithShortcut(WakeService());

		var seen = new List<string?>();
		for (var press = 0; press < 4; press++)
		{
			await component.Find("textarea").KeyDownAsync(_altM);
			seen.Add(PickerValue(component));
		}

		seen.Should().Equal("Hal", "Dictate", "Wake", "MicOff");
	}

	/// <summary>Verifies that only Alt with the host's key cycles, so AltGr (Ctrl and Alt) and other keys type as usual.</summary>
	[Theory]
	[InlineData("KeyM", false, false, false, false)]
	[InlineData("KeyM", true, true, false, false)]
	[InlineData("KeyM", true, false, true, false)]
	[InlineData("KeyM", true, false, false, true)]
	[InlineData("KeyN", true, false, false, false)]
	public async Task Other_keys_do_not_cycle(string code, bool alt, bool ctrl, bool shift, bool meta)
	{
		var module = SetUpVoiceModule();
		var component = RenderWithShortcut(VoiceService());

		await component.Find("textarea").KeyDownAsync(new KeyboardEventArgs { Code = code, AltKey = alt, CtrlKey = ctrl, ShiftKey = shift, MetaKey = meta });

		component.Instance.VoiceState.Should().Be(PDChatVoiceState.Off);
		module.Invocations.Should().BeEmpty();
	}

	/// <summary>Verifies that the shortcut is off unless the host names its key, so a host's own Alt shortcuts are left alone.</summary>
	[Fact]
	public async Task Alt_M_does_nothing_unless_the_host_names_the_key()
	{
		var module = SetUpVoiceModule();
		var component = RenderChat(VoiceService());

		await component.Find("textarea").KeyDownAsync(_altM);

		component.Instance.VoiceState.Should().Be(PDChatVoiceState.Off);
		module.Invocations.Should().BeEmpty();
	}

	/// <summary>Verifies that cycling leaves read aloud as the user set it.</summary>
	[Theory]
	[InlineData(true)]
	[InlineData(false)]
	public async Task Alt_M_leaves_read_aloud_as_set(bool readAloud)
	{
		SetUpVoiceModule();
		var service = VoiceService(readAloud: readAloud);
		var component = RenderWithShortcut(service);

		for (var press = 0; press < 3; press++)
		{
			await component.Find("textarea").KeyDownAsync(_altM);
		}

		((IChatService)service).IsReadAloudEnabled.Should().Be(readAloud);
	}

	/// <summary>Verifies that Alt+M does nothing while the chat is minimised, where the voice controls are not shown.</summary>
	[Fact]
	public async Task Alt_M_does_nothing_while_the_chat_is_minimised()
	{
		var module = SetUpVoiceModule();
		var service = VoiceService();
		service.DockMode = PDChatDockMode.Minimized;
		var component = RenderWithShortcut(service);

		await component.Find(".pdchat-toggle-collapsed").KeyDownAsync(_altM);

		component.Instance.VoiceState.Should().Be(PDChatVoiceState.Off);
		module.Invocations.Should().BeEmpty();
	}

	/// <summary>Verifies that the picker's tooltip names the shortcut when the host has one, and is unchanged when not.</summary>
	[Fact]
	public void The_picker_s_tooltip_names_the_shortcut_when_there_is_one()
	{
		RenderWithShortcut(VoiceService()).Find(".pdchat-voice-mode-picker select").GetAttribute("title")
			.Should().Be("How Voice Mode listens. Alt+M cycles the choices.");
		RenderChat(VoiceService()).Find(".pdchat-voice-mode-picker select").GetAttribute("title")
			.Should().Be("How Voice Mode listens");
	}

	private IRenderedComponent<PDChat> RenderWithShortcut(FakeChatService service)
		=> RenderChat(service, p => p.Add(x => x.VoiceModeShortcutKey, "KeyM"));

	private static string? PickerValue(IRenderedComponent<PDChat> component)
		=> component.Find(".pdchat-voice-mode-picker select").GetAttribute("value");
}
