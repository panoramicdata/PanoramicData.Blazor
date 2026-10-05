namespace PanoramicData.Blazor.Services;

/// <summary>
/// The Voice Mode and agent picker of <see cref="DumbChatService"/>, so a demo shows both without a speech service.
/// </summary>
public partial class DumbChatService
{
	/// <summary>Gets or sets Voice Mode's endpoints; simulated by default, and null hides Voice Mode.</summary>
	public PDChatVoiceEndpoints? VoiceEndpoints { get; set; } = PDChatVoiceEndpoints.Simulated;

	/// <summary>Gets the agents offered in the input area.</summary>
	public IReadOnlyList<PDChatAgentOption>? Agents { get; } =
	[
		new("dumbbot", "DumbBot", "Answers by keyword, and not very well."),
		new("parrot", "Parrot", "Exactly as helpful as DumbBot, with more feathers."),
	];
}
