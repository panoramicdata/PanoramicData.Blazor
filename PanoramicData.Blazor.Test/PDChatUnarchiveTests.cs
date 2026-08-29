using AwesomeAssertions;
using Bunit;
using PanoramicData.Blazor.Interfaces;
using PanoramicData.Blazor.Models;
using PanoramicData.Blazor.Services;

namespace PanoramicData.Blazor.Test;

/// <summary>
/// Tests the un-archive control in the full-screen conversation toolbar (issue #121, MS-25789).
/// </summary>
/// <remarks>
/// The point of these is that archiving is reversible from the UI, not merely from the API. Until this,
/// <see cref="IChatConversationService.UnarchiveAsync"/> existed and nothing anywhere called it - so a user
/// who archived the wrong conversation could see it (by including archived in the list) and had no way to
/// get it back. An archive with no way back is a delete wearing a different name, which is precisely what
/// this feature promises never to do.
/// </remarks>
public class PDChatUnarchiveTests : BunitContext
{
	/// <summary>Sets up the rendering context.</summary>
	public PDChatUnarchiveTests() => JSInterop.Mode = JSRuntimeMode.Loose;

	/// <summary>
	/// Verifies that the control is absent for a conversation that is not archived.
	/// </summary>
	/// <remarks>
	/// Hidden rather than disabled: un-archiving something never archived is meaningless, not merely
	/// unavailable, and a permanently greyed control teaches the reader nothing.
	/// </remarks>
	[Fact]
	public async Task The_control_is_absent_when_the_conversation_is_not_archived()
	{
		var (component, _) = await RenderFullScreenWithConversationAsync(isArchived: false);

		component.Markup.Should().NotContain("Un-archive");
	}

	/// <summary>Verifies that the control appears for an archived conversation that is open.</summary>
	/// <remarks>
	/// This is the case the old <see cref="ChatConversation.IsArchived"/> remark would have made impossible:
	/// it said opening an archived conversation should un-archive it, which would mean an open conversation
	/// is never archived and the control never renders.
	/// </remarks>
	[Fact]
	public async Task The_control_appears_for_an_open_archived_conversation()
	{
		var (component, _) = await RenderFullScreenWithConversationAsync(isArchived: true);

		component.Markup.Should().Contain("Un-archive");
	}

	/// <summary>Verifies that using the control un-archives the conversation in the store.</summary>
	[Fact]
	public async Task Using_the_control_un_archives_the_conversation_in_the_store()
	{
		var (component, store) = await RenderFullScreenWithConversationAsync(isArchived: true);

		await component.Find("button[title*='Un-archive']").ClickAsync(new());

		store.Unarchived.Should().ContainSingle().Which.Should().Be(store.ConversationId);
		store.Conversation.IsArchived.Should().BeFalse();
	}

	/// <summary>
	/// Verifies that the control disappears once used, without needing the list to be reloaded first.
	/// </summary>
	[Fact]
	public async Task The_control_disappears_once_the_conversation_is_un_archived()
	{
		var (component, _) = await RenderFullScreenWithConversationAsync(isArchived: true);

		await component.Find("button[title*='Un-archive']").ClickAsync(new());

		component.Markup.Should().NotContain("Un-archive");
	}

	/// <summary>
	/// Verifies that un-archiving leaves the conversation open, unlike archiving which closes its tab.
	/// </summary>
	/// <remarks>
	/// Archiving closes the tab because leaving an archived conversation open and re-activating it on the
	/// next keystroke is a confusing pair of behaviours. Un-archiving has no such tension: the user has just
	/// said they want this conversation back, so taking it off their screen would be perverse.
	/// </remarks>
	[Fact]
	public async Task Un_archiving_leaves_the_conversation_open()
	{
		var (component, store) = await RenderFullScreenWithConversationAsync(isArchived: true);

		await component.Find("button[title*='Un-archive']").ClickAsync(new());

		component.Markup.Should().Contain(store.Conversation.DisplayName);
	}

	/// <summary>
	/// Renders PDChat full-screen with one conversation open, in the given archive state.
	/// </summary>
	private async Task<(IRenderedComponent<PDChat> Component, RecordingConversationStore Store)>
		RenderFullScreenWithConversationAsync(bool isArchived)
	{
		var chatService = new DumbChatService { DockMode = PDChatDockMode.FullScreen };
		var store = new RecordingConversationStore(isArchived);

		var component = Render<PDChat>(parameters => parameters
			.Add(p => p.ChatService, chatService)
			.Add(p => p.User, new ChatMessageSender { Name = "Tester", IsUser = true, IsHuman = true })
			.Add(p => p.ConversationService, store));

		// An archived conversation is not in the default list, so it is not auto-opened - which is the point
		// of archiving. Reaching it is the journey a user actually takes: include archived conversations in
		// the list, then open the one you want back. Doing that here means these tests exercise the only
		// route to the control rather than a shortcut that does not exist in the product.
		if (isArchived)
		{
			await component.Find(".pdchat-conversation-include-archived input").ChangeAsync(new() { Value = true });
			await component.Find(".pdchat-conversation-row").ClickAsync(new());
		}

		return (component, store);
	}

	/// <summary>
	/// A conversation store holding exactly one conversation, recording what was asked of it.
	/// </summary>
	private sealed class RecordingConversationStore : IChatConversationService
	{
		public RecordingConversationStore(bool isArchived)
		{
			ConversationId = ChatConversation.ImplicitConversationId;
			Conversation = new ChatConversation
			{
				Id = ConversationId,
				Title = "A conversation",
				IsArchived = isArchived
			};
		}

		public Guid ConversationId { get; }

		public ChatConversation Conversation { get; }

		public List<Guid> Unarchived { get; } = [];

		public List<Guid> Archived { get; } = [];

		public Task<ChatConversationPage> ListAsync(ChatConversationQuery query, CancellationToken cancellationToken)
			=> Task.FromResult(new ChatConversationPage
			{
				Conversations = query.IncludeArchived || !Conversation.IsArchived ? [Conversation] : [],
				TotalCount = 1
			});

		public Task<IReadOnlyList<ChatMessage>> GetMessagesAsync(Guid id, CancellationToken cancellationToken)
			=> Task.FromResult<IReadOnlyList<ChatMessage>>([]);

		public Task<ChatConversation> CreateAsync(CancellationToken cancellationToken)
			=> Task.FromResult(new ChatConversation { Id = Guid.NewGuid() });

		public Task RenameAsync(Guid id, string title, CancellationToken cancellationToken)
			=> Task.CompletedTask;

		public Task ArchiveAsync(Guid id, CancellationToken cancellationToken)
		{
			Archived.Add(id);
			Conversation.IsArchived = true;
			return Task.CompletedTask;
		}

		public Task UnarchiveAsync(Guid id, CancellationToken cancellationToken)
		{
			Unarchived.Add(id);
			Conversation.IsArchived = false;
			return Task.CompletedTask;
		}
	}
}
