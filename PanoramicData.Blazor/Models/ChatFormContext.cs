namespace PanoramicData.Blazor.Models;

/// <summary>
/// Carries a form's outcome from the message that renders it back up to the chat (issue #106).
/// </summary>
/// <remarks>
/// <para>
/// Cascaded rather than threaded through as parameters. A form is rendered by
/// <c>PDFormMessage</c> inside <c>PDMessage</c> inside <c>PDMessages</c> inside <c>PDChat</c>, and
/// <c>PDChat</c> renders <c>PDMessages</c> in three places for its three dock modes - so a parameter
/// chain would have to be threaded through four components and kept correct in three of them.
/// Cascading is one insertion point and cannot be half-wired.
/// </para>
/// <para>
/// Null-tolerant by design: a <c>PDMessage</c> used outside a <c>PDChat</c> still renders its form,
/// it simply has nowhere to send the answers.
/// </para>
/// </remarks>
public sealed class ChatFormContext
{
	/// <summary>
	/// Called when a form is submitted.
	/// </summary>
	public Func<ChatFormSubmission, Task>? OnSubmitted { get; init; }

	/// <summary>
	/// Called when a form is dismissed unanswered.
	/// </summary>
	public Func<Guid, Task>? OnDismissed { get; init; }
}
