using System;
using System.Runtime.CompilerServices;

namespace PanoramicData.Blazor.Interfaces;

/// <summary>
/// Per-instance storage behind the settable default interface members of <see cref="IChatService"/>.
/// </summary>
/// <remarks>
/// An interface cannot declare instance fields, so a default implementation of a settable property has nowhere
/// of its own to keep the value. Each service instance that relies on the defaults is given one of these,
/// held weakly so that it lives exactly as long as the service does. A service that implements a member itself
/// never touches its entry here.
/// </remarks>
internal sealed class ChatServiceDefaultState
{
	private static readonly ConditionalWeakTable<IChatService, ChatServiceDefaultState> _states = [];

	/// <summary>Gets the default-member state for a service, creating it on first use.</summary>
	/// <param name="service">The service whose defaults are being read or written.</param>
	public static ChatServiceDefaultState For(IChatService service) => _states.GetValue(service, _ => new ChatServiceDefaultState());

	public bool IsInputPermitted { get; set; } = true;

	public string? InputDisabledMessage { get; set; }

	public PDChatToastAnimation ToastEntryAnimation { get; set; } = PDChatToastAnimation.Grow;

	public PDChatToastAnimation ToastExitAnimation { get; set; } = PDChatToastAnimation.Shrink;

	public double ToastAnimationDurationMs { get; set; } = 250d;

	public bool ToastAutoDismiss { get; set; } = true;

	public bool ToastShowTitle { get; set; } = true;

	public string ToastMinWidth { get; set; } = "200px";

	public string ToastMaxWidth { get; set; } = "300px";

	public string ToastMinHeight { get; set; } = string.Empty;

	public string ToastMaxHeight { get; set; } = string.Empty;

	public int ToastMaxVisible { get; set; } = 5;

	public PDChatButtonPosition ToastAnchor { get; set; } = PDChatButtonPosition.BottomRight;

	/// <summary>
	/// Gets or sets the handlers subscribed to the default <see cref="IChatService.OnConversationMessageReceived"/>.
	/// They are kept so that subscribing and unsubscribing behave as they would on any event, but a service that
	/// does not support conversations never raises it.
	/// </summary>
	public Action<Guid, ChatMessage>? ConversationMessageReceived { get; set; }
}
