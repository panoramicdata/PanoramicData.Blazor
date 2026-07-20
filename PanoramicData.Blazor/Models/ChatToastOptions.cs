namespace PanoramicData.Blazor.Models;

/// <summary>
/// Per-message overrides for the <see cref="PanoramicData.Blazor.PDChat"/> toast behaviour.
/// Every property is nullable: a <c>null</c> value means "fall back to the service default"
/// (the corresponding <c>Toast*</c> property on <see cref="Interfaces.IChatService"/>).
/// Attach an instance to <see cref="ChatMessage.ToastOptions"/> to override the toast shown for that message.
/// </summary>
public class ChatToastOptions
{
	/// <summary>Overrides the animation used when the toast appears. Default entry is <see cref="PDChatToastAnimation.Grow"/>.</summary>
	public PDChatToastAnimation? EntryAnimation { get; set; }

	/// <summary>Overrides the animation used when the toast is dismissed. Default exit is <see cref="PDChatToastAnimation.Shrink"/>.</summary>
	public PDChatToastAnimation? ExitAnimation { get; set; }

	/// <summary>Overrides the duration, in milliseconds, of the entry / exit transitions.</summary>
	public double? AnimationDurationMs { get; set; }

	/// <summary>Overrides whether the toast automatically dismisses after <see cref="DisplayDurationSeconds"/>.</summary>
	public bool? AutoDismiss { get; set; }

	/// <summary>Overrides how long, in seconds, the toast stays on screen before auto-dismissing.</summary>
	public double? DisplayDurationSeconds { get; set; }

	/// <summary>Overrides whether the message title is shown in the toast.</summary>
	public bool? ShowTitle { get; set; }

	/// <summary>Overrides the toast minimum width (any valid CSS length, e.g. "200px").</summary>
	public string? MinWidth { get; set; }

	/// <summary>Overrides the toast maximum width (any valid CSS length, e.g. "300px").</summary>
	public string? MaxWidth { get; set; }

	/// <summary>Overrides the toast minimum height (any valid CSS length).</summary>
	public string? MinHeight { get; set; }

	/// <summary>Overrides the toast maximum height (any valid CSS length).</summary>
	public string? MaxHeight { get; set; }
}
