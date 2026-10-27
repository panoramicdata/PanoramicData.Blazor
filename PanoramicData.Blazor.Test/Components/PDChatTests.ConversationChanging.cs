using AwesomeAssertions;
using Bunit;
using PanoramicData.Blazor.Models;

namespace PanoramicData.Blazor.Test;

/// <summary>
/// The host's chance to refuse a conversation switch, for example because the page behind the chat has unsaved changes.
/// </summary>
public partial class PDChatTests
{
	private (IRenderedComponent<PDChat> Component, FakeConversationStore Store, FakeChatService Service) RenderConversationsAsking(Func<Guid, Task<bool>> changing)
	{
		var store = new FakeConversationStore(failTranscripts: false);
		var service = new FakeChatService
		{
			DockMode = PDChatDockMode.FullScreen,
			SupportsConversations = true,
			ActiveConversationId = store.First.Id
		};
		service.ConversationStore[store.First.Id] = [Message("From the service")];
		var component = RenderChat(service, p => p
			.Add(x => x.ConversationService, store)
			.Add(x => x.ConversationChanging, changing));
		return (component, store, service);
	}

	/// <summary>Verifies that a refused sidebar choice leaves the first conversation shown and opens no tab.</summary>
	[Fact]
	public async Task A_refused_sidebar_choice_keeps_the_current_conversation()
	{
		var (component, store, service) = RenderConversationsAsking(_ => Task.FromResult(false));
		component.WaitForAssertion(() => TabTitles(component).Should().Equal("First"), Patience);

		await SidebarRow(component, "Second").ClickAsync(new());

		TabTitles(component).Should().Equal("First");
		ActiveTabTitle(component).Should().Be("First");
		service.ActiveConversationId.Should().Be(store.First.Id);
	}

	/// <summary>Verifies that a refused tab click puts the selection back on the conversation still shown.</summary>
	[Fact]
	public async Task A_refused_tab_click_keeps_the_current_conversation()
	{
		var refuse = false;
		var (component, store, service) = RenderConversationsAsking(_ => Task.FromResult(!refuse));
		component.WaitForAssertion(() => TabTitles(component).Should().Equal("First"), Patience);
		await SidebarRow(component, "Second").ClickAsync(new());
		component.WaitForAssertion(() => ActiveTabTitle(component).Should().Be("Second"), Patience);

		refuse = true;
		await Tab(component, "First").ClickAsync(new());

		component.WaitForAssertion(() => ActiveTabTitle(component).Should().Be("Second"), Patience);
		service.ActiveConversationId.Should().Be(store.Second.Id);
	}

	/// <summary>Verifies that the host is told which conversation is wanted, and that agreeing switches to it.</summary>
	[Fact]
	public async Task An_agreed_choice_switches_and_names_the_wanted_conversation()
	{
		var asked = new List<Guid>();
		var (component, store, _) = RenderConversationsAsking(id =>
		{
			asked.Add(id);
			return Task.FromResult(true);
		});
		component.WaitForAssertion(() => TabTitles(component).Should().Equal("First"), Patience);

		await SidebarRow(component, "Second").ClickAsync(new());

		component.WaitForAssertion(() => ActiveTabTitle(component).Should().Be("Second"), Patience);
		asked.Should().Equal(store.Second.Id);
	}

	/// <summary>Verifies that choosing the conversation already shown does not ask the host.</summary>
	[Fact]
	public async Task The_current_conversation_is_not_asked_about()
	{
		var asked = 0;
		var (component, _, _) = RenderConversationsAsking(_ =>
		{
			asked++;
			return Task.FromResult(false);
		});
		component.WaitForAssertion(() => TabTitles(component).Should().Equal("First"), Patience);

		await SidebarRow(component, "First").ClickAsync(new());

		asked.Should().Be(0);
	}
}
