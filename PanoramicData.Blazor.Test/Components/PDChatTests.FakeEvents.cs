using System;
using PanoramicData.Blazor.Models;

namespace PanoramicData.Blazor.Test;

/// <summary>
/// The events of the chat service double used by the <see cref="PDChat"/> tests, and the methods that raise them.
/// </summary>
public partial class PDChatTests
{
	/// <summary>The events of the chat service double, each of which a test can raise.</summary>
	private sealed partial class FakeChatService
	{
		public bool HasSubscribers => _onMessageReceived is not null || _onDockModeChanged is not null;

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
}
