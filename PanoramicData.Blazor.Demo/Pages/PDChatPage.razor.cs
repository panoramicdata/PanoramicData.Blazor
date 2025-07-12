using PanoramicData.Blazor.Models;

namespace PanoramicData.Blazor.Demo.Pages;

public partial class PDChatPage : IDisposable
{
	[CascadingParameter] protected EventManager? EventManager { get; set; }

	[Inject] private IChatService ChatService { get; set; } = default!;

	// This property will be synced with the global dock mode
	private PDChatDockMode _currentDockMode
	{
		get => ChatService.PreferredDockMode;
		set => ChatService.PreferredDockMode = value;
	}

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
		// Subscribe to configuration changes to trigger UI updates
		ChatService.OnConfigurationChanged += OnConfigurationChanged;
		ChatService.OnDockModeChanged += OnDockModeChanged;
	}

	private void OnConfigurationChanged()
	{
		StateHasChanged();
	}

	private void OnDockModeChanged(PDChatDockMode newMode)
	{
		StateHasChanged();
	}

	// Handle dock mode changes from the dropdown - this will trigger MainLayout updates
	private void OnDockModeChangedFromDropdown()
	{
		// The change will be automatically propagated through the service
		StateHasChanged();
	}

	// Handle restore mode changes from the dropdown
	private void OnRestoreModeChanged()
	{
		// The change will be automatically propagated through the service
		StateHasChanged();
	}

	// Handle auto-restore changes from the checkbox
	private void OnAutoRestoreChanged()
	{
		// The change will be automatically propagated through the service
		StateHasChanged();
	}

	// Helper methods to determine dock mode types
	private static bool IsCornerMode(PDChatDockMode mode)
		=> mode is PDChatDockMode.BottomRight or PDChatDockMode.TopRight
				   or PDChatDockMode.BottomLeft or PDChatDockMode.TopLeft;

	private static bool IsSplitMode(PDChatDockMode mode)
		=> mode is PDChatDockMode.Left or PDChatDockMode.Right
				   or PDChatDockMode.Top or PDChatDockMode.Bottom;

	private void SendWelcomeMessage()
	{
		var message = new ChatMessage
		{
			Id = Guid.NewGuid(),
			Message = "Hello! This is a welcome message from the demo bot. The chat is working perfectly across the entire application!",
			Sender = Bot,
			Type = MessageType.Normal,
			Timestamp = DateTime.UtcNow
		};
		ChatService.SendMessage(message);
	}

	private void SendInfoMessage()
	{
		var message = new ChatMessage
		{
			Id = Guid.NewGuid(),
			Message = "ℹ️ This is an informational message. Corner modes now respect max 30% width and 80% height constraints while maintaining usability. Navigate to other pages to see the chat persist!",
			Sender = Bot,
			Type = MessageType.Normal,
			Timestamp = DateTime.UtcNow
		};
		ChatService.SendMessage(message);
	}

	private void SendWarningMessage()
	{
		var message = new ChatMessage
		{
			Id = Guid.NewGuid(),
			Message = "This is a warning message. Pay attention to important notifications! Try switching to corner modes to see the new dimension constraints in action.",
			Sender = Bot,
			Type = MessageType.Warning,
			Timestamp = DateTime.UtcNow
		};
		ChatService.SendMessage(message);
	}

	private void SendErrorMessage()
	{
		var message = new ChatMessage
		{
			Id = Guid.NewGuid(),
			Message = "This is an error message. Something went wrong in the system. The chat will persist even when you navigate to other demo pages and now has proper size constraints.",
			Sender = Bot,
			Type = MessageType.Error,
			Timestamp = DateTime.UtcNow
		};
		ChatService.SendMessage(message);
	}

	private void SendCriticalMessage()
	{
		var message = new ChatMessage
		{
			Id = Guid.NewGuid(),
			Message = "🚨 CRITICAL: This is a critical message! Immediate attention required! The restored chat button functionality now works properly when starting from minimized state.",
			Sender = Bot,
			Type = MessageType.Critical,
			Timestamp = DateTime.UtcNow
		};
		ChatService.SendMessage(message);
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
		ChatService.SendMessage(typingMessage);

		// Wait a bit and then replace with actual message
		await Task.Delay(2000);

		var actualMessage = new ChatMessage
		{
			Id = typingMessage.Id, // Same ID to replace the typing message
			Message = "Here's the message I was typing! The typing indicator helps show when someone is responding. The global chat now works seamlessly with proper dimension constraints!",
			Sender = Bot,
			Type = MessageType.Normal,
			Timestamp = DateTime.UtcNow
		};
		ChatService.SendMessage(actualMessage);
	}

	private async Task TestAutoRestore()
	{
		// Small delay to ensure auto-restore setting is properly synchronized
		await Task.Delay(100);
		
		var message = new ChatMessage
		{
			Id = Guid.NewGuid(),
			Message = "🔄 This message was sent to test the auto-restore feature. If auto-restore is enabled and the chat is minimized, it should automatically open when this message arrives. The restored button functionality now works correctly!",
			Sender = Bot,
			Type = MessageType.Normal,
			Timestamp = DateTime.UtcNow
		};
		ChatService.SendMessage(message);
	}

	public void Dispose()
	{
		ChatService.OnConfigurationChanged -= OnConfigurationChanged;
		ChatService.OnDockModeChanged -= OnDockModeChanged;
		GC.SuppressFinalize(this);
	}
}