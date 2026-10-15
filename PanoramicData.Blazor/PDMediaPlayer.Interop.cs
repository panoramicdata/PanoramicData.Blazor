using PanoramicData.Blazor.Helpers;
using System.Text.Json;

namespace PanoramicData.Blazor;

public partial class PDMediaPlayer
{
	private MediaPlayerPersistedState? _persisted;
	private DateTimeOffset _lastPersist = DateTimeOffset.MinValue;

	/// <summary>
	/// Called by the player's script when the media element raises an event. The script sends
	/// <c>loadedmetadata</c>, <c>durationchange</c>, <c>play</c>, <c>pause</c>, <c>ended</c>, <c>error</c> and
	/// <c>seeked</c> as they happen, and <c>timeupdate</c> and <c>progress</c> at most
	/// <see cref="TimeUpdateRate"/> times a second.
	/// </summary>
	/// <param name="eventName">The name of the media event.</param>
	/// <param name="state">A snapshot of the media element.</param>
	/// <returns>A task that completes when the event has been handled.</returns>
	[JSInvokable]
	public Task OnMediaEvent(string eventName, MediaElementState state) => InvokeAsync(() => HandleMediaEventAsync(eventName, state));

	/// <inheritdoc />
	protected override async Task OnModuleLoadedAsync(bool firstRender)
	{
		// the saved speed and position must be known before the element can report that it has loaded
		await LoadPersistedStateAsync().ConfigureAwait(true);
		await AttachAsync().ConfigureAwait(true);
		if (_pendingPlay)
		{
			await PlayAsync().ConfigureAwait(true);
		}
	}

	/// <inheritdoc />
	protected override async Task OnAfterRenderWithModuleAsync(bool firstRender)
	{
		// switching between audio and video renders a new element, which needs the script's listeners
		if (_attachedKind is not null && _attachedKind != _kind)
		{
			await AttachAsync().ConfigureAwait(true);
		}
	}

	/// <summary>
	/// Detaches the script from the media element, saves the position when <see cref="PersistKey"/> is set, and
	/// releases the script and the object reference it was given.
	/// </summary>
	/// <returns>A task that completes when the player has been disposed.</returns>
	public override async ValueTask DisposeAsync()
	{
		if (!_disposed)
		{
			_disposed = true;
			if (Module is not null)
			{
				await SavePositionAsync(force: true).ConfigureAwait(true);
				await TryInvokeVoidAsync("detach", _mediaElement).ConfigureAwait(true);
			}

			await base.DisposeAsync().ConfigureAwait(true);
			_objRef?.Dispose();
			_objRef = null;
		}

		GC.SuppressFinalize(this);
	}

	private async Task AttachAsync()
	{
		_objRef ??= DotNetObjectReference.Create(this);
		_attachedKind = _kind;
		await TryInvokeVoidAsync("attach", _mediaElement, _objRef, _updateInterval).ConfigureAwait(true);
		await TryInvokeVoidAsync("setRate", _mediaElement, _rate).ConfigureAwait(true);
		await TryInvokeVoidAsync("setVolume", _mediaElement, _volume, _muted).ConfigureAwait(true);
	}

	private async Task HandleMediaEventAsync(string eventName, MediaElementState? state)
	{
		if (_disposed || state is null)
		{
			return;
		}

		switch (eventName)
		{
			case "loadedmetadata" or "durationchange":
				await OnMetadataAsync(state).ConfigureAwait(true);
				break;
			case "timeupdate" or "seeked":
				await OnProgressAsync(state, raiseTimeUpdate: true).ConfigureAwait(true);
				break;
			case "progress":
				await OnProgressAsync(state, raiseTimeUpdate: false).ConfigureAwait(true);
				break;
			case "play":
				_isPlaying = true;
				await OnPlay.InvokeAsync().ConfigureAwait(true);
				break;
			case "pause":
				_isPlaying = false;
				UpdatePosition(state);
				await OnPause.InvokeAsync().ConfigureAwait(true);
				await SavePositionAsync(force: true).ConfigureAwait(true);
				break;
			case "ended":
				_isPlaying = false;
				UpdatePosition(state);
				await OnEnded.InvokeAsync().ConfigureAwait(true);
				await ClearPositionAsync().ConfigureAwait(true);
				break;
			case "error":
				await OnMediaErrorAsync(state).ConfigureAwait(true);
				break;
			default:
				return;
		}

		StateHasChanged();
	}

