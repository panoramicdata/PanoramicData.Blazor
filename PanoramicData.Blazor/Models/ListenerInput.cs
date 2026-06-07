namespace PanoramicData.Blazor.Models;

/// <summary>
/// Represents a timestamped listener input item.
/// </summary>
public class ListenerInput
{
	/// <summary>
	/// Gets or sets the captured or injected text.
	/// </summary>
	public string Text { get; set; } = string.Empty;

	/// <summary>
	/// Gets or sets when this input item was created.
	/// </summary>
	public DateTimeOffset Timestamp { get; set; } = DateTimeOffset.UtcNow;

	/// <summary>
	/// Gets or sets whether this entry was injected by listener logic.
	/// </summary>
	public bool IsInjected { get; set; }
}
