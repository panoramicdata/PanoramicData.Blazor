namespace PanoramicData.Blazor;

/// <summary>
/// PDChat: the thin toolbar above the text box, holding the agent and model pickers (each offered only when the chat
/// service lists two or more), notifications and Voice Mode.
/// </summary>
public partial class PDChat
{
	private bool IsAgentPickerOffered => ChatService.Agents is { Count: > 1 };

	private bool IsModelPickerOffered => ChatService.Models is { Count: > 1 };

	// Null or unknown means the host's default, which each picker shows as its first option.
	private PDChatAgentOption? SelectedAgent => ChatService.Agents?.FirstOrDefault(agent => agent.Id == ChatService.SelectedAgentId)
		?? (ChatService.Agents is { Count: > 0 } agents ? agents[0] : null);

	private PDChatModelOption? SelectedModel => ChatService.Models?.FirstOrDefault(model => model.Id == ChatService.SelectedModelId)
		?? (ChatService.Models is { Count: > 0 } models ? models[0] : null);

	private string MuteButtonTitle => _isMuted ? "Notification sounds: off" : "Notification sounds: on";

	private void OnAgentSelected(ChangeEventArgs args)
	{
		var id = args.Value?.ToString();
		if (ChatService.Agents?.Any(agent => agent.Id == id) == true)
		{
			ChatService.SelectedAgentId = id;
		}
	}

	private void OnModelSelected(ChangeEventArgs args)
	{
		var id = args.Value?.ToString();
		if (ChatService.Models?.Any(model => model.Id == id) == true)
		{
			ChatService.SelectedModelId = id;
		}
	}
}
