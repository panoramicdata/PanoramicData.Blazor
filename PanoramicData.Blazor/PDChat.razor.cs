namespace PanoramicData.Blazor;

/// <summary>
/// A Blazor component that provides a chat interface with support for docking, muting, and message history.
/// </summary>
public partial class PDChat : JSModuleComponentBase
{
	/// <summary>
	/// Gets or sets the chat service used to send and receive messages.
	/// </summary>
	[EditorRequired]
	[Parameter]
	public required IChatService ChatService { get; set; }

	/// <summary>
	/// Gets or sets the sender identity for the current user.
	/// </summary>
	[EditorRequired]
	[Parameter]
	public required ChatMessageSender User { get; set; }

	/// <summary>
	/// Gets or sets an optional conversation history: the list of previous conversations that can be searched,
	/// opened and archived (issue #108). <c>null</c> - the default - means the host has no such store.
	/// </summary>
	/// <remarks>
	/// <para>
	/// <b>Null means the capability is absent, not merely hidden.</b> Without one there is no sidebar, no
	/// conversation tabs and no toolbar, and <see cref="HasConversationHistory"/> is the single test that says
	/// so. A host that supplies nothing gets exactly the chat it has today.
	/// </para>
	/// <para>
	/// A parameter rather than an injected service, matching <see cref="ChatService"/> beside it. Acquiring
	/// the two by different routes would let a host pass a bespoke chat service and silently receive a
	/// conversation store from the container that knows nothing about it - two halves of one conversation,
	/// disagreeing. A host that does keep this in its container passes it through as
	/// <c>ConversationService="@ConversationService"</c>.
	/// </para>
	/// </remarks>
	[Parameter]
	public IChatConversationService? ConversationService { get; set; }

	/// <summary>
	/// Gets a value indicating whether a conversation history is available to show.
	/// </summary>
	/// <remarks>
	/// The single place the question is asked, so that the sidebar, the conversation tabs and the toolbar
	/// cannot end up disagreeing about whether the capability is present - which would show, for instance, a
	/// toolbar whose every control is dead.
	/// </remarks>
	private bool HasConversationHistory => ConversationService is not null;

	/// <summary>
	/// Handed to descendant messages so an inline form can report its outcome (issue #106).
	/// </summary>
	private ChatFormContext FormContext => _formContext ??= new ChatFormContext
	{
		OnSubmitted = OnFormSubmittedAsync,
		OnDismissed = OnFormDismissedAsync
	};

	private ChatFormContext? _formContext;

	/// <summary>
	/// Cascading parameter to get the parent chat container, if any.
	/// When present, dock mode changes will be automatically synchronized.
	/// </summary>
	[CascadingParameter(Name = "ChatContainer")]
	public PDChatContainer? Container { get; set; }

	/// <summary>
	/// Gets or sets the dock position of the chat window.
	/// </summary>
	[Parameter]
	public PDChatDockPosition ChatDockPosition { get; set; } = PDChatDockPosition.Right;

	/// <summary>
	/// Gets or sets the icon to display when the chat window is collapsed.
	/// </summary>
	[Parameter]
	public string CollapsedIcon { get; set; } = "💬";

	/// <summary>
	/// A function to select a user icon for a given message.
	/// </summary>
	[Parameter]
	public Func<ChatMessage, string?>? UserIconSelector { get; set; }

	/// <summary>
	/// A function to select a priority icon for a given message.
	/// </summary>
	[Parameter]
	public Func<ChatMessage, string?>? PriorityIconSelector { get; set; }

	/// <summary>
	/// A function to select a sound to play for a given message.
	/// </summary>
	[Parameter]
	public Func<ChatMessage, string?>? SoundSelector { get; set; }

	/// <summary>
	/// An event callback that is invoked when the chat window is minimized.
	/// </summary>
	[Parameter]
	public EventCallback OnChatMinimized { get; set; }

