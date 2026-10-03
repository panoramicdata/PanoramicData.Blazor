namespace PanoramicData.Blazor;

/// <summary>
/// The placement of <see cref="PDChat"/> and the badge its minimised button wears for unread messages.
/// </summary>
public partial class PDChat
{
	// Updates the highest priority unread message. Called only as a new, readable message arrives while
	// minimised, so there is always at least one message and the chat is always unread at this point.
	private void UpdateHighestPriorityUnreadMessage()
	{
		// Get the highest priority message that arrived after the last read timestamp
		// and exclude typing messages
		var unreadNonTypingMessages = _messages
			.Where(m => m.Type != MessageType.Typing && m.Timestamp > _lastReadTimestamp)
			.ToList();

		if (unreadNonTypingMessages.Count == 0)
		{
			_highestPriorityUnreadMessage = MessageType.Normal;
			return;
		}

		// Rank by severity explicitly rather than relying on the raw MessageType enum order.
		// The enum's numeric order is not a severity order: Success = 5 is the highest value,
		// so a plain .Max() would let a success message mask an unread warning/error/critical.
		_highestPriorityUnreadMessage = unreadNonTypingMessages
			.Select(m => m.Type)
			.OrderByDescending(GetSeverityRank)
			.First();
	}

	// Maps a message type to a severity rank (higher = more severe) so the minimized badge
	// reflects the worst unread message. Deliberately independent of the MessageType enum's
	// numeric values, which are not ordered by severity (Success is the highest enum value).
	private static int GetSeverityRank(MessageType type) => type switch
	{
		MessageType.Critical => 5,
		MessageType.Error => 4,
		MessageType.Warning => 3,
		MessageType.Normal => 2,
		MessageType.Success => 1,
		_ => 0,
	};

	// Helper method to get the bootstrap color class based on message priority
	private string GetBootstrapColorClass()
	{
		if (!ChatService.IsLive)
		{
			return "pdchat-not-live";
		}

		if (!_unreadMessages)
		{
			return string.Empty;
		}

		return _highestPriorityUnreadMessage switch
		{
			MessageType.Critical => "pdchat-critical",
			MessageType.Error => "pdchat-error",
			MessageType.Warning => "pdchat-warning",
			MessageType.Normal => "pdchat-info",
			MessageType.Success => "pdchat-success",
			_ => string.Empty
		};
	}

	// Helper method to get the animation class based on message priority
	private string GetAnimationClass()
	{
		if (!_unreadMessages)
		{
			return string.Empty;
		}

		return _highestPriorityUnreadMessage switch
		{
			MessageType.Critical => "pulsate-critical",
			MessageType.Error => "pulsate-error",
			MessageType.Warning => "pulsate-warning",
			MessageType.Normal => "pulsate",
			_ => string.Empty
		};
	}

	// Helper method to get the priority indicator icon
	private string GetPriorityIndicator()
	{
		if (!_unreadMessages)
		{
			return string.Empty;
		}

		return _highestPriorityUnreadMessage switch
		{
			MessageType.Warning => "⚠",
			MessageType.Error => "!",
			MessageType.Critical => "!!",
			_ => string.Empty
		};
	}

	// Helper method to get CSS classes for dock mode positioning
	private string GetDockModeClasses()
	{
		// If minimized, always use minimized logic regardless of container state
		if (ChatService.DockMode == PDChatDockMode.Minimized)
		{
			// When minimized, use the service's MinimizedButtonPosition to position the button
			var buttonPositionClass = ChatService.MinimizedButtonPosition switch
			{
				PDChatButtonPosition.TopRight => "dock-top-right",
				PDChatButtonPosition.BottomLeft => "dock-bottom-left",
				PDChatButtonPosition.TopLeft => "dock-top-left",
				PDChatButtonPosition.None => "dock-none", // Hide the button completely
				_ => "dock-bottom-right" // BottomRight, and the fallback
			};
			return $"{buttonPositionClass} dock-minimized";
		}

		// Check if we're in a container that's handling split mode
		if (Container?.IsSplitMode == true && (ChatService.DockMode == PDChatDockMode.Left || ChatService.DockMode == PDChatDockMode.Right))
		{
			return "dock-split-panel";
		}

		return ChatService.DockMode switch
		{
			PDChatDockMode.TopRight => "dock-top-right",
			PDChatDockMode.BottomLeft => "dock-bottom-left",
			PDChatDockMode.TopLeft => "dock-top-left",
			PDChatDockMode.FullScreen => "dock-fullscreen",
			PDChatDockMode.Left => "dock-left",
			PDChatDockMode.Right => "dock-right",
			_ => "dock-bottom-right" // BottomRight, and the fallback
		};
	}
}