	private async Task OnMetadataAsync(MediaElementState state)
	{
		_buffered = state.Buffered;
		_isLive = state.IsInfinite;
		var duration = _isLive ? null : MediaTimeFormatter.FromSeconds(state.Duration);
		if (duration != _duration)
		{
			_duration = duration;
			InvalidateTicks();
		}

		if (_isReady || (duration is null && !_isLive))
		{
			return;
		}

		_isReady = true;
		_error = null;

		// a new source starts at the element's default rate, which the script keeps equal to the chosen rate
		if (Math.Abs(state.PlaybackRate - _rate) > 0.001)
		{
			await TryInvokeVoidAsync("setRate", _mediaElement, _rate).ConfigureAwait(true);
		}

		var target = _isLive ? null : _pendingSeek ?? GetSavedPosition() ?? StartAt;
		_pendingSeek = null;
		if (target is { } position && position > TimeSpan.Zero)
		{
			await SeekCoreAsync(position).ConfigureAwait(true);
		}
		else
		{
			UpdatePosition(state);
		}

		await OnReady.InvokeAsync(_duration).ConfigureAwait(true);
	}

	private async Task OnProgressAsync(MediaElementState state, bool raiseTimeUpdate)
	{
		_buffered = state.Buffered;
		UpdatePosition(state);
		if (!raiseTimeUpdate)
		{
			return;
		}

		await OnTimeUpdate.InvokeAsync(_position).ConfigureAwait(true);
		await RaisePositionChangedAsync().ConfigureAwait(true);
		await CheckRangeAsync().ConfigureAwait(true);
		await SavePositionAsync(force: false).ConfigureAwait(true);
	}

	private void UpdatePosition(MediaElementState state)
		=> _position = MediaTimeFormatter.FromSeconds(state.CurrentTime) ?? TimeSpan.Zero;

	private async Task CheckRangeAsync()
	{
		var inRange = MediaTimelineLayout.IsInRange(_position, RangeStart, RangeEnd);
		var wasInRange = _wasInRange;
		_wasInRange = inRange;
		if (inRange == wasInRange || inRange is null)
		{
			return;
		}

		if (inRange.Value)
		{
			await OnRangeEntered.InvokeAsync(_position).ConfigureAwait(true);
		}
		else if (wasInRange == true)
		{
			await OnRangeLeft.InvokeAsync(_position).ConfigureAwait(true);
		}
	}

	private async Task OnMediaErrorAsync(MediaElementState state)
	{
		_isPlaying = false;
		_pendingPlay = false;
		var src = _src;
		int? status = null;
		if (MediaErrorMapper.ShouldProbe(state.ErrorCode, src))
		{
			status = await TryInvokeAsync<int?>(null, "probe", src).ConfigureAwait(true);
			if (src != _src)
			{
				// the source changed while the server was asked; the error belonged to the old one
				return;
			}
		}

		_error = MediaErrorMapper.Map(state.ErrorCode, state.ErrorMessage, status);
		await OnError.InvokeAsync(_error).ConfigureAwait(true);
	}

	private TimeSpan? GetSavedPosition()
		=> string.IsNullOrWhiteSpace(PersistKey) ? null : _persisted?.GetPosition(_src);

