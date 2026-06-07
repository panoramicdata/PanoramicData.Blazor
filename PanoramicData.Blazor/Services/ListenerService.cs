namespace PanoramicData.Blazor.Services;

/// <summary>
/// Default implementation of listener fan-out and mode handling.
/// </summary>
public class ListenerService : IListenerService, IDisposable
{
	private readonly Lock _syncLock = new();
	private ListenerConfiguration _configuration = new();
	private Timer? _keywordTimeoutTimer;
	private bool _manualActivationActive;
	private string? _lastErrorCode;
	private string? _lastErrorMessage;

	/// <summary>
	/// Event raised for each listener input item.
	/// </summary>
	public event EventHandler<ListenerInput>? InputReceived;

	/// <summary>
	/// Event raised when listener state changes.
	/// </summary>
	public event EventHandler<ListenerStatusChangedEventArgs>? StatusChanged;

	/// <summary>
	/// Gets the active listener mode.
	/// </summary>
	public ListenerMode Mode => _configuration.Mode;

	/// <summary>
	/// Gets the active listener state.
	/// </summary>
	public ListenerState State { get; private set; } = ListenerState.Idle;

	/// <summary>
	/// Applies listener runtime configuration.
	/// </summary>
	/// <param name="configuration">Configuration to apply.</param>
	public void Configure(ListenerConfiguration configuration)
	{
		lock (_syncLock)
		{
			_configuration = configuration ?? new ListenerConfiguration();
			_manualActivationActive = false;
			StopKeywordTimeoutTimer();
		}

		SetState(Mode == ListenerMode.KeywordActivation ? ListenerState.ActiveAwaitingKeyword : ListenerState.Idle, null, null);
	}

	/// <summary>
	/// Starts manual activation mode.
	/// </summary>
	public void StartListening()
	{
		if (Mode != ListenerMode.ManualActivation)
		{
			return;
		}

		_manualActivationActive = true;
		SetState(ListenerState.Listening, null, null);
		EmitInjectedToken(_configuration.ManualStartToken);
	}

	/// <summary>
	/// Stops manual activation mode.
	/// </summary>
	public void StopListening()
	{
		if (Mode != ListenerMode.ManualActivation)
		{
			return;
		}

		_manualActivationActive = false;
		EmitInjectedToken(_configuration.ManualStopToken);
		SetState(ListenerState.Idle, null, null);
	}

	/// <summary>
	/// Handles recognized text from a listener source.
	/// </summary>
	/// <param name="text">Recognized text.</param>
	/// <param name="timestamp">Recognition timestamp.</param>
	public void HandleRecognizedText(string text, DateTimeOffset timestamp)
	{
		if (string.IsNullOrWhiteSpace(text))
		{
			return;
		}

		text = text.Trim();
		switch (Mode)
		{
			case ListenerMode.ManualActivation:
				if (_manualActivationActive)
				{
					EmitInput(new ListenerInput { Text = text, Timestamp = timestamp });
				}

				break;
			case ListenerMode.KeywordActivation:
				HandleKeywordModeText(text, timestamp);
				break;
			case ListenerMode.Continuous:
				EmitInput(new ListenerInput { Text = text, Timestamp = timestamp });
				SetState(ListenerState.Listening, null, null);
				break;
		}
	}

	/// <summary>
	/// Updates state for source listening started.
	/// </summary>
	public void HandleListeningStarted()
	{
		if (Mode == ListenerMode.KeywordActivation)
		{
			SetState(ListenerState.ActiveAwaitingKeyword, null, null);
			return;
		}

		if (Mode == ListenerMode.ManualActivation)
		{
			SetState(_manualActivationActive ? ListenerState.Listening : ListenerState.Idle, null, null);
			return;
		}

		SetState(ListenerState.Listening, null, null);
	}

	/// <summary>
	/// Updates state for source listening stopped.
	/// </summary>
	public void HandleListeningStopped()
	{
		StopKeywordTimeoutTimer();
		SetState(ListenerState.Idle, null, null);
	}

	/// <summary>
	/// Marks listener as unsupported.
	/// </summary>
	public void HandleUnsupported()
	{
		StopKeywordTimeoutTimer();
		SetState(ListenerState.Unsupported, "unsupported", "Speech recognition is not supported by this browser.");
	}

	/// <summary>
	/// Marks listener as permission denied.
	/// </summary>
	public void HandlePermissionDenied()
	{
		StopKeywordTimeoutTimer();
		SetState(ListenerState.PermissionDenied, "not-allowed", "Microphone permission denied.");
	}

	/// <summary>
	/// Marks listener as failed.
	/// </summary>
	/// <param name="errorCode">Error code from listener source.</param>
	/// <param name="message">Optional message.</param>
	public void HandleError(string? errorCode, string? message)
	{
		StopKeywordTimeoutTimer();
		SetState(ListenerState.Error, errorCode, message);
	}

	public void Dispose()
	{
		StopKeywordTimeoutTimer();
		GC.SuppressFinalize(this);
	}

	private void HandleKeywordModeText(string text, DateTimeOffset timestamp)
	{
		if (State == ListenerState.ActiveAwaitingKeyword)
		{
			if (string.Equals(text, _configuration.Keyword, StringComparison.OrdinalIgnoreCase))
			{
				SetState(ListenerState.Listening, null, null);
				StartKeywordTimeoutTimer();
			}

			return;
		}

		EmitInput(new ListenerInput { Text = text, Timestamp = timestamp });
		StartKeywordTimeoutTimer();
	}

	private void StartKeywordTimeoutTimer()
	{
		if (Mode != ListenerMode.KeywordActivation)
		{
			return;
		}

		if (_configuration.KeywordSilenceTimeout <= TimeSpan.Zero)
		{
			StopKeywordTimeoutTimer();
			return;
		}

		_keywordTimeoutTimer ??= new Timer(OnKeywordTimeout);

		_keywordTimeoutTimer.Change(_configuration.KeywordSilenceTimeout, Timeout.InfiniteTimeSpan);
	}

	private void StopKeywordTimeoutTimer() => _keywordTimeoutTimer?.Change(Timeout.InfiniteTimeSpan, Timeout.InfiniteTimeSpan);

	private void OnKeywordTimeout(object? state)
	{
		if (Mode != ListenerMode.KeywordActivation || State != ListenerState.Listening)
		{
			return;
		}

		SetState(ListenerState.ActiveAwaitingKeyword, null, null);
		EmitInjectedToken(_configuration.KeywordTimeoutToken);
	}

	private void EmitInjectedToken(string? token)
	{
		if (string.IsNullOrWhiteSpace(token))
		{
			return;
		}

		EmitInput(new ListenerInput
		{
			Text = token,
			Timestamp = DateTimeOffset.UtcNow,
			IsInjected = true
		});
	}

	private void EmitInput(ListenerInput input) => InputReceived?.Invoke(this, input);

	private void SetState(ListenerState state, string? errorCode, string? message)
	{
		if (State == state
			&& string.Equals(_lastErrorCode, errorCode, StringComparison.Ordinal)
			&& string.Equals(_lastErrorMessage, message, StringComparison.Ordinal))
		{
			return;
		}

		State = state;
		_lastErrorCode = errorCode;
		_lastErrorMessage = message;
		StatusChanged?.Invoke(this, new ListenerStatusChangedEventArgs
		{
			State = state,
			ErrorCode = errorCode,
			Message = message
		});
	}
}
