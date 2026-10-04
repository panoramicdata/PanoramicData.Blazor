using System;
using PanoramicData.Blazor.Interfaces;
using PanoramicData.Blazor.Models;

namespace PanoramicData.Blazor.Test;

/// <summary>
/// The chat service doubles used by the conversation addressing tests: one written before conversations existed,
/// and one that knows about them, and a builder for the messages they carry.
/// </summary>
public partial class ChatServiceConversationAddressingTests
{
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

	private static ChatMessage NewMessage(string text) => new()
	{
		Id = Guid.NewGuid(),
		Sender = new ChatMessageSender { Name = "User", IsUser = true, IsHuman = true },
		Message = text,
		Type = MessageType.Normal
	};
}
