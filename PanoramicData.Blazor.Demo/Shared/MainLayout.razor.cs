namespace PanoramicData.Blazor.Demo.Shared;

public partial class MainLayout
{
	private readonly EventManager _eventManager = new();

	[Inject] private IChatService ChatService { get; set; } = default!;

	private ChatMessageSender User => new ChatMessageSender
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
}