	/// <summary>
	/// An event callback that is invoked when the chat window is restored.
	/// </summary>
	[Parameter]
	public EventCallback OnChatRestored { get; set; }

	/// <summary>
	/// An event callback that is invoked when the chat window is maximized.
	/// </summary>
	[Parameter]
	public EventCallback OnChatMaximized { get; set; }

	/// <summary>
	/// An event callback that is invoked when the mute setting is toggled.
	/// </summary>
	[Parameter]
	public EventCallback OnMuteToggled { get; set; }

	/// <summary>
	/// An event callback that is invoked when the chat is cleared.
	/// </summary>
	[Parameter]
	public EventCallback OnChatCleared { get; set; }

	/// <summary>
	/// An event callback that is invoked when a message is sent.
	/// </summary>
	[Parameter]
	public EventCallback<ChatMessage> OnMessageSent { get; set; }

	/// <summary>
	/// An event callback that is invoked when a message is received.
	/// </summary>
	[Parameter]
	public EventCallback<ChatMessage> OnMessageReceivedEvent { get; set; }

	/// <summary>
	/// An event callback that is invoked when the chat window is automatically restored.
	/// </summary>
	[Parameter]
	public EventCallback OnAutoRestored { get; set; }

	private bool _isMuted;
	private bool _unreadMessages;
	private MessageType _highestPriorityUnreadMessage = MessageType.Normal;
	private DateTimeOffset _lastReadTimestamp = DateTimeOffset.UtcNow;
	private string _currentInput = "";

	private readonly List<ChatMessage> _messages = [];
	private PDChatDockMode? _restoreDockMode;

	/// <summary>Gets or sets the canvas tab set; set by the markup's <c>@ref</c>.</summary>
	internal PDTabSet? CanvasTabSet { get; set; }

	/// <summary>Gets or sets the message list currently rendered; set by the markup's <c>@ref</c>.</summary>
	internal PDMessages? MessagesComponent { get; set; }

	// The chat service's events carry arguments that these two handlers have no use for: the render reads the
	// live status back from the service, and the singular OnMessageReceived has already handled the message
	// itself. Holding the delegates lets the same instances be unsubscribed on disposal.
	private readonly Action<bool> _onLiveStatusChanged;
	private readonly Action<Guid, ChatMessage> _onConversationMessageReceived;

	/// <summary>
	/// Initializes a new instance of the <see cref="PDChat"/> class.
	/// </summary>
	public PDChat()
	{
		_onLiveStatusChanged = _ => RequestRender();
		_onConversationMessageReceived = (conversationId, _) => OnConversationMessageReceived(conversationId);
	}

	/// <summary>Gets the JavaScript module path for this component.</summary>
	protected override string ModulePath => "./_content/PanoramicData.Blazor/PDChat.razor.js";

	/// <inheritdoc />
	protected override Task OnInitializedAsync()
	{
		// Load existing messages from the service
		_messages.Clear();
		_messages.AddRange(ChatService.Messages);

		// Sync local mute state with service
		_isMuted = ChatService.IsMuted;

		ChatService.OnMessageReceived += OnMessageReceived;

		// Issue #112: a reply arriving for a tab the user is not looking at has to mark that tab rather than
		// append to the one they are. Only the conversation-addressed event says which conversation a message
		// belongs to; the singular one above cannot, which is why this is conditional rather than a
		// replacement for it.
		if (ChatService.SupportsConversations)
		{
			ChatService.OnConversationMessageReceived += _onConversationMessageReceived;
			_selectedConversationId = ChatService.ActiveConversationId;
		}

		ChatService.OnLiveStatusChanged += _onLiveStatusChanged;
		ChatService.OnDockModeChanged += OnServiceDockModeChanged;
		ChatService.OnMuteStatusChanged += OnServiceMuteStatusChanged;
		ChatService.OnConfigurationChanged += OnServiceConfigurationChanged;
		ChatService.Initialize();
		return base.OnInitializedAsync();
	}

