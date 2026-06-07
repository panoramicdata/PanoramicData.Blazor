namespace PanoramicData.Blazor.Enums;

/// <summary>
/// Represents the current runtime state of the listener pipeline.
/// </summary>
public enum ListenerState
{
	/// <summary>
	/// Listener is idle and not actively processing speech.
	/// </summary>
	Idle,

	/// <summary>
	/// Listener is actively processing speech input.
	/// </summary>
	Listening,

	/// <summary>
	/// Listener is active but waiting for the keyword before emitting user speech.
	/// </summary>
	ActiveAwaitingKeyword,

	/// <summary>
	/// Browser speech recognition is unavailable.
	/// </summary>
	Unsupported,

	/// <summary>
	/// Microphone permission was denied.
	/// </summary>
	PermissionDenied,

	/// <summary>
	/// Listener encountered an error.
	/// </summary>
	Error
}
