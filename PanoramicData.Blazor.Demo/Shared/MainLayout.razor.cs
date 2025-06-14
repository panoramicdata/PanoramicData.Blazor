namespace PanoramicData.Blazor.Demo.Shared;

public partial class MainLayout
{
	private readonly EventManager _eventManager = new();

	[Inject] private IChatService ChatService { get; set; } = default!;

	private string GetUserIcon(ChatMessage chatMessage)
	{
		return chatMessage.Sender switch
		{
			"User" => "🙋",
			string x when x.EndsWith("Bot") => "🤖",
			_ => "👤"
		};
	}

	private string GetPriorityIcon(ChatMessage chatMessage)
	{
		return chatMessage.Priority switch
		{
			MessagePriority.Normal => "ℹ️",
			MessagePriority.Warning => "⚠️",
			MessagePriority.High => "❗",
			MessagePriority.Critical => "🚨",
			_ => "❓"
		};
	}
}