	/// <summary>
	/// Change the dock mode state with proper state tracking.
	/// </summary>
	private async Task ChangeDockModeAsync(PDChatDockMode newMode)
	{
		// Only remember genuine corner positions here - _restoreDockMode exists purely so
		// UnpinFromSideAsync can return to "wherever the panel was before it got pinned to a
		// side". Capturing Left/Right too (as this used to) meant a minimize/reopen or
		// fullscreen/restore round-trip while docked would leave it holding the *current*
		// split mode, silently turning the next "Unpin from Side" click back into the exact
		// no-op MS-24840 fixed - reproducibly, not just intermittently.
		if (ChatService.DockMode is PDChatDockMode.TopLeft or PDChatDockMode.TopRight
			or PDChatDockMode.BottomLeft or PDChatDockMode.BottomRight)
		{
			_restoreDockMode = ChatService.DockMode;
		}

		// Notify container if there is one, and let it handle the changes
		if (Container is not null)
		{
			await Container.OnInternalDockModeChanged(newMode);
			return;
		}

		ChatService.DockMode = newMode;

		await InvokeAsync(StateHasChanged);
	}

	private void RequestRender() => _ = InvokeAsync(StateHasChanged);

	private void OnServiceDockModeChanged(PDChatDockMode newDockMode)
		=> _ = ChangeDockModeAsync(newDockMode);

	private void OnServiceMuteStatusChanged(bool isMuted)
	{
		// Sync local mute state with service
		_isMuted = isMuted;
		RequestRender();
	}

	// Configuration changed, trigger UI update and ensure parameters are synchronized
	private void OnServiceConfigurationChanged() => RequestRender();

	// This is an async void method because it is a synchronous event handler for
	// ChatService.OnMessageReceived. Exceptions thrown here cannot be observed by the caller and
	// would otherwise be posted to the renderer's synchronization context, surfacing as Blazor's
	// "An unhandled error has occurred". During circuit teardown (page navigation or a dropped
	// WebSocket) the JS interop calls below throw TaskCanceledException / JSDisconnectedException,
	// so those teardown exceptions are swallowed here rather than crashing the circuit. See MS-24383.
	private async void OnMessageReceived(ChatMessage message)
	{
		try
		{
			await OnMessageReceivedAsync(message);
		}
		catch (Exception ex) when (ex is JSDisconnectedException or OperationCanceledException or ObjectDisposedException)
		{
			// Expected when the Blazor circuit / JS runtime is being torn down; nothing to do.
		}
	}

	private async Task OnMessageReceivedAsync(ChatMessage message)
	{
		var isNewMessage = UpsertMessage(message);

		await SpeakAnswerIfAwaitedAsync(message);

		// Emit OnMessageReceived event for new messages
		if (isNewMessage && OnMessageReceivedEvent.HasDelegate)
		{
			await OnMessageReceivedEvent.InvokeAsync(message);
		}

		if (ChatService.DockMode == PDChatDockMode.Minimized)
		{
			await OnMessageReceivedWhileMinimizedAsync(message, isNewMessage);
		}

		await PlaySoundAsync(message);

		await InvokeAsync(StateHasChanged);
	}

	/// <summary>
	/// Adds a message to the transcript, or updates the copy already there when it shares an id.
	/// </summary>
	/// <returns><c>true</c> if the message is new; <c>false</c> if it updated an existing one.</returns>
	private bool UpsertMessage(ChatMessage message)
	{
		var existing = _messages.FirstOrDefault(m => m.Id == message.Id);
		if (existing is null)
		{
			_messages.Add(message);
			return true;
		}

		existing.Message = message.Message;
		existing.Type = message.Type;
		existing.Title = message.Title;
		existing.Timestamp = message.Timestamp;
		existing.IsTitleHtml = message.IsTitleHtml;
		existing.IsMessageHtml = message.IsMessageHtml;

		// Issue #98: the in-progress fields have to be copied too. This list is hand-maintained,
		// which is a trap - a field added to ChatMessage and not added here is silently dropped on
		// every update after the first, and the symptom is baffling: the title changes, so the
		// update is clearly arriving, while the content it was carrying never appears. That is
		// exactly how these three were first missed.
		existing.ProgressSteps = message.ProgressSteps;
		existing.Thoughts = message.Thoughts;
		existing.PartialMessage = message.PartialMessage;
		existing.ToastOptions = message.ToastOptions;
		// Issue #106. Every field here is copied by hand, so a new payload on ChatMessage that is
		// not added to this list is silently dropped - which has already caught out ProgressSteps
		// and Thoughts once.
		existing.Form = message.Form;
		existing.FormSubmission = message.FormSubmission;

		return false;
	}

