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

	private static ChatMessageSender Bot => new()
	{
		Name = "Demo Bot",
		IsUser = false,
		IsHuman = false,
		IsSupport = true
	};

	private readonly Action<PDChatDockMode> _onDockModeChanged;

	public PDChatPage()
	{
		// The new dock mode is read from the service when re-rendering
		_onDockModeChanged = _ => StateHasChanged();
	}

	protected override void OnInitialized()
	{
		// Subscribe to configuration changes to trigger UI updates
		ChatService.OnConfigurationChanged += OnSettingChanged;
		ChatService.OnDockModeChanged += _onDockModeChanged;
	}

	// Configuration, dock mode, restore mode and auto-restore changes are propagated through the
	// service automatically; the page only needs to re-render to reflect them.
	private void OnSettingChanged()
	{
		StateHasChanged();
	}

	// Helper methods to determine dock mode types
	private static bool IsCornerMode(PDChatDockMode mode)
		=> mode is PDChatDockMode.BottomRight or PDChatDockMode.TopRight
				   or PDChatDockMode.BottomLeft or PDChatDockMode.TopLeft;

	private static bool IsSplitMode(PDChatDockMode mode)
		=> mode is PDChatDockMode.Left or PDChatDockMode.Right;

	private const string _welcomeText = "Hello! This is a welcome message from the demo bot. The chat is working perfectly across the entire application!";
	private const string _infoText = "ℹ️ This is an informational message. Corner modes now respect max 30% width and 80% height constraints while maintaining usability. Navigate to other pages to see the chat persist!";
	private const string _warningText = "This is a warning message. Pay attention to important notifications! Try switching to corner modes to see the new dimension constraints in action.";
	private const string _successText = "✅ This is a success message! Everything is working as expected. The global chat now has proper size constraints and works seamlessly across the entire application!";
	private const string _errorText = "This is an error message. Something went wrong in the system. The chat will persist even when you navigate to other demo pages and now has proper size constraints.";
	private const string _criticalText = "🚨 CRITICAL: This is a critical message! Immediate attention required! The restored chat button functionality now works properly when starting from minimized state.";

	private static ChatMessage CreateBotMessage(Guid id, string text, MessageType type) => new()
	{
		Id = id,
		Message = text,
		Sender = Bot,
		Type = type,
		Timestamp = DateTime.UtcNow
	};

	private void SendBotMessage(string text, MessageType type) => ChatService.SendMessage(CreateBotMessage(Guid.NewGuid(), text, type));

	private async Task SendTypingMessage()
	{
		// First show typing indicator
		var typingMessage = CreateBotMessage(Guid.NewGuid(), "Typing...", MessageType.Typing);
		ChatService.SendMessage(typingMessage);

		// Wait a bit and then replace with actual message
		await Task.Delay(2000);

		// Same ID to replace the typing message
		var actualMessage = CreateBotMessage(typingMessage.Id, "Here's the message I was typing! The typing indicator helps show when someone is responding. The global chat now works seamlessly with proper dimension constraints!", MessageType.Normal);
		ChatService.SendMessage(actualMessage);
	}

	private async Task TestAutoRestore()
	{
		// Small delay to ensure auto-restore setting is properly synchronized
		await Task.Delay(100);

		SendBotMessage("🔄 This message was sent to test the auto-restore feature. If auto-restore is enabled and the chat is minimized, it should automatically open when this message arrives. The restored button functionality now works correctly!", MessageType.Normal);
	}

	private async Task TestMessagePreview()
	{
		// Force chat to minimized state first
		ChatService.PreferredDockMode = PDChatDockMode.Minimized;
		await Task.Delay(100);

		SendBotMessage("👀 This message demonstrates the toast feature! With the chat closed, it animates in using the configured entry animation and auto-dismisses after the display duration.", MessageType.Normal);
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
		ChatService.OnConfigurationChanged -= OnSettingChanged;
		ChatService.OnDockModeChanged -= _onDockModeChanged;
		GC.SuppressFinalize(this);
	}
}