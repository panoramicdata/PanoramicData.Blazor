using PanoramicData.Blazor.Services;

namespace PanoramicData.Blazor.Demo.Pages;

public partial class PDChatPage : IDisposable
{
	private readonly DumbChatService _chatService = new();
	private PDChat? _chatRef;

	// Demo configuration properties
	private PDChatDockMode _currentDockMode = PDChatDockMode.BottomRight;
	private PDChatDockPosition _currentChatDockPosition = PDChatDockPosition.Right;
	private bool _isMaximizePermitted = true;
	private bool _isCanvasUsePermitted = true;
	private bool _isClearPermitted = true;
	private bool _autoRestoreOnNewMessage = false;
	private string _chatTitle = "Demo Chat";

	private ChatMessageSender User => new()
	{
		Name = "Demo User",
		IsUser = true,
		IsHuman = true,
		IsSupport = false
	};

	private ChatMessageSender Bot => new()
	{
		Name = "Demo Bot",
		IsUser = false,
		IsHuman = false,
		IsSupport = true
	};

	protected override void OnInitialized()
	{
		// Send initial welcome message
		Task.Delay(1000).ContinueWith(_ =>
		{
			var welcomeMessage = new ChatMessage
			{
				Id = Guid.NewGuid(),
				Message = "Welcome to the PDChat demo! Try changing the dock modes and sending messages.",
				Sender = Bot,
				Type = MessageType.Normal,
				Timestamp = DateTime.UtcNow
			};
			_chatService.SendMessage(welcomeMessage);
		});
	}

	private void OnDockModeChanged(ChangeEventArgs e)
	{
		if (Enum.TryParse<PDChatDockMode>(e.Value?.ToString(), out var mode))
		{
			_currentDockMode = mode;
			StateHasChanged();
		}
	}

	private void SendWelcomeMessage()
	{
		var message = new ChatMessage
		{
			Id = Guid.NewGuid(),
			Message = "Hello! This is a welcome message from the demo bot. The chat is working perfectly!",
			Sender = Bot,
			Type = MessageType.Normal,
			Timestamp = DateTime.UtcNow
		};
		_chatService.SendMessage(message);
	}

	private void SendInfoMessage()
	{
		var message = new ChatMessage
		{
			Id = Guid.NewGuid(),
			Message = "ℹ️ This is an informational message. You can customize message types and icons.",
			Sender = Bot,
			Type = MessageType.Normal,
			Timestamp = DateTime.UtcNow
		};
		_chatService.SendMessage(message);
	}

	private void SendWarningMessage()
	{
		var message = new ChatMessage
		{
			Id = Guid.NewGuid(),
			Message = "This is a warning message. Pay attention to important notifications!",
			Sender = Bot,
			Type = MessageType.Warning,
			Timestamp = DateTime.UtcNow
		};
		_chatService.SendMessage(message);
	}

	private void SendErrorMessage()
	{
		var message = new ChatMessage
		{
			Id = Guid.NewGuid(),
			Message = "This is an error message. Something went wrong in the system.",
			Sender = Bot,
			Type = MessageType.Error,
			Timestamp = DateTime.UtcNow
		};
		_chatService.SendMessage(message);
	}

	private async Task SendTypingMessage()
	{
		// First show typing indicator
		var typingMessage = new ChatMessage
		{
			Id = Guid.NewGuid(),
			Message = "Typing...",
			Sender = Bot,
			Type = MessageType.Typing,
			Timestamp = DateTime.UtcNow
		};
		_chatService.SendMessage(typingMessage);

		// Wait a bit and then replace with actual message
		await Task.Delay(2000);

		var actualMessage = new ChatMessage
		{
			Id = typingMessage.Id, // Same ID to replace the typing message
			Message = "Here's the message I was typing! The typing indicator helps show when someone is responding.",
			Sender = Bot,
			Type = MessageType.Normal,
			Timestamp = DateTime.UtcNow
		};
		_chatService.SendMessage(actualMessage);
	}

	private static string? GetUserIcon(ChatMessage chatMessage)
		=> chatMessage.Sender.IsHuman ? "👤" : "🤖";

	private static string? GetPriorityIcon(ChatMessage chatMessage)
		=> chatMessage.Type switch
		{
			MessageType.Normal or MessageType.Typing => string.Empty,
			MessageType.Warning => "⚠️",
			MessageType.Error => "🛑",
			MessageType.Critical => "🚨",
			_ => "?"
		};

	private static string? GetSoundUrl(ChatMessage chatMessage)
		=> chatMessage.Sender.IsUser || chatMessage.Type == MessageType.Typing
			? null
			: "/_content/PanoramicData.Blazor.Demo/sounds/" + chatMessage.Type switch
			{
				MessageType.Normal => "tick.mp3",
				MessageType.Warning => "warning.mp3",
				MessageType.Error => "error.mp3",
				MessageType.Critical => "critical.mp3",
				_ => null
			};

	private void TestAutoRestore()
	{
		var message = new ChatMessage
		{
			Id = Guid.NewGuid(),
			Message = "🔄 This message was sent to test the auto-restore feature. If auto-restore is enabled and the chat is minimized, it should automatically open when this message arrives.",
			Sender = Bot,
			Type = MessageType.Normal,
			Timestamp = DateTime.UtcNow
		};
		_chatService.SendMessage(message);
	}

	public void Dispose()
	{
		_chatService?.Dispose();
		GC.SuppressFinalize(this);
	}
}