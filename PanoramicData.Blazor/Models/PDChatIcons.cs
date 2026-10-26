namespace PanoramicData.Blazor.Models;

/// <summary>
/// The CSS classes of the icons on <see cref="PanoramicData.Blazor.PDChat"/>'s header and toolbar, each rendered as an
/// <c>&lt;i&gt;</c> element. Font Awesome 5 names by default, which Font Awesome 6 also accepts.
/// </summary>
/// <remarks>An empty class shows no icon.</remarks>
public sealed record PDChatIcons
{
	/// <summary>Gets the icon in the agent picker, for an agent with no icon of its own.</summary>
	public string Agent { get; init; } = "fas fa-robot";

	/// <summary>Gets the icon in the model picker.</summary>
	public string Model { get; init; } = "fas fa-microchip";

	/// <summary>Gets the notification sounds button's icon while sounds are on.</summary>
	public string SoundsOn { get; init; } = "fas fa-bell";

	/// <summary>Gets the notification sounds button's icon while sounds are off.</summary>
	public string SoundsOff { get; init; } = "fas fa-bell-slash";

	/// <summary>Gets the read aloud button's icon while answers are read aloud.</summary>
	public string ReadAloudOn { get; init; } = "fas fa-volume-up";

	/// <summary>Gets the read aloud button's icon while answers are not read aloud.</summary>
	public string ReadAloudOff { get; init; } = "fas fa-volume-mute";

	/// <summary>Gets the voice button's icon in Dictate mode. HAL mode shows its eye instead.</summary>
	public string Dictate { get; init; } = "fas fa-microphone";

	/// <summary>Gets the voice button's icon in wake phrase mode.</summary>
	public string Wake { get; init; } = "fas fa-assistive-listening-systems";

	/// <summary>Gets the voice button's icon while the microphone is being opened.</summary>
	public string VoiceStarting { get; init; } = "fas fa-circle-notch fa-spin";

	/// <summary>Gets the badge on the voice button while dictation is paused.</summary>
	public string VoicePaused { get; init; } = "fas fa-pause";

	/// <summary>Gets the attach images button's icon.</summary>
	public string Attach { get; init; } = "fas fa-paperclip";

	/// <summary>Gets the screenshot button's icon.</summary>
	public string Screenshot { get; init; } = "fas fa-camera";

	/// <summary>Gets the Send button's icon.</summary>
	public string Send { get; init; } = "fas fa-paper-plane";

	/// <summary>Gets the header's clear chat button's icon.</summary>
	public string Clear { get; init; } = "fas fa-trash-alt";

	/// <summary>Gets the header's pin button's icon, which docks the chat to the side and, pressed, unpins it.</summary>
	public string Pin { get; init; } = "fas fa-thumbtack";

	/// <summary>Gets the header's full screen button's icon.</summary>
	public string FullScreen { get; init; } = "fas fa-expand";

	/// <summary>Gets the header's button's icon for leaving full screen.</summary>
	public string Restore { get; init; } = "fas fa-compress";

	/// <summary>Gets the header's minimise button's icon.</summary>
	public string Minimize { get; init; } = "fas fa-minus";
}
