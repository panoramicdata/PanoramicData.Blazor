namespace PanoramicData.Blazor;

/// <summary>
/// The toast surface of <see cref="PDChat"/>: messages that arrive while the chat is closed animate into view in
/// a stack, each with its own auto-dismiss countdown.
/// </summary>
public partial class PDChat
{
	// Fixed duration, in milliseconds, of the "de-stack" animation used when a toast leaves a stack
	// that still contains other toasts. Deliberately uniform and independent of any per-message
	// animation configuration (see the toast stacking rules).
	private const double _deStackDurationMs = 250d;

	private readonly List<ToastItem> _toasts = [];

	/// <summary>
	/// Adds a new toast for the supplied message, resolving per-message overrides against the
	/// service-level defaults, enforcing the visible-toast cap, and starting its auto-dismiss timer.
	/// </summary>
	private async Task ShowToastAsync(ChatMessage message)
	{
		var item = CreateToast(message);

		DismissOldestToastsToMakeRoom();

		// Newest is added at the bottom of the stack (oldest remains at the top).
		_toasts.Add(item);
		await InvokeAsync(StateHasChanged);

		if (item.AutoDismiss && item.DisplayDurationSeconds > 0)
		{
			StartDismissTimer(item, item.DisplayDurationSeconds * 1000d);
		}
	}

	// Resolves a toast's presentation: each per-message option wins over the service-level default.
	private ToastItem CreateToast(ChatMessage message)
	{
		var options = message.ToastOptions ?? new ChatToastOptions();
		var item = new ToastItem { Message = message };
		ApplyToastTiming(item, options);
		ApplyToastAppearance(item, options);
		return item;
	}

	private void ApplyToastTiming(ToastItem item, ChatToastOptions options)
	{
		item.EntryAnimation = options.EntryAnimation ?? ChatService.ToastEntryAnimation;
		item.ExitAnimation = options.ExitAnimation ?? ChatService.ToastExitAnimation;
		item.AnimationDurationMs = options.AnimationDurationMs ?? ChatService.ToastAnimationDurationMs;
		item.AutoDismiss = options.AutoDismiss ?? ChatService.ToastAutoDismiss;
		item.DisplayDurationSeconds = options.DisplayDurationSeconds ?? ChatService.ToastDisplayDurationSeconds;
	}

	private void ApplyToastAppearance(ToastItem item, ChatToastOptions options)
	{
		item.ShowTitle = options.ShowTitle ?? ChatService.ToastShowTitle;
		item.MinWidth = options.MinWidth ?? ChatService.ToastMinWidth;
		item.MaxWidth = options.MaxWidth ?? ChatService.ToastMaxWidth;
		item.MinHeight = options.MinHeight ?? ChatService.ToastMinHeight;
		item.MaxHeight = options.MaxHeight ?? ChatService.ToastMaxHeight;
	}

	// Enforces the visible-toast cap, leaving room for one more, by dismissing the oldest still-present toasts.
	private void DismissOldestToastsToMakeRoom()
	{
		var maxVisible = Math.Max(1, ChatService.ToastMaxVisible);
		var overflow = _toasts.Count(t => !t.IsExiting) - (maxVisible - 1);
		for (var i = 0; overflow > 0 && i < _toasts.Count; i++)
		{
			if (!_toasts[i].IsExiting)
			{
				BeginDismiss(_toasts[i]);
				overflow--;
			}
		}
	}

	// (Re)starts the auto-dismiss timer for a toast, tracking the start time so hover-pause can
	// compute the remaining time accurately. A toast with no time left is dismissed straight away.
	private void StartDismissTimer(ToastItem item, double remainingMs)
	{
		item.DismissTimer?.Dispose();
		item.RemainingMs = remainingMs;
		item.StartedTicks = Environment.TickCount64;
		item.Paused = false;
		item.DismissTimer = new Timer(
			state => _ = InvokeAsync(() => BeginDismiss(item)),
			null,
			TimeSpan.FromMilliseconds(Math.Max(0, remainingMs)),
			Timeout.InfiniteTimeSpan);
	}

	// Pauses a toast's auto-dismiss countdown (e.g. while the pointer hovers over it).
	private static void PauseToast(ToastItem item)
	{
		if (item.IsExiting || item.Paused || item.DismissTimer is null)
		{
			return;
		}

		var elapsed = Environment.TickCount64 - item.StartedTicks;
		item.RemainingMs = Math.Max(0, item.RemainingMs - elapsed);
		item.DismissTimer.Dispose();
		item.DismissTimer = null;
		item.Paused = true;
	}

	// Resumes a paused toast's auto-dismiss countdown from where it left off.
	private void ResumeToast(ToastItem item)
	{
		if (item.IsExiting || !item.Paused)
		{
			return;
		}

		StartDismissTimer(item, item.RemainingMs);
	}

	// Begins the exit of a toast. If other toasts remain in the stack it leaves via the fixed
	// de-stack animation; if it is the last one it uses its own configured exit animation.
	private void BeginDismiss(ToastItem item)
	{
		if (item.IsExiting)
		{
			return;
		}

		item.DismissTimer?.Dispose();
		item.DismissTimer = null;

		var othersRemain = _toasts.Any(t => t != item && !t.IsExiting);
		item.IsExiting = true;
		item.IsDeStacking = othersRemain;

		var exitMs = othersRemain ? _deStackDurationMs : item.AnimationDurationMs;

		item.RemovalTimer?.Dispose();
		item.RemovalTimer = new Timer(
			state => _ = InvokeAsync(() => RemoveToast(item)),
			null,
			TimeSpan.FromMilliseconds(Math.Max(1, exitMs)),
			Timeout.InfiniteTimeSpan);

		RequestRender();
	}

