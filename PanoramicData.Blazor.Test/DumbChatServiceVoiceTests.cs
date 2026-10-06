using AwesomeAssertions;
using PanoramicData.Blazor.Interfaces;
using PanoramicData.Blazor.Models;
using PanoramicData.Blazor.Services;

namespace PanoramicData.Blazor.Test;

/// <summary>
/// Tests that the voice, agent and model settings of <see cref="DumbChatService"/> can be changed, so the demo page can
/// show every option, and that each change is announced.
/// </summary>
public class DumbChatServiceVoiceTests
{
	/// <summary>Verifies the defaults the demo starts with, read through the interface so that the members are seen to bind.</summary>
	[Fact]
	public void The_demo_service_starts_with_the_documented_defaults()
	{
		using var service = new DumbChatService();
		IChatService chat = service;

		chat.VoiceEndpoints.Should().Be(PDChatVoiceEndpoints.Simulated);
		chat.Agents.Should().HaveCount(2);
		chat.Models.Should().HaveCount(2);
		chat.WakePhrases.Should().BeNull();
		chat.VoiceIdleTimeout.Should().Be(TimeSpan.FromSeconds(10));
		chat.VoiceAutoSendDelay.Should().Be(TimeSpan.FromMilliseconds(1000));
		chat.IsReadAloudEnabled.Should().BeFalse();
	}

	/// <summary>Verifies that each setting, changed through the interface where it is settable there, announces the change once.</summary>
	[Theory]
	[MemberData(nameof(Settings))]
	public void Changing_a_voice_setting_announces_it(string setting)
	{
		using var service = new DumbChatService();
		var announcements = 0;
		service.OnConfigurationChanged += () => announcements++;

		_changes[setting](service);
		_changes[setting](service);

		announcements.Should().Be(1, $"{setting} changed once and was then set to the same value");
	}

	/// <summary>Verifies that a wake phrase set through the interface reaches the demo service rather than the default store.</summary>
	[Fact]
	public void Wake_phrases_set_through_the_interface_are_the_services_own()
	{
		using var service = new DumbChatService();
		string[] phrases = ["Hey DumbBot"];

		((IChatService)service).WakePhrases = phrases;

		service.WakePhrases.Should().BeSameAs(phrases);
	}

	public static TheoryData<string> Settings() => [.. _changes.Keys];

	private static readonly string[] _phrases = ["Hey DumbBot"];

	private static readonly Dictionary<string, Action<DumbChatService>> _changes = new()
	{
		[nameof(DumbChatService.VoiceEndpoints)] = service => service.VoiceEndpoints = null,
		[nameof(DumbChatService.Agents)] = service => service.Agents = null,
		[nameof(DumbChatService.Models)] = service => service.Models = null,
		[nameof(IChatService.WakePhrases)] = service => ((IChatService)service).WakePhrases = _phrases,
		[nameof(IChatService.VoiceIdleTimeout)] = service => ((IChatService)service).VoiceIdleTimeout = TimeSpan.FromSeconds(3),
		[nameof(IChatService.VoiceAutoSendDelay)] = service => ((IChatService)service).VoiceAutoSendDelay = TimeSpan.FromMilliseconds(250),
		[nameof(IChatService.IsReadAloudEnabled)] = service => ((IChatService)service).IsReadAloudEnabled = true,
		[nameof(IChatService.SelectedAgentId)] = service => ((IChatService)service).SelectedAgentId = "pedant",
		[nameof(IChatService.SelectedModelId)] = service => ((IChatService)service).SelectedModelId = "dumb-max",
	};
}