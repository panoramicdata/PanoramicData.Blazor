namespace PanoramicData.Blazor;

/// <summary>
/// A Blazor component that provides voice recognition input via the browser's speech recognition API.
/// </summary>
public partial class PDVoiceListener : IAsyncDisposable
{
	private DotNetObjectReference<PDVoiceListener>? _dotNetObjectReference;
	private IJSObjectReference? _module;
	private bool _isStarted;
	private ListenerConfiguration? _lastConfiguration;
	private IListenerService? _lastConfiguredService;

	/// <summary>
	/// Gets or sets optional child content.
	/// </summary>
	[Parameter] public RenderFragment? ChildContent { get; set; }

	/// <summary>
	/// Gets or sets the listener mode.
	/// </summary>
	[Parameter] public ListenerMode Mode { get; set; } = ListenerMode.ManualActivation;

	/// <summary>
	/// Gets or sets the keyword used for keyword activation mode.
	/// </summary>
	[Parameter] public string Keyword { get; set; } = string.Empty;

	/// <summary>
	/// Gets or sets timeout after silence before keyword is required again.
	/// </summary>
	[Parameter] public TimeSpan KeywordSilenceTimeout { get; set; } = TimeSpan.FromSeconds(3);

	/// <summary>
	/// Gets or sets optional token emitted when keyword mode times out.
	/// </summary>
	[Parameter] public string? KeywordTimeoutToken { get; set; }

	/// <summary>
	/// Gets or sets optional manual start token.
	/// </summary>
	[Parameter] public string? ManualStartToken { get; set; }

	/// <summary>
	/// Gets or sets optional manual stop token.
	/// </summary>
	[Parameter] public string? ManualStopToken { get; set; }

	/// <summary>
	/// Gets or sets whether listener should run in background for active modes.
	/// </summary>
	[Parameter] public bool RunInBackground { get; set; } = true;

	/// <summary>
	/// Gets or sets whether recognition should auto start for non-manual modes.
	/// </summary>
	[Parameter] public bool AutoStart { get; set; } = true;

	/// <summary>
	/// Gets or sets an explicit listener service instance.
	/// </summary>
	[Parameter] public IListenerService? ListenerService { get; set; }

	[Inject] private IListenerService DefaultListenerService { get; set; } = null!;

	/// <summary>
	/// Gets the injected JavaScript runtime.
	/// </summary>
	[Inject] public IJSRuntime JSRuntime { get; set; } = null!;

	private IListenerService ActiveListenerService => ListenerService ?? DefaultListenerService;

	/// <inheritdoc />
	protected override async Task OnParametersSetAsync()
	{
		ConfigureServiceIfChanged();
		if (_module != null)
		{
			await _module.InvokeVoidAsync("configure", new
			{
				mode = Mode.ToString(),
				runInBackground = RunInBackground
			}).ConfigureAwait(true);
			await SyncListeningStateAsync().ConfigureAwait(true);
		}
	}

	/// <inheritdoc />
	protected override async Task OnAfterRenderAsync(bool firstRender)
	{
		if (!firstRender || JSRuntime is null)
		{
			return;
		}

		_dotNetObjectReference = DotNetObjectReference.Create(this);
		_module = await JSRuntime.InvokeAsync<IJSObjectReference>("import", "./_content/PanoramicData.Blazor/PDVoiceListener.razor.js").ConfigureAwait(true);
		await _module.InvokeVoidAsync("initialize", _dotNetObjectReference, new
		{
			mode = Mode.ToString(),
			runInBackground = RunInBackground
		}).ConfigureAwait(true);

		await SyncListeningStateAsync().ConfigureAwait(true);
	}

	/// <summary>
	/// Starts manual mode listening.
	/// </summary>
	public async Task StartAsync()
	{
		ActiveListenerService.StartListening();
		if (_module != null && !_isStarted)
		{
			await _module.InvokeVoidAsync("startListening").ConfigureAwait(true);
			_isStarted = true;
		}
	}

	/// <summary>
	/// Stops manual mode listening.
	/// </summary>
	public async Task StopAsync()
	{
		ActiveListenerService.StopListening();
		if (_module != null && _isStarted)
		{
			await _module.InvokeVoidAsync("stopListening").ConfigureAwait(true);
			_isStarted = false;
		}
	}

