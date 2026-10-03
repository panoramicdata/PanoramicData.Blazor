using System;

namespace PanoramicData.Blazor.Services;

/// <summary>
/// Demo chat service that simulates responses and online/offline behavior.
/// </summary>
public partial class DumbChatService : IChatService, IDisposable
{
	private PDChatDockMode _preferredDockMode = PDChatDockMode.Right;

	/// <inheritdoc />
	public event Action<ChatMessage>? OnMessageReceived;

	/// <inheritdoc />
	public event Action<Guid, ChatMessage>? OnConversationMessageReceived;

	/// <inheritdoc />
	public event Action<PDChatDockMode>? OnDockModeChanged;

	/// <inheritdoc />
	public PDChatDockMode PreferredDockMode
	{
		get => _preferredDockMode;
		set
		{
			if (_preferredDockMode != value)
			{
				_preferredDockMode = value;
				OnDockModeChanged?.Invoke(value);
			}
		}
	}

	/// <summary>
	/// Gets the predefined sender used for automated demo replies.
	/// </summary>
	public static ChatMessageSender DumbBot { get; } = new()
	{
		Name = "DumbBot",
		IsUser = false,
		IsHuman = false,
		IsSupport = false
	};

	/// <inheritdoc />
	public PDChatDockMode DockMode { get; set; }

	/// <inheritdoc />
	public void Dispose()
	{
		_timer?.Dispose();
		GC.SuppressFinalize(this);
	}

	/// <summary>
	/// Records a message against one conversation and tells subscribers about it.
	/// </summary>
	/// <param name="conversationId">The conversation the message belongs to.</param>
	/// <param name="chatMessage">The message.</param>
	/// <remarks>
	/// <para>
	/// The single place a message is delivered, so that the conversation it lands in and the conversation it
	/// is announced against cannot disagree. Every path that used to do
	/// <c>_messages.Add(x); OnMessageReceived?.Invoke(x);</c> goes through here instead - and there were nine
	/// of them, which is exactly how a reply ends up in the wrong transcript when one is edited and the others
	/// are not.
	/// </para>
	/// <para>
	/// <see cref="OnConversationMessageReceived"/> is raised for every message, whichever conversation it
	/// belongs to. <see cref="OnMessageReceived"/> is raised only for the active one, because a consumer using
	/// the singular API has no way to tell which conversation it was handed and would append a background
	/// conversation's reply to whatever it is showing.
	/// </para>
	/// </remarks>
	private void Deliver(Guid conversationId, ChatMessage chatMessage)
	{
		EnsureConversation(conversationId);
		var messages = _conversations[conversationId];

		// A message id is an identity, not a sequence number: a "Typing..." placeholder and the answer that
		// replaces it deliberately share one, so that a consumer can swap the first for the second. The
		// service therefore has to replace in place too. Appending both left the placeholder in the stored
		// transcript for ever - invisible while PDChat kept its own de-duplicated copy, and plainly wrong the
		// moment a conversation tab read the transcript back from here.
		var existing = messages.FirstOrDefault(message => message.Id == chatMessage.Id);
		if (existing is null)
		{
			messages.Add(chatMessage);
		}
		else
		{
			existing.Message = chatMessage.Message;
			existing.Type = chatMessage.Type;
			existing.Title = chatMessage.Title;
			existing.Timestamp = chatMessage.Timestamp;
			existing.IsTitleHtml = chatMessage.IsTitleHtml;
			existing.IsMessageHtml = chatMessage.IsMessageHtml;
			// Issue #106: copied by hand like the rest, so a new payload must be added here too.
			existing.Form = chatMessage.Form;
			existing.FormSubmission = chatMessage.FormSubmission;
		}

		OnConversationMessageReceived?.Invoke(conversationId, chatMessage);

		if (conversationId == _activeConversationId)
		{
			OnMessageReceived?.Invoke(chatMessage);
		}
	}

	/// <inheritdoc />
	public void SendMessage(ChatMessage chatMessage) => SendMessage(_activeConversationId, chatMessage);

	/// <inheritdoc />
	/// <remarks>
	/// Throws for a conversation this service does not have, rather than falling back to the active one. A
	/// caller addressing a conversation that does not exist has a bug, and it should surface where it happened
	/// rather than as a message appearing in an unrelated conversation several seconds later.
	/// </remarks>
	public void SendMessage(Guid conversationId, ChatMessage chatMessage)
	{
		if (!_conversations.ContainsKey(conversationId))
		{
			throw new InvalidOperationException(
				$"This chat service does not have conversation {conversationId}. " +
				$"Create it with {nameof(CreateConversation)} or {nameof(EnsureConversation)} first.");
		}

		// Record the user's message and tell subscribers immediately. Deliver handles the add-or-update, so
		// there is one implementation of what a repeated message id means rather than two that can disagree.
		Deliver(conversationId, chatMessage);

		// Kick off the async reply workflow, on the conversation the message was sent to. Threading the id
		// through is what makes a slow reply arrive back where it was asked for: without it, a reply landed
		// wherever the user happened to be looking by the time it finished, which is precisely the case
		// conversation tabs make visible.
		_ = RespondAsync(conversationId, userMessage: chatMessage);
	}

	/// <summary>
	/// Updates the dock mode and raises the dock-mode-changed event when needed.
	/// </summary>
	/// <param name="newMode">The requested dock mode.</param>
	/// <returns>A completed task.</returns>
	public Task SetDockModeAsync(PDChatDockMode newMode)
	{
		if (DockMode == newMode)
		{
			return Task.CompletedTask;
		}

		DockMode = newMode;
		OnDockModeChanged?.Invoke(DockMode);
		return Task.CompletedTask;
	}
}