	/// <summary>
	/// Marks the minimised chat unread for a message, then restores the chat or shows a toast for it.
	/// </summary>
	private async Task OnMessageReceivedWhileMinimizedAsync(ChatMessage message, bool isNewMessage)
	{
		// A typing indicator is not something to read, so it must not light the badge (#190), nor be
		// restored to or toasted.
		if (message.Type == MessageType.Typing)
		{
			return;
		}

		_unreadMessages = true;

		if (!isNewMessage)
		{
			return;
		}

		UpdateHighestPriorityUnreadMessage();

		// Auto-restore takes priority over toasts: if the chat is going to open anyway there is no
		// point showing a toast for the same message.
		if (ChatService.AutoRestoreOnNewMessage)
		{
			await AutoRestoreAsync();
		}
		else if (ChatService.ToastEnabled)
		{
			// Show the message as an animated toast. This works whether or not the minimized
			// button is visible (MinimizedButtonPosition.None => headless toast).
			await ShowToastAsync(message);
		}
	}

	private async Task AutoRestoreAsync()
	{
		ClearToasts();
		await ChangeDockModeAsync(ChatService.RestoreMode);
		MarkAllRead();

		if (OnAutoRestored.HasDelegate)
		{
			await OnAutoRestored.InvokeAsync();
		}
	}

	private void MarkAllRead()
	{
		_unreadMessages = false;
		_highestPriorityUnreadMessage = MessageType.Normal;
		_lastReadTimestamp = DateTimeOffset.UtcNow;
	}

	private async Task PlaySoundAsync(ChatMessage message)
	{
		var soundUrlString = SoundSelector?.Invoke(message);
		if (string.IsNullOrWhiteSpace(soundUrlString) || _isMuted || Module is null)
		{
			return;
		}

		try
		{
			await Module.InvokeVoidAsync("playSound", soundUrlString).ConfigureAwait(true);
		}
		catch (Exception ex) when (ex is JSDisconnectedException or OperationCanceledException or ObjectDisposedException)
		{
			// The circuit / JS runtime is gone (e.g. the tab was closed). Ignore and continue
			// so the remaining state update is still attempted. See MS-24383.
		}
	}

	private async Task ToggleChatAsync()
	{
		if (ChatService.DockMode == PDChatDockMode.Minimized)
		{
			// Dismiss any toasts when opening chat
			ClearToasts();

			// Restore to last normal state
			await ChangeDockModeAsync(ChatService.RestoreMode);
			MarkAllRead();

			if (OnChatRestored.HasDelegate)
			{
				await OnChatRestored.InvokeAsync();
			}
		}
		else
		{
			// Minimize
			await ChangeDockModeAsync(PDChatDockMode.Minimized);

			if (OnChatMinimized.HasDelegate)
			{
				await OnChatMinimized.InvokeAsync();
			}
		}
	}

	private async Task ToggleMuteAsync()
	{
		// Update both local and service mute state
		_isMuted = !_isMuted;
		ChatService.IsMuted = _isMuted;

		// Emit mute toggle event
		if (OnMuteToggled.HasDelegate)
		{
			await OnMuteToggled.InvokeAsync();
		}
	}

