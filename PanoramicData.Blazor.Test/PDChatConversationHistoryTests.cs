using AwesomeAssertions;
using Bunit;
using PanoramicData.Blazor.Interfaces;
using PanoramicData.Blazor.Models;

namespace PanoramicData.Blazor.Test;

/// <summary>
/// Tests that <see cref="PDChat"/> is unchanged for a host that has no conversation history
/// (issue #108, MS-25786).
/// </summary>
/// <remarks>
/// The promise of this feature is that a host which does not implement
/// <see cref="IChatConversationService"/> gets exactly the chat it has today. The conversation UI that follows
/// - sidebar, tabs, toolbar - all hangs off the same optional dependency, so this is the test that keeps that
/// promise true as those land: it asserts on the <i>absence</i> of conversation markup rather than on the
/// presence of today's, so it does not need rewriting each time the docked chat is restyled.
/// </remarks>
public class PDChatConversationHistoryTests : BunitContext
{
	/// <summary>Sets up the rendering context.</summary>
	public PDChatConversationHistoryTests()
		// PDChat imports a JavaScript module. Loose mode stubs it, which is all these tests need:
		// none of them exercise anything on the JavaScript side.
		=> JSInterop.Mode = JSRuntimeMode.Loose;

	/// <summary>
	/// Verifies that the conversation history is genuinely optional - a host supplying nothing gets a chat
	/// that renders, with no conversation UI anywhere in it.
	/// </summary>
	/// <remarks>
	/// The class-prefix check is the durable half. Every part of the conversation UI is namespaced
	/// <c>pdchat-conversation-</c>, so this fails the moment any of it renders on the path where no store
	/// exists to back it - which would present to a user as a sidebar that lists nothing and controls that do
	/// nothing.
	/// </remarks>
	[Fact]
	public void Without_a_conversation_service_the_chat_shows_no_conversation_history()
	{
		var component = Render<PDChat>(parameters => parameters
			.Add(p => p.ChatService, new SilentChatService())
			.Add(p => p.User, new ChatMessageSender { Name = "Tester", IsUser = true, IsHuman = true }));

		component.Instance.ConversationService.Should().BeNull(
			"a host that supplies nothing must not acquire a conversation store by some other route");

		component.Markup.Should().NotContain("pdchat-conversation-");
	}

	/// <summary>
	/// Verifies that supplying a conversation service is all a host has to do - it is a parameter, not a
	/// registration.
	/// </summary>
	/// <remarks>
	/// It is deliberately a parameter rather than an injected service, matching <c>ChatService</c> beside it.
	/// Acquiring the two by different routes would let a host pass a bespoke chat service and silently receive
	/// a conversation store from the container that knows nothing about it: two halves of one conversation,
	/// disagreeing.
	/// </remarks>
	[Fact]
	public void A_conversation_service_is_supplied_as_a_parameter()
	{
		var conversationService = new SilentConversationService();

		var component = Render<PDChat>(parameters => parameters
			.Add(p => p.ChatService, new SilentChatService())
			.Add(p => p.User, new ChatMessageSender { Name = "Tester", IsUser = true, IsHuman = true })
			.Add(p => p.ConversationService, conversationService));

		component.Instance.ConversationService.Should().BeSameAs(conversationService);
	}

	/// <summary>A chat service that holds no messages and sends nowhere.</summary>
	private sealed class SilentChatService : TestChatServiceBase
	{
		public override IReadOnlyList<ChatMessage> Messages => [];

		public override void SendMessage(ChatMessage chatMessage)
		{
		}

		public override void ClearMessages()
		{
		}
	}

	/// <summary>A conversation store holding nothing, present only so the parameter has something to hold.</summary>
	private sealed class SilentConversationService : IChatConversationService
	{
		public Task<ChatConversationPage> ListAsync(ChatConversationQuery query, CancellationToken cancellationToken)
			=> Task.FromResult(ChatConversationPage.Empty);

		public Task<IReadOnlyList<ChatMessage>> GetMessagesAsync(Guid id, CancellationToken cancellationToken)
			=> Task.FromResult<IReadOnlyList<ChatMessage>>([]);

		public Task<ChatConversation> CreateAsync(CancellationToken cancellationToken)
			=> Task.FromResult(new ChatConversation { Id = Guid.NewGuid() });

		public Task RenameAsync(Guid id, string title, CancellationToken cancellationToken)
			=> Task.CompletedTask;

		public Task ArchiveAsync(Guid id, CancellationToken cancellationToken)
			=> Task.CompletedTask;

		public Task UnarchiveAsync(Guid id, CancellationToken cancellationToken)
			=> Task.CompletedTask;
	}
}
