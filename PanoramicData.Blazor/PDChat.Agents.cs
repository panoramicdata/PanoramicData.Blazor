namespace PanoramicData.Blazor;

/// <summary>
/// PDChat: the agent picker in the input area, offered when the chat service lists two or more
/// <see cref="IChatService.Agents"/>.
/// </summary>
public partial class PDChat
{
	private bool IsAgentPickerOffered => ChatService.Agents is { Count: > 1 };

	// Null or unknown means the host's default, which the picker shows as its first agent.
	private PDChatAgentOption? SelectedAgent => ChatService.Agents?.FirstOrDefault(agent => agent.Id == ChatService.SelectedAgentId)
		?? (ChatService.Agents is { Count: > 0 } agents ? agents[0] : null);

	private RenderFragment? InputAccessories => IsVoiceModeOffered || IsAgentPickerOffered ? InputAccessoriesContent : null;

	private void OnAgentSelected(ChangeEventArgs args)
	{
		var id = args.Value?.ToString();
		if (ChatService.Agents?.Any(agent => agent.Id == id) == true)
		{
			ChatService.SelectedAgentId = id;
		}
	}
}
