namespace PanoramicData.Blazor.Demo.Shared;

public partial class MainLayout : LayoutComponentBase, IDisposable
{
	private readonly EventManager _eventManager = new();

	[Inject] private IChatService ChatService { get; set; } = default!;

	private static ChatMessageSender User => new()
	{
		Name = "User",
		IsUser = true,
		IsHuman = true,
		IsSupport = false
	};

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
			: "_content/PanoramicData.Blazor.Demo/sounds/" + chatMessage.Type switch
			{
				MessageType.Normal => "tick.mp3",
				MessageType.Warning => "warning.mp3",
				MessageType.Error => "error.mp3",
				MessageType.Critical => "critical.mp3",
				_ => null
			};

	protected override void OnInitialized()
	{
		// Subscribe to configuration changes to trigger UI updates
		ChatService.OnConfigurationChanged += OnConfigurationChanged;

		// Always start minimized, but ensure service has a valid restore mode
		if (!IsNormalDockMode(ChatService.RestoreMode))
		{
			ChatService.RestoreMode = PDChatDockMode.BottomRight;
		}
	}

	private void OnConfigurationChanged()
	{
		StateHasChanged();
	}

	// Optional dock mode change handler - for monitoring purposes only
	// The PDChatContainer now handles all dock mode synchronization automatically!
	private static void OnDockModeChanged(PDChatDockMode newMode)
	{
		// This is now optional - you could log analytics, show notifications, etc.
		// The container handles all the actual state management internally
		Console.WriteLine($"Chat dock mode changed to: {newMode}");
	}

	// Helper method to check if a dock mode is a "normal" docked position (not minimized or fullscreen)
	private static bool IsNormalDockMode(PDChatDockMode mode)
		=> mode != PDChatDockMode.Minimized && mode != PDChatDockMode.FullScreen;

	public void Dispose()
	{
		ChatService.OnConfigurationChanged -= OnConfigurationChanged;
		GC.SuppressFinalize(this);
	}
}
