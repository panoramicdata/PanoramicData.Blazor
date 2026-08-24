using AwesomeAssertions;
using PanoramicData.Blazor.Interfaces;
using PanoramicData.Blazor.Models;

namespace PanoramicData.Blazor.Test;

/// <summary>
/// Tests for the conversation-addressed members added to <see cref="IChatService"/> (issue #104).
/// </summary>
/// <remarks>
/// <para>
/// The members exist so that several conversations can be live at once. They are default interface
/// implementations so that every existing implementation keeps compiling and behaving identically, which is the
/// same technique the toast API already used on this interface.
/// </para>
/// <para>
/// <b>The doubles are deliberately built two different ways.</b> <see cref="LegacyChatService"/> implements
/// <i>only</i> the singular members, so the tests against it exercise the real default interface implementations
/// rather than anything written here - a double that implemented the addressed members would confirm the test's
/// own premise and prove nothing about the interface.
/// <see cref="ConversationAwareChatService"/> implements the addressed members itself, and so exercises the
/// opt-in path.
/// </para>
/// <para>
/// The two negative tests matter most. A default implementation that quietly served its one transcript for an id
/// it had never heard of would be the "reply landed in the wrong tab" bug moved out of the UI and into the
/// interface, where it is harder to see and easier to trust.
/// </para>
/// </remarks>
public class ChatServiceConversationAddressingTests
{
	/// <summary>
	/// Verifies that a service written before conversations existed says so, rather than leaving a consumer to
	/// subscribe to an addressed event that will never fire.
	/// </summary>
	[Fact]
	public void A_service_that_knows_nothing_of_conversations_reports_that()
	{
		IChatService service = new LegacyChatService();

		service.SupportsConversations.Should().BeFalse();
	}

	/// <summary>Verifies that such a service still answers for its own single, implicit conversation.</summary>
	[Fact]
	public void Its_single_conversation_is_reachable_by_the_implicit_id()
	{
		IChatService service = new LegacyChatService();
		service.SendMessage(NewMessage("hello"));

		var messages = service.GetMessages(ChatConversation.ImplicitConversationId);

		messages.Should().ContainSingle();
		messages[0].Message.Should().Be("hello");
	}

	/// <summary>
	/// Verifies that asking for a conversation the service has never heard of yields nothing - rather than the
	/// one transcript it happens to have.
	/// </summary>
	[Fact]
	public void Reading_an_unknown_conversation_yields_nothing_rather_than_the_wrong_transcript()
	{
		IChatService service = new LegacyChatService();
		service.SendMessage(NewMessage("hello"));

		var messages = service.GetMessages(Guid.NewGuid());

		messages.Should().BeEmpty();
	}

	/// <summary>Verifies that sending to the implicit conversation reaches the underlying single-conversation send.</summary>
	[Fact]
	public void Sending_to_the_implicit_conversation_reaches_the_service()
	{
		var service = new LegacyChatService();

		((IChatService)service).SendMessage(ChatConversation.ImplicitConversationId, NewMessage("hello"));

		service.Messages.Should().ContainSingle();
		service.Messages[0].Message.Should().Be("hello");
	}

	/// <summary>
	/// Verifies that sending to an unknown conversation fails loudly instead of delivering to the wrong one.
	/// </summary>
	/// <remarks>
	/// Silent misdelivery is the whole failure this work exists to prevent. A caller addressing a conversation
	/// the service does not have has a bug, and it should surface where it happened rather than as a message
	/// appearing in somebody else's tab several seconds later.
	/// </remarks>
	[Fact]
	public void Sending_to_an_unknown_conversation_throws_rather_than_misdelivering()
	{
		var service = new LegacyChatService();

		var send = () => ((IChatService)service).SendMessage(Guid.NewGuid(), NewMessage("hello"));

		send.Should().Throw<InvalidOperationException>();
		service.Messages.Should().BeEmpty();
	}

	/// <summary>Verifies that a conversation-aware service opts in and keeps its transcripts apart.</summary>
	[Fact]
	public void A_conversation_aware_service_keeps_transcripts_apart()
	{
		var alpha = Guid.NewGuid();
		var beta = Guid.NewGuid();
		IChatService service = new ConversationAwareChatService(alpha, beta);

		service.SupportsConversations.Should().BeTrue();
		service.SendMessage(alpha, NewMessage("about the guest network"));
		service.SendMessage(beta, NewMessage("about the firewall"));

		service.GetMessages(alpha).Should().ContainSingle().Which.Message.Should().Be("about the guest network");
		service.GetMessages(beta).Should().ContainSingle().Which.Message.Should().Be("about the firewall");
	}

