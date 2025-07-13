namespace PanoramicData.Blazor.Demo.Shared;

public partial class MainLayout
{
	private readonly EventManager _eventManager = new();

	[Inject] private IChatService ChatService { get; set; } = default!;

	private ChatMessageSender User => new()
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
			: "/_content/PanoramicData.Blazor.Demo/sounds/" + chatMessage.Type switch
			{
				MessageType.Normal => "tick.mp3",
				MessageType.Warning => "warning.mp3",
				MessageType.Error => "error.mp3",
				MessageType.Critical => "critical.mp3",
				_ => null
			};

	private PDChatDockMode _currentDockMode = PDChatDockMode.Minimized;

	protected override void OnInitialized()
	{
		// Subscribe to configuration changes to trigger UI updates
		ChatService.OnConfigurationChanged += OnConfigurationChanged;
		ChatService.OnDockModeChanged += OnServiceDockModeChanged;

		// Always start minimized, but ensure service has a valid restore mode
		if (!IsNormalDockMode(ChatService.RestoreMode))
		{
			ChatService.RestoreMode = PDChatDockMode.BottomRight;
		}

		// Keep the current dock mode as minimized regardless of service preference
		_currentDockMode = PDChatDockMode.Minimized;
	}

	private void OnConfigurationChanged()
	{
		StateHasChanged();
	}

	private void OnServiceDockModeChanged(PDChatDockMode newMode)
	{
		// Only update if it's not a change to minimized (which we control here)
		if (newMode != PDChatDockMode.Minimized)
		{
			_currentDockMode = newMode;
			StateHasChanged();
		}
	}

	// Handle dock mode changes from the chat component
	private void OnDockModeChanged(PDChatDockMode newMode)
	{
		_currentDockMode = newMode;

		// Don't automatically update restore mode here - let the PDChat component manage it
		// The PDChat component will set RestoreMode when appropriate (e.g., during pin operations)

		StateHasChanged();
	}

	// Helper method to determine if current dock mode requires split layout
	private bool IsSplitMode() => _currentDockMode is PDChatDockMode.Left or PDChatDockMode.Right;

	// Helper method to check if a dock mode is a "normal" docked position (not minimized or fullscreen)
	private bool IsNormalDockMode(PDChatDockMode mode)
		=> mode != PDChatDockMode.Minimized && mode != PDChatDockMode.FullScreen;

	// Get a component key that only changes when layout fundamentally changes
	private string GetChatComponentKey()
	{
		// Only recreate component when switching between these fundamental layout types:
		// - Split modes (Left, Right, Top, Bottom)
		// - Non-split modes (corners, fullscreen, minimized)
		// This preserves message history during dock mode changes within the same layout type
		return IsSplitMode() ? "split-layout" : "overlay-layout";
	}

	public void Dispose()
	{
		ChatService.OnConfigurationChanged -= OnConfigurationChanged;
		ChatService.OnDockModeChanged -= OnServiceDockModeChanged;
	}
}
