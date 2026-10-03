using AwesomeAssertions;
using Bunit;
using Microsoft.AspNetCore.Components.Web;

namespace PanoramicData.Blazor.Test;

/// <summary>
/// Mute, clear and service announcement tests for <see cref="PDChat"/>.
/// </summary>
public partial class PDChatTests
{
	// ------------------------------------------------------------------------------------------
	// Mute, clear, live status and configuration
	// ------------------------------------------------------------------------------------------

	/// <summary>Verifies that the mute button toggles the service's mute state and raises the event.</summary>
	[Fact]
	public async Task Mute_toggles_the_service_and_raises_the_event()
	{
		var toggles = 0;
		var service = new FakeChatService();
		var component = RenderChat(service, p => p.Add(x => x.OnMuteToggled, () => toggles++));

		await HeaderButton(component, "Mute").ClickAsync(new MouseEventArgs());

		service.IsMuted.Should().BeTrue();
		HeaderButton(component, "Unmute").TextContent.Should().Contain("🔇");
		toggles.Should().Be(1);
	}

	/// <summary>Verifies that a mute change announced by the service is reflected.</summary>
	[Fact]
	public async Task A_mute_change_from_the_service_is_reflected()
	{
		var service = new FakeChatService();
		var component = RenderChat(service);

		await component.InvokeAsync(() => service.AnnounceMute(true));

		component.WaitForAssertion(() => HeaderButton(component, "Unmute").Should().NotBeNull(), Patience);
	}

	/// <summary>Verifies that live-status and configuration announcements re-render the chat.</summary>
	[Fact]
	public async Task Live_and_configuration_announcements_re_render()
	{
		var service = new FakeChatService();
		var component = RenderChat(service);

		await component.InvokeAsync(() => service.AnnounceLive(false));
		component.WaitForAssertion(() => component.Find(".pdchat-title").TextContent.Should().EndWith("(Offline)"), Patience);

		service.Title = "Renamed";
		await component.InvokeAsync(service.AnnounceConfiguration);
		component.WaitForAssertion(() => component.Find(".pdchat-title").TextContent.Should().StartWith("Renamed"), Patience);
	}

	/// <summary>Verifies that Clear empties the transcript and the service and raises the event.</summary>
	[Fact]
	public async Task Clear_empties_the_transcript_and_the_service()
	{
		var cleared = 0;
		var service = new FakeChatService();
		service.Store.Add(Message("Old"));
		var component = RenderChat(service, p => p.Add(x => x.OnChatCleared, () => cleared++));

		await HeaderButton(component, "Clear Chat").ClickAsync(new MouseEventArgs());

		component.FindAll(".pdchat-message").Should().BeEmpty();
		service.ClearCount.Should().Be(1);
		cleared.Should().Be(1);
		component.FindAll(".pdchat-header-btn[title='Clear Chat']").Should().BeEmpty();
	}

	/// <summary>Verifies that Clear is not offered when not permitted.</summary>
	[Fact]
	public void Clear_is_absent_when_not_permitted()
	{
		var service = new FakeChatService { IsClearPermitted = false };
		service.Store.Add(Message("Old"));

		var component = RenderChat(service);

		component.FindAll(".pdchat-header-btn[title='Clear Chat']").Should().BeEmpty();
	}
}
