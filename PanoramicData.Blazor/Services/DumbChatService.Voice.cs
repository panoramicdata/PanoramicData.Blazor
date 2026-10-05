namespace PanoramicData.Blazor.Services;

/// <summary>
/// The voice controls and the agent and model pickers of <see cref="DumbChatService"/>, so a demo shows the whole input
/// toolbar without a speech service.
/// </summary>
public partial class DumbChatService
{
	/// <summary>Gets or sets the voice endpoints; simulated by default, and null hides the voice controls.</summary>
	public PDChatVoiceEndpoints? VoiceEndpoints { get; set; } = PDChatVoiceEndpoints.Simulated;

	/// <summary>Gets the agents offered in the input toolbar.</summary>
	public IReadOnlyList<PDChatAgentOption>? Agents { get; } =
	[
		new("dumbbot", "DumbBot", "Answers by keyword, and not very well."),
		new("pedant", "Pedant", "Exactly as helpful as DumbBot, but would like to correct your grammar first."),
	];

	/// <summary>Gets the models offered in the input toolbar. The demo answers the same whichever is chosen.</summary>
	public IReadOnlyList<PDChatModelOption>? Models { get; } =
	[
		new("dumb-mini", "Dumb Mini", "Small and quick."),
		new("dumb-max", "Dumb Max", "Large and thorough. Equally dumb."),
	];
}