	// Removes a toast once its exit animation has played.
	private void RemoveToast(ToastItem item)
	{
		item.RemovalTimer?.Dispose();
		item.RemovalTimer = null;
		_toasts.Remove(item);
		StateHasChanged();
	}

	// Disposes every toast timer and clears the stack immediately (no exit animation).
	private void ClearToasts()
	{
		foreach (var t in _toasts)
		{
			t.DismissTimer?.Dispose();
			t.RemovalTimer?.Dispose();
		}

		_toasts.Clear();
	}

	// Invoked when a toast is clicked: opens the chat and clears the stack.
	private async Task OnToastClickedAsync()
	{
		ClearToasts();
		await ToggleChatAsync();
	}

	// Maps a message type to its toast colour-scheme class (shared with the legacy preview styles).
	private static string GetToastTypeClass(MessageType type) => type switch
	{
		MessageType.Warning => "preview-warning",
		MessageType.Error => "preview-error",
		MessageType.Critical => "preview-critical",
		MessageType.Success => "preview-success",
		_ => "preview-normal"
	};

	// Builds the full class list for a toast card: colour scheme, anchor side, and the current
	// entry / exit / de-stack animation state.
	private static string GetToastClasses(ToastItem item)
	{
		var animationClass = item.IsDeStacking
			? "toast-destack"
			: item.IsExiting
				? $"toast-exit-{GetAnimationName(item.ExitAnimation)}"
				: $"toast-enter-{GetAnimationName(item.EntryAnimation)}";

		return $"{GetToastTypeClass(item.Message.Type)} {animationClass}";
	}

	// Inline style carrying the resolved dimensions and the per-toast animation duration.
	private static string GetToastStyle(ToastItem item)
	{
		var durationMs = item.IsDeStacking ? _deStackDurationMs : item.AnimationDurationMs;
		var sb = new StringBuilder();
		sb.Append("--pdchat-toast-anim-ms:").Append(durationMs.ToString(CultureInfo.InvariantCulture)).Append("ms;");
		AppendStyle(sb, "min-width", item.MinWidth);
		AppendStyle(sb, "max-width", item.MaxWidth);
		AppendStyle(sb, "min-height", item.MinHeight);
		AppendStyle(sb, "max-height", item.MaxHeight);
		return sb.ToString();
	}

	private static void AppendStyle(StringBuilder sb, string name, string? value)
	{
		if (!string.IsNullOrWhiteSpace(value))
		{
			sb.Append(name).Append(':').Append(value).Append(';');
		}
	}

	// Grow is both the default entry animation and the fallback for an unrecognised value.
	private static string GetAnimationName(PDChatToastAnimation animation) => animation switch
	{
		PDChatToastAnimation.None => "none",
		PDChatToastAnimation.Fade => "fade",
		PDChatToastAnimation.Shrink => "shrink",
		PDChatToastAnimation.Slide => "slide",
		_ => "grow"
	};

	// Resolves the corner the toast stack anchors to: follow the minimized button when it is shown,
	// otherwise fall back to the configured headless anchor. Bottom right is the default, and also what
	// a headless anchor of None means.
	private string GetToastAnchorClass()
	{
		var anchor = ChatService.MinimizedButtonPosition == PDChatButtonPosition.None
			? ChatService.ToastAnchor
			: ChatService.MinimizedButtonPosition;

		return anchor switch
		{
			PDChatButtonPosition.TopLeft => "toast-anchor-top-left",
			PDChatButtonPosition.TopRight => "toast-anchor-top-right",
			PDChatButtonPosition.BottomLeft => "toast-anchor-bottom-left",
			_ => "toast-anchor-bottom-right"
		};
	}

	// aria-live politeness: escalate to assertive for the most severe message types.
	private static string GetToastAriaLive(MessageType type)
		=> type is MessageType.Error or MessageType.Critical ? "assertive" : "polite";

	/// <summary>
	/// Represents a single live toast instance in the stack, together with its resolved presentation
	/// options and auto-dismiss timers.
	/// </summary>
	private sealed class ToastItem
	{
		public Guid Key { get; } = Guid.NewGuid();
		public required ChatMessage Message { get; init; }
		public PDChatToastAnimation EntryAnimation { get; set; }
		public PDChatToastAnimation ExitAnimation { get; set; }
		public double AnimationDurationMs { get; set; }
		public bool AutoDismiss { get; set; }
		public double DisplayDurationSeconds { get; set; }
		public bool ShowTitle { get; set; }
		public string? MinWidth { get; set; }
		public string? MaxWidth { get; set; }
		public string? MinHeight { get; set; }
		public string? MaxHeight { get; set; }

		public bool IsExiting { get; set; }
		public bool IsDeStacking { get; set; }
		public Timer? DismissTimer { get; set; }
		public Timer? RemovalTimer { get; set; }
		public bool Paused { get; set; }
		public double RemainingMs { get; set; }
		public long StartedTicks { get; set; }
	}
}