	private async Task ToggleFullScreenAsync()
	{
		if (ChatService.DockMode == PDChatDockMode.FullScreen)
		{
			// Restore to last normal state
			await ChangeDockModeAsync(ChatService.RestoreMode);

			if (OnChatRestored.HasDelegate)
			{
				await OnChatRestored.InvokeAsync();
			}
		}
		else
		{
			// Maximize to fullscreen
			await ChangeDockModeAsync(PDChatDockMode.FullScreen);

			if (OnChatMaximized.HasDelegate)
			{
				await OnChatMaximized.InvokeAsync();
			}
		}
	}

	private async Task ClearChatAsync()
	{
		// Clear messages from both local collection and service
		_messages.Clear();
		ChatService.ClearMessages();
		_currentInput = string.Empty;
		MarkAllRead();
		await InvokeAsync(StateHasChanged);

		// Emit chat cleared event
		if (OnChatCleared.HasDelegate)
		{
			await OnChatCleared.InvokeAsync();
		}
	}

	private async Task DockToSideAsync()
	{
		// Determine which side to dock to based on current position
		await ChangeDockModeAsync(ChatService.DockMode switch
		{
			PDChatDockMode.TopRight or PDChatDockMode.BottomRight => PDChatDockMode.Right,
			PDChatDockMode.TopLeft or PDChatDockMode.BottomLeft => PDChatDockMode.Left,
			_ => PDChatDockMode.Right // Default to right for other cases
		});
	}

	private async Task UnpinFromSideAsync()
	{
		// Restore to wherever the panel was docked before it was pinned to a side. If that was
		// never recorded (e.g. the panel opened directly into a split RestoreMode from Minimized,
		// so there is no prior non-split mode to remember), ChatService.RestoreMode is not a safe
		// fallback here - it commonly *is* the current split mode, which would make this a no-op.
		// Fall back to the corner the minimized button already lives in instead, which is always
		// a corner position and never equal to the current (split) mode.
		await ChangeDockModeAsync(_restoreDockMode ?? GetFallbackCornerDockMode());

		if (OnChatRestored.HasDelegate)
		{
			await OnChatRestored.InvokeAsync();
		}
	}

	private PDChatDockMode GetFallbackCornerDockMode()
		=> ChatService.MinimizedButtonPosition switch
		{
			PDChatButtonPosition.TopLeft => PDChatDockMode.TopLeft,
			PDChatButtonPosition.TopRight => PDChatDockMode.TopRight,
			PDChatButtonPosition.BottomLeft => PDChatDockMode.BottomLeft,
			_ => PDChatDockMode.BottomRight
		};

	private async Task SendCurrentMessageAsync()
	{
		if (string.IsNullOrWhiteSpace(_currentInput))
		{
			return;
		}

		var message = new ChatMessage
		{
			Id = Guid.NewGuid(),
			Message = _currentInput,
			Sender = User,
			Type = MessageType.Normal,
			Timestamp = DateTime.UtcNow
		};

		// Before sending, so a reply that arrives at once is still the one spoken.
		await BeginSpokenExchangeAsync();

		// Fire it off
		ChatService.SendMessage(message);

		// Emit message sent event
		if (OnMessageSent.HasDelegate)
		{
			await OnMessageSent.InvokeAsync(message);
		}

		// Clear input locally
		_currentInput = string.Empty;

		// Clear the PDMessages component's textarea
		MessagesComponent?.ClearInput();
	}

	private bool CanSend => ChatService.IsInputPermitted && ChatService.IsLive && !string.IsNullOrWhiteSpace(_currentInput);