	/// <summary>
	/// Called from JavaScript when voice recognition produces text.
	/// </summary>
	/// <param name="text">The recognised text.</param>
	/// <param name="timestamp">The ISO-8601 timestamp of the recognition event.</param>
	[JSInvokable]
	public void OnRecognizedText(string text, string timestamp)
	{
		DateTimeOffset parsedTimestamp = DateTimeOffset.UtcNow;
		if (!string.IsNullOrWhiteSpace(timestamp) && DateTimeOffset.TryParse(timestamp, out DateTimeOffset value))
		{
			parsedTimestamp = value;
		}

		ActiveListenerService.HandleRecognizedText(text, parsedTimestamp);
	}

	/// <summary>
	/// Called from JavaScript when voice listening starts.
	/// </summary>
	[JSInvokable]
	public void OnListeningStarted() => ActiveListenerService.HandleListeningStarted();

	/// <summary>
	/// Called from JavaScript when voice listening stops.
	/// </summary>
	[JSInvokable]
	public void OnListeningStopped() => ActiveListenerService.HandleListeningStopped();

	/// <summary>
	/// Called from JavaScript when speech recognition is not supported by the browser.
	/// </summary>
	[JSInvokable]
	public void OnUnsupported() => ActiveListenerService.HandleUnsupported();

	/// <summary>
	/// Called from JavaScript when microphone permission is denied.
	/// </summary>
	[JSInvokable]
	public void OnPermissionDenied() => ActiveListenerService.HandlePermissionDenied();

	/// <summary>
	/// Called from JavaScript when a voice listener error occurs.
	/// </summary>
	/// <param name="errorCode">The error code from the speech recognition API.</param>
	/// <param name="message">A human-readable error message.</param>
	[JSInvokable]
	public void OnListenerError(string? errorCode, string? message) => ActiveListenerService.HandleError(errorCode, message);

	/// <inheritdoc />
	public async ValueTask DisposeAsync()
	{
		try
		{
			if (_module != null)
			{
				await _module.InvokeVoidAsync("dispose").ConfigureAwait(true);
				await _module.DisposeAsync().ConfigureAwait(true);
			}
		}
		catch
		{
		}
		finally
		{
			_dotNetObjectReference?.Dispose();
			GC.SuppressFinalize(this);
		}
	}

	private void ConfigureServiceIfChanged()
	{
		IListenerService activeListenerService = ActiveListenerService;
		ListenerConfiguration configuration = CreateConfiguration();
		if (ReferenceEquals(_lastConfiguredService, activeListenerService) && IsSameConfiguration(configuration, _lastConfiguration))
		{
			return;
		}

		activeListenerService.Configure(configuration);
		_lastConfiguredService = activeListenerService;
		_lastConfiguration = configuration;
	}

	private async Task SyncListeningStateAsync()
	{
		if (_module is null)
		{
			return;
		}

		if (!AutoStart || Mode == ListenerMode.ManualActivation)
		{
			if (_isStarted)
			{
				await _module.InvokeVoidAsync("stopListening").ConfigureAwait(true);
				_isStarted = false;
			}

			return;
		}

		if (!_isStarted)
		{
			await _module.InvokeVoidAsync("startListening").ConfigureAwait(true);
			_isStarted = true;
		}
	}

	private ListenerConfiguration CreateConfiguration()
		=> new()
		{
			Mode = Mode,
			Keyword = Keyword,
			KeywordSilenceTimeout = KeywordSilenceTimeout,
			KeywordTimeoutToken = KeywordTimeoutToken,
			ManualStartToken = ManualStartToken,
			ManualStopToken = ManualStopToken
		};

	private static bool IsSameConfiguration(ListenerConfiguration current, ListenerConfiguration? previous)
		=> previous is not null
			&& current.Mode == previous.Mode
			&& string.Equals(current.Keyword, previous.Keyword, StringComparison.Ordinal)
			&& current.KeywordSilenceTimeout == previous.KeywordSilenceTimeout
			&& string.Equals(current.KeywordTimeoutToken, previous.KeywordTimeoutToken, StringComparison.Ordinal)
			&& string.Equals(current.ManualStartToken, previous.ManualStartToken, StringComparison.Ordinal)
			&& string.Equals(current.ManualStopToken, previous.ManualStopToken, StringComparison.Ordinal);
}
