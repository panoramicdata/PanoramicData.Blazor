using AwesomeAssertions;
using PanoramicData.Blazor.Interfaces;
using PanoramicData.Blazor.Models;

namespace PanoramicData.Blazor.Test;

/// <summary>
/// The chat service and conversation store doubles used by the <see cref="PDChat"/> tests.
/// </summary>
public partial class PDChatTests
{
	/// <summary>A chat service whose every setting can be changed and whose every event can be raised.</summary>
	private sealed class FakeChatService : IChatService
	{
		public List<ChatMessage> Store { get; } = [];
		public List<ChatMessage> Sent { get; } = [];
		public Dictionary<Guid, List<ChatMessage>> ConversationStore { get; } = [];
		public int ClearCount { get; private set; }
		public int InitializeCount { get; private set; }
		public bool HasSubscribers => _onMessageReceived is not null || _onDockModeChanged is not null;

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

		public IReadOnlyList<ChatMessage> Messages => Store;
		public bool SupportsConversations { get; init; }
		public Guid ActiveConversationId { get; set; }

		private Action<ChatMessage>? _onMessageReceived;
		private Action<Guid, ChatMessage>? _onConversationMessageReceived;
		private Action<bool>? _onLiveStatusChanged;
		private Action<PDChatDockMode>? _onDockModeChanged;
		private Action<bool>? _onMuteStatusChanged;
		private Action? _onConfigurationChanged;

		public event Action<ChatMessage>? OnMessageReceived
		{
			add => _onMessageReceived += value;
			remove => _onMessageReceived -= value;
		}

		public event Action<Guid, ChatMessage>? OnConversationMessageReceived
		{
			add => _onConversationMessageReceived += value;
			remove => _onConversationMessageReceived -= value;
		}

		public event Action<bool>? OnLiveStatusChanged
		{
			add => _onLiveStatusChanged += value;
			remove => _onLiveStatusChanged -= value;
		}

		public event Action<PDChatDockMode>? OnDockModeChanged
		{
			add => _onDockModeChanged += value;
			remove => _onDockModeChanged -= value;
		}

		public event Action<bool>? OnMuteStatusChanged
		{
			add => _onMuteStatusChanged += value;
			remove => _onMuteStatusChanged -= value;
		}

		public event Action? OnConfigurationChanged
		{
			add => _onConfigurationChanged += value;
			remove => _onConfigurationChanged -= value;
		}

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

		public void Receive(ChatMessage message) => _onMessageReceived?.Invoke(message);

		public void ReceiveFor(Guid conversationId, ChatMessage message)
			=> _onConversationMessageReceived?.Invoke(conversationId, message);

		public void AnnounceLive(bool isLive)
		{
			IsLive = isLive;
			_onLiveStatusChanged?.Invoke(isLive);
		}

		public void AnnounceDockMode(PDChatDockMode mode) => _onDockModeChanged?.Invoke(mode);

		public void AnnounceMute(bool isMuted)
		{
			IsMuted = isMuted;
			_onMuteStatusChanged?.Invoke(isMuted);
		}

		public void AnnounceConfiguration() => _onConfigurationChanged?.Invoke();
	}

	/// <summary>A conversation store holding two conversations and recording what was asked of it.</summary>
	private sealed class FakeConversationStore(bool failTranscripts = false) : IChatConversationService
	{
		public ChatConversation First { get; } = new() { Id = Guid.NewGuid(), Title = "First" };
		public ChatConversation Second { get; } = new() { Id = Guid.NewGuid(), Title = "Second" };
		public List<Guid> MessageRequests { get; } = [];
		public List<ChatConversation> Created { get; } = [];
		public List<(Guid Id, string Title)> Renamed { get; } = [];
		public List<Guid> Archived { get; } = [];

		public Task<ChatConversationPage> ListAsync(ChatConversationQuery query, CancellationToken cancellationToken)
		{
			cancellationToken.ThrowIfCancellationRequested();
			ChatConversation[] all = [First, Second, .. Created];
			var visible = all.Where(c => query.IncludeArchived || !c.IsArchived).ToList();
			return Task.FromResult(new ChatConversationPage { Conversations = visible, TotalCount = visible.Count });
		}

		public Task<IReadOnlyList<ChatMessage>> GetMessagesAsync(Guid id, CancellationToken cancellationToken)
		{
			cancellationToken.ThrowIfCancellationRequested();
			MessageRequests.Add(id);
			if (failTranscripts)
			{
				throw new InvalidOperationException("Store offline");
			}

			var title = id == First.Id ? "First" : id == Second.Id ? "Second" : "New";
			return Task.FromResult<IReadOnlyList<ChatMessage>>([Message($"{title} message")]);
		}

		public Task<ChatConversation> CreateAsync(CancellationToken cancellationToken)
		{
			cancellationToken.ThrowIfCancellationRequested();
			var conversation = new ChatConversation { Id = Guid.NewGuid() };
			Created.Add(conversation);
			return Task.FromResult(conversation);
		}

		public Task RenameAsync(Guid id, string title, CancellationToken cancellationToken)
		{
			cancellationToken.ThrowIfCancellationRequested();
			Renamed.Add((id, title));
			return Task.CompletedTask;
		}

		public Task ArchiveAsync(Guid id, CancellationToken cancellationToken)
		{
			cancellationToken.ThrowIfCancellationRequested();
			Archived.Add(id);
			SetArchived(id, true);
			return Task.CompletedTask;
		}

		public Task UnarchiveAsync(Guid id, CancellationToken cancellationToken)
		{
			cancellationToken.ThrowIfCancellationRequested();
			SetArchived(id, false);
			return Task.CompletedTask;
		}

		private void SetArchived(Guid id, bool isArchived)
		{
			foreach (var conversation in new[] { First, Second }.Concat(Created).Where(c => c.Id == id))
			{
				conversation.IsArchived = isArchived;
			}
		}
	}
}