	private async Task LoadPersistedStateAsync()
	{
		if (string.IsNullOrWhiteSpace(PersistKey))
		{
			return;
		}

		try
		{
			if (StateManager is not null)
			{
				await StateManager.InitializeAsync().ConfigureAwait(true);
				_persisted = await StateManager.LoadStateAsync<MediaPlayerPersistedState>(PersistKey).ConfigureAwait(true);
			}
			else
			{
				var json = await Module!.InvokeAsync<string?>("getItem", PersistKey).ConfigureAwait(true);
				_persisted = string.IsNullOrWhiteSpace(json) ? null : JsonSerializer.Deserialize<MediaPlayerPersistedState>(json);
			}
		}
		catch (Exception ex) when (ex is JSException or JSDisconnectedException or JsonException or StateException or InvalidOperationException or TaskCanceledException)
		{
			// storage that cannot be read (blocked, full or holding something else) must not stop playback
			_persisted = null;
		}

		_persisted ??= new MediaPlayerPersistedState();
		if (_persisted.Rate is { } rate && !MediaPlaybackRates.Clamp(rate).Equals(_rate))
		{
			_rate = MediaPlaybackRates.Clamp(rate);
			await PlaybackRateChanged.InvokeAsync(_rate).ConfigureAwait(true);
			StateHasChanged();
		}
	}

	private async Task SaveRateAsync()
	{
		if (_persisted is null || string.IsNullOrWhiteSpace(PersistKey))
		{
			return;
		}

		_persisted.Rate = _rate;
		await WritePersistedStateAsync().ConfigureAwait(true);
	}

	private async Task SavePositionAsync(bool force)
	{
		if (_persisted is null || string.IsNullOrWhiteSpace(PersistKey) || !_isReady || _isLive || string.IsNullOrWhiteSpace(_src))
		{
			return;
		}

		var now = Clock.GetUtcNow();
		if (!force && now - _lastPersist < PersistInterval)
		{
			return;
		}

		_lastPersist = now;
		if (_position > TimeSpan.Zero)
		{
			_persisted.SetPosition(_src, _position);
		}
		else
		{
			_persisted.RemovePosition(_src);
		}

		await WritePersistedStateAsync().ConfigureAwait(true);
	}

	private async Task ClearPositionAsync()
	{
		if (_persisted?.RemovePosition(_src) == true)
		{
			await WritePersistedStateAsync().ConfigureAwait(true);
		}
	}

	private async Task WritePersistedStateAsync()
	{
		try
		{
			if (StateManager is not null)
			{
				await StateManager.SaveStateAsync(PersistKey!, _persisted!).ConfigureAwait(true);
			}
			else if (Module is not null)
			{
				await Module.InvokeVoidAsync("setItem", PersistKey, JsonSerializer.Serialize(_persisted)).ConfigureAwait(true);
			}
		}
		catch (Exception ex) when (ex is JSException or JSDisconnectedException or StateException or InvalidOperationException or TaskCanceledException or ObjectDisposedException)
		{
			// a failed save only loses the resume point
		}
	}

	private async Task TryInvokeVoidAsync(string identifier, params object?[] args)
	{
		if (Module is null || _attachedKind is null)
		{
			return;
		}

		try
		{
			await Module.InvokeVoidAsync(identifier, args).ConfigureAwait(true);
		}
		catch (Exception ex) when (ex is JSException or JSDisconnectedException or TaskCanceledException or ObjectDisposedException)
		{
			// the page is going away, or the element has gone; there is nothing left to control
		}
	}

	private async Task<T> TryInvokeAsync<T>(T fallback, string identifier, params object?[] args)
	{
		if (Module is null || _attachedKind is null)
		{
			return fallback;
		}

		try
		{
			return await Module.InvokeAsync<T>(identifier, args).ConfigureAwait(true);
		}
		catch (Exception ex) when (ex is JSException or JSDisconnectedException or TaskCanceledException or ObjectDisposedException)
		{
			return fallback;
		}
	}
}