	/// <summary>
	/// Verifies that a reply for a background conversation is reported with the id that asked for it, so that a
	/// consumer can route it without guessing at "the current one".
	/// </summary>
	[Fact]
	public void A_reply_is_reported_against_the_conversation_that_asked_for_it()
	{
		var alpha = Guid.NewGuid();
		var beta = Guid.NewGuid();
		var service = new ConversationAwareChatService(alpha, beta);
		var received = new List<(Guid ConversationId, string Message)>();
		((IChatService)service).OnConversationMessageReceived += (id, m) => received.Add((id, m.Message));

		service.Receive(beta, "the firewall is fine");

		received.Should().ContainSingle();
		received[0].ConversationId.Should().Be(beta);
		received[0].Message.Should().Be("the firewall is fine");
	}

	/// <summary>
	/// Verifies that the active conversation of a service that knows nothing of conversations is the implicit
	/// one, so that a consumer has a single id it can always address.
	/// </summary>
	[Fact]
	public void The_implicit_conversation_is_active_by_default()
	{
		IChatService service = new LegacyChatService();

		service.ActiveConversationId.Should().Be(ChatConversation.ImplicitConversationId);
	}

	private static ChatMessage NewMessage(string text) => new()
	{
		Id = Guid.NewGuid(),
		Sender = new ChatMessageSender { Name = "User", IsUser = true, IsHuman = true },
		Message = text,
		Type = MessageType.Normal
	};

	/// <summary>
	/// A minimal <see cref="IChatService"/> implementing <i>only</i> the singular members, standing in for every
	/// implementation written before conversations existed. It deliberately does not implement any
	/// conversation-addressed member, so tests against it exercise the interface's own defaults.
	/// </summary>
	private sealed class LegacyChatService : TestChatServiceBase
	{
		private readonly List<ChatMessage> _messages = [];

		public override IReadOnlyList<ChatMessage> Messages => _messages;

		public override void SendMessage(ChatMessage chatMessage) => _messages.Add(chatMessage);

		public override void ClearMessages() => _messages.Clear();
	}

	/// <summary>
	/// A minimal <see cref="IChatService"/> that opts in to conversations and holds a transcript per conversation.
	/// </summary>
	/// <remarks>
	/// Note the repeated <see cref="IChatService"/> in the base list. It is not redundant: the interface mapping
	/// is fixed at <see cref="TestChatServiceBase"/>, so without re-stating the interface here these members
	/// would be ordinary class members and a caller holding an <see cref="IChatService"/> would silently get the
	/// defaults instead. That trap is documented on the interface itself.
	/// </remarks>
	private sealed class ConversationAwareChatService : TestChatServiceBase, IChatService
	{
		private readonly Dictionary<Guid, List<ChatMessage>> _byConversation;

		public ConversationAwareChatService(params Guid[] conversationIds)
		{
			_byConversation = conversationIds.ToDictionary(x => x, _ => new List<ChatMessage>());
			ActiveConversationId = conversationIds[0];
		}

		public bool SupportsConversations => true;

		public Guid ActiveConversationId { get; set; }

		public event Action<Guid, ChatMessage>? OnConversationMessageReceived;

		public override IReadOnlyList<ChatMessage> Messages => GetMessages(ActiveConversationId);

		public IReadOnlyList<ChatMessage> GetMessages(Guid conversationId)
			=> _byConversation.TryGetValue(conversationId, out var messages) ? messages : [];

		public void SendMessage(Guid conversationId, ChatMessage chatMessage)
		{
			if (!_byConversation.TryGetValue(conversationId, out var messages))
			{
				throw new InvalidOperationException($"Unknown conversation {conversationId}.");
			}

			messages.Add(chatMessage);
		}

		public override void SendMessage(ChatMessage chatMessage) => SendMessage(ActiveConversationId, chatMessage);

		public override void ClearMessages() => _byConversation[ActiveConversationId].Clear();

		public void Receive(Guid conversationId, string text)
		{
			var message = NewMessage(text);
			_byConversation[conversationId].Add(message);
			OnConversationMessageReceived?.Invoke(conversationId, message);
		}
	}
}
