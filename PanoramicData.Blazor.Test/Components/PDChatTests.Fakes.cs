using PanoramicData.Blazor.Interfaces;
using PanoramicData.Blazor.Models;

namespace PanoramicData.Blazor.Test;

/// <summary>
/// The chat service double used by the <see cref="PDChat"/> tests: its settings and its transcript.
/// </summary>
public partial class PDChatTests
{
	/// <summary>A chat service whose every setting can be changed and whose every event can be raised.</summary>
	private sealed partial class FakeChatService : IChatService
	{
		public List<ChatMessage> Store { get; } = [];
		public List<ChatMessage> Sent { get; } = [];
		public Dictionary<Guid, List<ChatMessage>> ConversationStore { get; } = [];
		public int ClearCount { get; private set; }
		public int InitializeCount { get; private set; }

		public bool IsLive { get; set; } = true;
		public PDChatDockMode DockMode { get; set; } = PDChatDockMode.BottomRight;
		public PDChatDockMode PreferredDockMode { get; set; } = PDChatDockMode.BottomRight;
		public PDChatDockMode RestoreMode { get; set; } = PDChatDockMode.BottomRight;
		public PDChatButtonPosition MinimizedButtonPosition { get; set; } = PDChatButtonPosition.BottomRight;
		public bool IsMuted { get; set; }
		public string Title { get; set; } = "Test Chat";
		public bool IsMaximizePermitted { get; set; } = true;
		public bool IsCanvasUsePermitted { get; set; } = true;
		public bool IsClearPermitted { get; set; } = true;
		public bool AutoRestoreOnNewMessage { get; set; }
		public bool UseFullWidthMessages { get; set; } = true;
		public MessageMetadataDisplayMode MessageMetadataDisplayMode { get; set; } = MessageMetadataDisplayMode.UserOnlyOnRightOthersOnLeft;
		public bool ShowMessageUserIcon { get; set; } = true;
		public bool ShowMessageUserName { get; set; } = true;
		public bool ShowMessageTimestamp { get; set; } = true;
		public string MessageTimestampFormat { get; set; } = "HH:mm:ss";

		[Obsolete("Required by the interface for backward compatibility; superseded by the toast API.")]
		public bool ShowLastMessage { get; set; }

		[Obsolete("Required by the interface for backward compatibility; superseded by the toast API.")]
		public double ShowLastMessageDurationSeconds { get; set; } = 5d;

		public bool ToastEnabled { get; set; }
		public PDChatToastAnimation ToastEntryAnimation { get; set; } = PDChatToastAnimation.Grow;
		public PDChatToastAnimation ToastExitAnimation { get; set; } = PDChatToastAnimation.Grow;
		public double ToastAnimationDurationMs { get; set; } = 1;
		public bool ToastAutoDismiss { get; set; }
		public double ToastDisplayDurationSeconds { get; set; } = 5;
		public bool ToastShowTitle { get; set; } = true;
		public string ToastMinWidth { get; set; } = string.Empty;
		public string ToastMaxWidth { get; set; } = string.Empty;
		public string ToastMinHeight { get; set; } = string.Empty;
		public string ToastMaxHeight { get; set; } = string.Empty;
		public int ToastMaxVisible { get; set; } = 3;
		public PDChatButtonPosition ToastAnchor { get; set; } = PDChatButtonPosition.BottomRight;
		public PDChatVoiceEndpoints? VoiceEndpoints { get; set; }
		public IReadOnlyList<PDChatAgentOption>? Agents { get; set; }

		public IReadOnlyList<ChatMessage> Messages => Store;
		public bool SupportsConversations { get; init; }
		public Guid ActiveConversationId { get; set; }

		public IReadOnlyList<ChatMessage> GetMessages(Guid conversationId)
			=> ConversationStore.TryGetValue(conversationId, out var messages) ? messages : [];

		public void SendMessage(ChatMessage chatMessage) => Sent.Add(chatMessage);

		public void SendMessage(Guid conversationId, ChatMessage chatMessage)
		{
			Sent.Add(chatMessage);
			if (ConversationStore.TryGetValue(conversationId, out var messages))
			{
				messages.Add(chatMessage);
			}
		}

		public void ClearMessages()
		{
			Store.Clear();
			ClearCount++;
		}

		public void Initialize() => InitializeCount++;

		public void Dispose()
		{
			// Nothing to release.
		}
	}
}
