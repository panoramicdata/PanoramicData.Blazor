namespace PanoramicData.Blazor.Models;

/// <summary>
/// Event payload for listener status transitions.
/// </summary>
public class ListenerStatusChangedEventArgs : EventArgs
{
	/// <summary>
	/// Gets or sets the current listener state.
	/// </summary>
	public ListenerState State { get; set; }

	/// <summary>
	/// Gets or sets an optional error code.
	/// </summary>
	public string? ErrorCode { get; set; }

	/// <summary>
	/// Gets or sets an optional descriptive message.
	/// </summary>
	public string? Message { get; set; }
}
