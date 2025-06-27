namespace PanoramicData.Blazor.Models;

public class ChatMessageSender
{
	public required string Name { get; init; }

	public bool IsUser { get; init; }

	public bool IsHuman { get; init; }

	public bool IsSupport { get; init; }
}