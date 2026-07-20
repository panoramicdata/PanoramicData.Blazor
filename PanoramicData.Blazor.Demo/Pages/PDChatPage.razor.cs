namespace PanoramicData.Blazor.Demo.Pages;

public partial class PDChatPage : IDisposable
{
	[CascadingParameter] protected EventManager? EventManager { get; set; }

	[Inject] private IChatService ChatService { get; set; } = default!;

	// This property will be synced with the global dock mode
	private PDChatDockMode CurrentDockMode
	{
		get => ChatService.PreferredDockMode;
		set => ChatService.PreferredDockMode = value;
	}

	private static ChatMessageSender User => new()
	{
		Name = "Demo User",
		IsUser = true,
		IsHuman = true,
		IsSupport = false
	};

	private static ChatMessageSender Bot => new()
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
		=> mode is PDChatDockMode.Left or PDChatDockMode.Right;

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

	private void SendSuccessMessage()
	{
		var message = new ChatMessage
		{
			Id = Guid.NewGuid(),
			Message = "✅ This is a success message! Everything is working as expected. The global chat now has proper size constraints and works seamlessly across the entire application!",
			Sender = Bot,
			Type = MessageType.Success,
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

	private async Task TestMessagePreview()
	{
		// Force chat to minimized state first
		ChatService.PreferredDockMode = PDChatDockMode.Minimized;
		await Task.Delay(100);

		var message = new ChatMessage
		{
			Id = Guid.NewGuid(),
			Message = "👀 This message demonstrates the toast feature! With the chat closed, it animates in using the configured entry animation and auto-dismisses after the display duration.",
			Sender = Bot,
			Type = MessageType.Normal,
			Timestamp = DateTime.UtcNow
		};
		ChatService.SendMessage(message);
	}

	// Sends three toasts in quick succession with different display durations to demonstrate the
	// stacking behaviour: oldest at the top, each dismissing on its own independent timer.
	private async Task TestToastStack()
	{
		ChatService.PreferredDockMode = PDChatDockMode.Minimized;
		await Task.Delay(100);

		var specs = new (string Text, MessageType Type, double Seconds)[]
		{
			("🥇 First toast — stays 12 seconds (oldest, at the top).", MessageType.Normal, 12),
			("🥈 Second toast — stays 6 seconds.", MessageType.Warning, 6),
			("🥉 Third toast — stays 2 seconds (dismisses first, de-stacking the others).", MessageType.Success, 2),
		};

		foreach (var spec in specs)
		{
			ChatService.SendMessage(new ChatMessage
			{
				Id = Guid.NewGuid(),
				Message = spec.Text,
				Sender = Bot,
				Type = spec.Type,
				Timestamp = DateTime.UtcNow,
				ToastOptions = new ChatToastOptions { DisplayDurationSeconds = spec.Seconds }
			});

			await Task.Delay(300);
		}
	}

	// Sends a single toast that overrides the service defaults on a per-message basis.
	private async Task TestToastOverride()
	{
		ChatService.PreferredDockMode = PDChatDockMode.Minimized;
		await Task.Delay(100);

		ChatService.SendMessage(new ChatMessage
		{
			Id = Guid.NewGuid(),
			Title = "Per-message override",
			Message = "This toast overrides the defaults: it slides in and out, stays for 12 seconds, and uses a wider max-width — regardless of the service-level toast settings.",
			Sender = Bot,
			Type = MessageType.Normal,
			Timestamp = DateTime.UtcNow,
			ToastOptions = new ChatToastOptions
			{
				EntryAnimation = PDChatToastAnimation.Slide,
				ExitAnimation = PDChatToastAnimation.Slide,
				DisplayDurationSeconds = 12,
				AnimationDurationMs = 350,
				MaxWidth = "360px"
			}
		});
	}

	private void SendHtmlMessage()
	{
		var message = new ChatMessage
		{
			Id = Guid.NewGuid(),
			Title = "<strong>HTML</strong> <em>Title</em> This title is longer than the size of the Preview",
			Message = "<p>This is an <strong>HTML</strong> message with a <a href='https://example.com' target='_blank'>link</a> and a list after this text:</p><ul><li>One</li><li>Two</li></ul>",
			IsTitleHtml = true,
			IsMessageHtml = true,
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