	private void OnTabAdded()
	{
		if (CanvasTabSet is not null)
		{
#pragma warning disable BL0005 // Setting component parameters directly when building tabs programmatically
			var newTab = new PDTab
			{
				Title = "New Tab",
				IsRenamingEnabled = true,
				ChildContent = builder =>
					{
						// PDMonacoEditor already sets the language, theme, value and automatic layout on the
						// options it builds, and InitializeOptions is an Action that adjusts them (#190).
						builder.OpenComponent<PDMonacoEditor>(0);
						builder.AddAttribute(1, nameof(PDMonacoEditor.Language), "csharp");
						builder.AddAttribute(2, nameof(PDMonacoEditor.Theme), "vs-dark");
						builder.AddAttribute(3, nameof(PDMonacoEditor.Value), "// Welcome to the Monaco Editor!\n// Start coding here...\n");
						builder.AddAttribute(4, nameof(PDMonacoEditor.InitializeOptions), new Action<BlazorMonaco.Editor.StandaloneEditorConstructionOptions>(options =>
							options.Minimap = new BlazorMonaco.Editor.EditorMinimapOptions { Enabled = false }));
						builder.CloseComponent();
					}
			};
#pragma warning restore BL0005

			CanvasTabSet.AddTab(newTab);
			CanvasTabSet.StartRenamingTab(newTab);
		}
	}

	/// <inheritdoc />
	public override async ValueTask DisposeAsync()
	{
		await DisposeVoiceAsync();

		// Clean up event handlers
		ChatService.OnMessageReceived -= OnMessageReceived;

		if (ChatService.SupportsConversations)
		{
			ChatService.OnConversationMessageReceived -= _onConversationMessageReceived;
		}
		ChatService.OnLiveStatusChanged -= _onLiveStatusChanged;
		ChatService.OnDockModeChanged -= OnServiceDockModeChanged;
		ChatService.OnMuteStatusChanged -= OnServiceMuteStatusChanged;
		ChatService.OnConfigurationChanged -= OnServiceConfigurationChanged;

		// Clean up toast timers
		ClearToasts();

		await base.DisposeAsync();

		GC.SuppressFinalize(this);
	}

	/// <summary>
	/// Turns a completed form into an ordinary outbound message (issue #106).
	/// </summary>
	/// <remarks>
	/// Deliberately the same path a typed message takes - ChatService.SendMessage plus
	/// OnMessageSent - so a consumer needs no new wiring to receive answers, and the transcript
	/// keeps them in order alongside everything else.
	/// </remarks>
	private async Task OnFormSubmittedAsync(ChatFormSubmission submission)
	{
		var message = new ChatMessage
		{
			Id = Guid.NewGuid(),
			Message = DescribeSubmission(submission),
			Sender = User,
			Type = MessageType.Normal,
			Timestamp = DateTime.UtcNow,
			FormSubmission = submission
		};

		ChatService.SendMessage(message);

		if (OnMessageSent.HasDelegate)
		{
			await OnMessageSent.InvokeAsync(message);
		}

		StateHasChanged();
	}

	/// <summary>
	/// Records that a form was dismissed, without sending anything.
	/// </summary>
	/// <remarks>
	/// Nothing goes to the chat service on purpose. Declining to answer is not a contribution to the
	/// conversation, and a message saying "the user ignored your questions" would invite the asker to
	/// press the point - which is exactly what making the form optional was meant to avoid.
	/// </remarks>
	private Task OnFormDismissedAsync(Guid formId)
	{
		_ = formId;

		StateHasChanged();

		return Task.CompletedTask;
	}

	/// <summary>
	/// Renders a submission as the plain text a human reads in the transcript.
	/// </summary>
	internal static string DescribeSubmission(ChatFormSubmission submission)
	{
		ArgumentNullException.ThrowIfNull(submission);

		var lines = new List<string>();

		foreach (var answer in submission.Answers)
		{
			// Skipped questions are listed rather than omitted, so the reader can see what was asked
			// and declined - the absence of a line would look like the question was never put.
			lines.Add(answer.WasSkipped
				? $"{answer.Question} - skipped"
				: $"{answer.Question} - {answer.Value}");
		}

		return lines.Count == 0
			? "(no answers)"
			: string.Join(Environment.NewLine, lines);
	}
}
