using PanoramicData.Blazor.Helpers;

namespace PanoramicData.Blazor;

/// <summary>
/// Plays audio or video through a native HTML media element, with an accessible Bootstrap transport (play, skip,
/// seek, volume and speed), and markers: events along the timeline that are drawn on the seek bar, listed beside or
/// beneath the player, filtered by kind and jumped between.
/// </summary>
/// <remarks>
/// <para>
/// The element streams its source itself, so a server that supports HTTP range requests lets the user seek without
/// downloading the whole file; the player does no buffering of its own.
/// </para>
/// <para>
/// Keyboard shortcuts work while focus is inside the player: K plays or pauses, J or [ goes to the previous marker,
/// L or ] to the next marker, M mutes, and &lt; or &gt; chooses the next slower or faster speed. The seek bar is a
/// native slider, so the arrow keys, Page Up, Page Down, Home and End move it.
/// </para>
/// <para>
/// Every public method can be called before the media element is ready: a seek waits for the media to load and a
/// play request waits for the player's script.
/// </para>
/// </remarks>
public partial class PDMediaPlayer : JSModuleComponentBase
{
	private const string DefaultKindLabel = "Marker";
	private const int MaxReportedPositions = 8;

	private bool _parametersInitialized;

	private string? _src;
	private MediaKind _kind;
	private TimeSpan _position;
	private TimeSpan? _duration;
	private TimeSpan? _scrubPosition;
	private bool _isLive;
	private bool _isReady;
	private bool _isPlaying;
	private IReadOnlyList<double>? _buffered;
	private MediaPlayerError? _error;
	private TimeSpan? _pendingSeek;
	private bool _pendingPlay;
	private bool? _wasInRange;

	private double _rate = MediaPlaybackRates.Normal;
	private double _volume = 1;
	private bool _muted;
	private IReadOnlyList<double> _rates = MediaPlaybackRates.Default;
	private int _updateInterval = MediaTimelineLayout.GetUpdateIntervalMilliseconds(4);
	private bool _isMarkerListExpanded = true;

	private double _lastRateParameter = double.NaN;
	private double _lastVolumeParameter = double.NaN;
	private bool? _lastMutedParameter;
	private TimeSpan? _lastPositionParameter;
	private readonly Queue<TimeSpan> _reportedPositions = new();
	private bool? _lastExpandedParameter;
	private string? _lastPersistKey;

	/// <summary>
	/// Gets or sets the clock used to throttle saving the position. The default is the system clock.
	/// </summary>
	[Parameter]
	public TimeProvider Clock { get; set; } = TimeProvider.System;

	/// <summary>
	/// Gets or sets the unique identifier of the player's root element.
	/// </summary>
	[Parameter]
	public string Id { get; set; } = $"pd-media-player-{ComponentIdSequence.Next()}";

	/// <summary>
	/// Gets or sets additional CSS classes for the player's root element.
	/// </summary>
	[Parameter]
	public string? CssClass { get; set; }

	/// <summary>
	/// Gets or sets the URL of the media. Changing it loads the new media and resets the position, duration and
	/// error, while the chosen speed, volume and mute are kept.
	/// </summary>
	[Parameter]
	public string? Src { get; set; }

	/// <summary>
	/// Gets or sets whether to render an audio or a video element. The default is <see cref="MediaKind.Audio"/>.
	/// </summary>
	[Parameter]
	public MediaKind MediaKind { get; set; } = MediaKind.Audio;

	/// <summary>
	/// Gets or sets the URL of an image shown before a video plays.
	/// </summary>
	[Parameter]
	public string? Poster { get; set; }

	/// <summary>
	/// Gets or sets the title shown above the controls, also used as the accessible name of the player.
	/// </summary>
	[Parameter]
	public string? Title { get; set; }

	/// <summary>
	/// Gets or sets how much of the media the browser loads before playback. The default is
	/// <see cref="MediaPreload.Metadata"/>.
	/// </summary>
	[Parameter]
	public MediaPreload Preload { get; set; } = MediaPreload.Metadata;

	/// <summary>
	/// Gets or sets whether playback starts as soon as the media can play. Browsers may block this unless the media is
	/// muted. The default is false.
	/// </summary>
	[Parameter]
	public bool Autoplay { get; set; }

	/// <summary>
	/// Gets or sets whether playback starts again from the beginning when it ends.
	/// </summary>
	[Parameter]
	public bool Loop { get; set; }

	/// <summary>
	/// Gets or sets whether the sound is muted. Use with <see cref="MutedChanged"/> for two-way binding.
	/// </summary>
	[Parameter]
	public bool Muted { get; set; }

	/// <summary>
	/// Gets or sets the callback invoked when the user mutes or unmutes.
	/// </summary>
	[Parameter]
	public EventCallback<bool> MutedChanged { get; set; }

	/// <summary>
	/// Gets or sets the volume, from 0 to 1. The default is 1. Use with <see cref="VolumeChanged"/> for two-way
	/// binding.
	/// </summary>
	[Parameter]
	public double Volume { get; set; } = 1;

	/// <summary>
	/// Gets or sets the callback invoked when the user changes the volume.
	/// </summary>
	[Parameter]
	public EventCallback<double> VolumeChanged { get; set; }

	/// <summary>
	/// Gets or sets whether to show a download button. The default is false.
	/// </summary>
	[Parameter]
	public bool ShowDownload { get; set; }

	/// <summary>
	/// Gets or sets the URL the download button links to, for example an authorised download endpoint. When not set,
	/// the button links to <see cref="Src"/>.
	/// </summary>
	[Parameter]
	public string? DownloadUrl { get; set; }

	/// <summary>
	/// Gets or sets the file name suggested to the browser for a download.
	/// </summary>
	[Parameter]
	public string? DownloadFileName { get; set; }

	/// <summary>
	/// Gets or sets how many seconds the skip back button moves. The default is 15.
	/// </summary>
	[Parameter]
	public int SkipBackSeconds { get; set; } = 15;

	/// <summary>
	/// Gets or sets how many seconds the skip forward button moves. The default is 30.
	/// </summary>
	[Parameter]
	public int SkipForwardSeconds { get; set; } = 30;

	/// <summary>
	/// Gets or sets the speeds offered in the speed menu. Values outside 0.5 to 4 are dropped; the default is 0.75,
	/// 1, 1.25, 1.5, 1.75 and 2.
	/// </summary>
	[Parameter]
	public IReadOnlyList<double>? PlaybackRates { get; set; }

	/// <summary>
	/// Gets or sets the playback speed, from 0.5 to 4. Pitch is preserved. The chosen speed is kept when
	/// <see cref="Src"/> changes. Like <see cref="Volume"/> and <see cref="Muted"/>, it is applied when the host
	/// passes a different value from last time, so the user's own choice survives the host re-rendering. Use with
	/// <see cref="PlaybackRateChanged"/> for two-way binding.
	/// </summary>
	[Parameter]
	public double PlaybackRate { get; set; } = MediaPlaybackRates.Normal;

	/// <summary>
	/// Gets or sets the callback invoked when the playback speed changes.
	/// </summary>
	[Parameter]
	public EventCallback<double> PlaybackRateChanged { get; set; }

	/// <summary>
	/// Gets or sets a key under which the chosen speed and the position (one per source, the 50 most recent) are kept
	/// in browser local storage, so that both survive a reload. The position is saved every
	/// <see cref="PersistInterval"/> while playing and on pause, restored when the same source loads again and cleared
	/// when playback ends. When the player is inside a <see cref="PDLocalStorageStateManager"/> (or any other cascaded
	/// <see cref="IAsyncStateManager"/>), that is used; otherwise the player uses local storage directly.
	/// </summary>
	[Parameter]
	public string? PersistKey { get; set; }

	/// <summary>
	/// Gets or sets the minimum time between saves of the position while playing. The default is 5 seconds.
	/// </summary>
	[Parameter]
	public TimeSpan PersistInterval { get; set; } = TimeSpan.FromSeconds(5);

	/// <summary>
	/// Gets or sets the cascaded state manager used for <see cref="PersistKey"/>, if there is one.
	/// </summary>
	[CascadingParameter]
	public IAsyncStateManager? StateManager { get; set; }

	/// <summary>
	/// Gets or sets how many times a second, at most, the position is reported while playing (and so how often
	/// <see cref="OnTimeUpdate"/> and <see cref="PositionChanged"/> are raised). It is limited to 1 to 30; the default
	/// is 4.
	/// </summary>
	[Parameter]
	public double TimeUpdateRate { get; set; } = 4;

	/// <summary>
	/// Gets or sets the playback position. Setting it to a new value seeks; a value the player itself recently
	/// reported through <see cref="PositionChanged"/> does not. Use with <see cref="PositionChanged"/> for two-way
	/// binding.
	/// </summary>
	[Parameter]
	public TimeSpan Position { get; set; }

	/// <summary>
	/// Gets or sets the callback invoked when the position changes, at most <see cref="TimeUpdateRate"/> times a
	/// second while playing, and on every seek.
	/// </summary>
	[Parameter]
	public EventCallback<TimeSpan> PositionChanged { get; set; }

	/// <summary>
	/// Gets or sets a position to seek to when each source has loaded. A position saved under
	/// <see cref="PersistKey"/>, or a seek requested before loading, takes precedence.
	/// </summary>
	[Parameter]
	public TimeSpan? StartAt { get; set; }

	/// <summary>
	/// Gets or sets the start of a range to shade on the seek bar, such as the part of a recording that is the show
	/// proper. A missing start means the start of the media. The media itself is never trimmed.
	/// </summary>
	[Parameter]
	public TimeSpan? RangeStart { get; set; }

	/// <summary>
	/// Gets or sets the end of the shaded range. A missing end means the end of the media.
	/// </summary>
	[Parameter]
	public TimeSpan? RangeEnd { get; set; }

	/// <summary>
	/// Gets or sets the callback invoked with the position when playback enters the shaded range (including when it
	/// first starts inside it).
	/// </summary>
	[Parameter]
	public EventCallback<TimeSpan> OnRangeEntered { get; set; }

	/// <summary>
	/// Gets or sets the callback invoked with the position when playback leaves the shaded range.
	/// </summary>
	[Parameter]
	public EventCallback<TimeSpan> OnRangeLeft { get; set; }

	/// <summary>
	/// Gets or sets the markers: events along the timeline, in any order. Assign a new collection to change them.
	/// </summary>
	[Parameter]
	public IEnumerable<PDMediaMarker>? Markers { get; set; }

	/// <summary>
	/// Gets or sets how far before a marker to seek when it is selected, so that the lead-up can be heard. The default
	/// is zero. Marker navigation and the current marker are worked out from the position plus this lead.
	/// </summary>
	[Parameter]
	public TimeSpan SeekLead { get; set; }

	/// <summary>
	/// Gets or sets how close to the current position a marker may be before next and previous skip over it. Previous
	/// within this time of a marker's start goes to the marker before; further in, it returns to the marker's start.
	/// The default is half a second.
	/// </summary>
	[Parameter]
	public TimeSpan MarkerTolerance { get; set; } = TimeSpan.FromSeconds(0.5);

	/// <summary>
	/// Gets or sets the kinds of marker to show, or <c>null</c> (the default) to show every kind. Hidden kinds are
	/// left off the seek bar and the list and skipped by next and previous. A kind missing from the set is hidden,
	/// including a kind that only appears when <see cref="Markers"/> changes. Use with
	/// <see cref="VisibleKindsChanged"/> for two-way binding.
	/// </summary>
	[Parameter]
	public IReadOnlySet<string>? VisibleKinds { get; set; }

	/// <summary>
	/// Gets or sets the callback invoked when the user shows or hides a kind.
	/// </summary>
	[Parameter]
	public EventCallback<IReadOnlySet<string>?> VisibleKindsChanged { get; set; }

	/// <summary>
	/// Gets or sets display names for marker kinds, for example <c>dead-air</c> to <c>Dead air</c>. A kind without
	/// a name is shown as it is.
	/// </summary>
	[Parameter]
	public IReadOnlyDictionary<string, string>? KindLabels { get; set; }

	/// <summary>
	/// Gets or sets CSS colours for marker kinds. A kind without a colour gets one from a built-in palette, and a
	/// marker's own <see cref="PDMediaMarker.Color"/> takes precedence.
	/// </summary>
	[Parameter]
	public IReadOnlyDictionary<string, string>? KindColors { get; set; }

	/// <summary>
	/// Gets or sets whether to show the toggle chips (with counts) that show or hide each kind of marker. The default
	/// is true.
	/// </summary>
	[Parameter]
	public bool ShowKindFilter { get; set; } = true;

	/// <summary>
	/// Gets or sets whether to show the marker list. The default is true.
	/// </summary>
	[Parameter]
	public bool ShowMarkerList { get; set; } = true;

	/// <summary>
	/// Gets or sets whether the marker list starts expanded. The user can collapse and expand it. The default is true.
	/// </summary>
	[Parameter]
	public bool MarkerListExpanded { get; set; } = true;

	/// <summary>
	/// Gets or sets where the marker list is shown. The default is <see cref="MediaMarkerListPlacement.Below"/>.
	/// </summary>
	[Parameter]
	public MediaMarkerListPlacement MarkerListPlacement { get; set; } = MediaMarkerListPlacement.Below;

	/// <summary>
	/// Gets or sets the maximum height of the marker list, as a CSS length. The list scrolls and only renders the
	/// items in view, so it stays fast with thousands of markers. The default is <c>16rem</c>.
	/// </summary>
	[Parameter]
	public string MarkerListMaxHeight { get; set; } = "16rem";

	/// <summary>
	/// Gets or sets the number of slots the seek bar is divided into for drawing markers. Markers of the same kind
	/// that fall in the same slot are drawn as one tick (its tooltip gives the count), so at most this many ticks are
	/// drawn per kind. The default is 400.
	/// </summary>
	[Parameter]
	public int MarkerTickResolution { get; set; } = MediaTimelineLayout.DefaultTickResolution;

	/// <summary>
	/// Gets or sets whether to show a message inside the player when the media cannot be played. The default is true.
	/// </summary>
	[Parameter]
	public bool ShowErrors { get; set; } = true;

	/// <summary>
	/// Gets or sets the callback invoked when the media has loaded enough to know its duration, with the duration,
	/// or <c>null</c> for a live stream.
	/// </summary>
	[Parameter]
	public EventCallback<TimeSpan?> OnReady { get; set; }

	/// <summary>
	/// Gets or sets the callback invoked with the position while playing, at most <see cref="TimeUpdateRate"/> times
	/// a second, and after each seek.
	/// </summary>
	[Parameter]
	public EventCallback<TimeSpan> OnTimeUpdate { get; set; }

	/// <summary>
	/// Gets or sets the callback invoked when playback starts.
	/// </summary>
	[Parameter]
	public EventCallback OnPlay { get; set; }

	/// <summary>
	/// Gets or sets the callback invoked when playback pauses.
	/// </summary>
	[Parameter]
	public EventCallback OnPause { get; set; }

	/// <summary>
	/// Gets or sets the callback invoked when playback reaches the end.
	/// </summary>
	[Parameter]
	public EventCallback OnEnded { get; set; }

	/// <summary>
	/// Gets or sets the callback invoked when the media cannot be played, with a friendly reason (for example
	/// <see cref="MediaErrorKind.Unauthorized"/> for a 401 response).
	/// </summary>
	[Parameter]
	public EventCallback<MediaPlayerError> OnError { get; set; }

	/// <summary>
	/// Gets or sets the callback invoked when a marker is selected, from the list, the seek bar, the next and previous
	/// buttons or shortcuts, or a call to <see cref="SeekToMarkerAsync"/>.
	/// </summary>
	[Parameter]
	public EventCallback<PDMediaMarker> OnMarkerSelected { get; set; }

	/// <summary>
	/// Gets the current playback position.
	/// </summary>
	public TimeSpan CurrentPosition => _position;

	/// <summary>
	/// Gets the duration, or <c>null</c> while it is unknown or for a live stream.
	/// </summary>
	public TimeSpan? Duration => _duration;

	/// <summary>
	/// Gets whether the media has loaded enough to know its duration (or that it is a live stream).
	/// </summary>
	public bool IsReady => _isReady;

	/// <summary>
	/// Gets whether the media is a live stream, whose duration is infinite. The seek bar is disabled for one.
	/// </summary>
	public bool IsLive => _isLive;

	/// <summary>
	/// Gets whether the media is playing.
	/// </summary>
	public bool IsPlaying => _isPlaying;

	/// <summary>
	/// Gets the current playback speed.
	/// </summary>
	public double CurrentPlaybackRate => _rate;

	/// <summary>
	/// Gets the current volume, from 0 to 1.
	/// </summary>
	public double CurrentVolume => _volume;

	/// <summary>
	/// Gets whether the sound is muted.
	/// </summary>
	public bool IsMuted => _muted;

	/// <summary>
	/// Gets the error that stopped the current media from playing, if any.
	/// </summary>
	public MediaPlayerError? LastError => _error;

	/// <summary>
	/// Gets the speeds offered in the speed menu, after validation.
	/// </summary>
	public IReadOnlyList<double> AvailablePlaybackRates => _rates;

	/// <inheritdoc />
	protected override string ModulePath => "./_content/PanoramicData.Blazor/PDMediaPlayer.razor.js";

	/// <inheritdoc />
	protected override async Task OnParametersSetAsync()
	{
		_rates = MediaPlaybackRates.Normalize(PlaybackRates);
		ApplyMarkerParameters();

		if (!_parametersInitialized)
		{
			_parametersInitialized = true;
			_src = Src;
			_kind = MediaKind;
			_rate = MediaPlaybackRates.Clamp(PlaybackRate);
			_volume = ClampVolume(Volume);
			_muted = Muted;
			_lastRateParameter = PlaybackRate;
			_lastVolumeParameter = Volume;
			_lastMutedParameter = Muted;
			_lastPositionParameter = Position;
			_lastExpandedParameter = MarkerListExpanded;
			_isMarkerListExpanded = MarkerListExpanded;
			_lastPersistKey = PersistKey;
			_updateInterval = MediaTimelineLayout.GetUpdateIntervalMilliseconds(TimeUpdateRate);
			return;
		}

		if (_src != Src || _kind != MediaKind)
		{
			_src = Src;
			_kind = MediaKind;
			ResetForNewSource();
		}

		if (_lastExpandedParameter != MarkerListExpanded)
		{
			_lastExpandedParameter = MarkerListExpanded;
			_isMarkerListExpanded = MarkerListExpanded;
		}

		await ApplyChangedParametersAsync().ConfigureAwait(true);
	}

	// each parameter is applied only when the host passes a different value from last time
	private async Task ApplyChangedParametersAsync()
	{
		await ApplySoundParametersAsync().ConfigureAwait(true);
		await ApplyPositionParameterAsync().ConfigureAwait(true);
		await ApplyPersistKeyParameterAsync().ConfigureAwait(true);

		var interval = MediaTimelineLayout.GetUpdateIntervalMilliseconds(TimeUpdateRate);
		if (interval != _updateInterval)
		{
			_updateInterval = interval;
			await TryInvokeVoidAsync("configure", MediaElement, _updateInterval).ConfigureAwait(true);
		}
	}

	private async Task ApplySoundParametersAsync()
	{
		if (!_lastRateParameter.Equals(PlaybackRate))
		{
			_lastRateParameter = PlaybackRate;
			await SetPlaybackRateAsync(PlaybackRate).ConfigureAwait(true);
		}

		if (!_lastVolumeParameter.Equals(Volume))
		{
			_lastVolumeParameter = Volume;
			await SetVolumeAsync(Volume).ConfigureAwait(true);
		}

		if (_lastMutedParameter != Muted)
		{
			_lastMutedParameter = Muted;
			await SetMutedAsync(Muted).ConfigureAwait(true);
		}
	}

	private async Task ApplyPositionParameterAsync()
	{
		if (_lastPositionParameter == Position)
		{
			return;
		}

		_lastPositionParameter = Position;

		// a host bound to Position passes back the positions the player reported, which are not requests to seek
		if (Position != _position && !_reportedPositions.Contains(Position))
		{
			await SeekAsync(Position).ConfigureAwait(true);
		}
	}

	private async Task ApplyPersistKeyParameterAsync()
	{
		if (_lastPersistKey == PersistKey)
		{
			return;
		}

		// a different key holds a different speed and different positions
		_lastPersistKey = PersistKey;
		_persisted = null;
		if (Module is null)
		{
			return;
		}

		await LoadPersistedStateAsync().ConfigureAwait(true);
		await TryInvokeVoidAsync("setRate", MediaElement, _rate).ConfigureAwait(true);
		if (_isReady && !_isLive && GetSavedPosition() is { } saved)
		{
			await SeekCoreAsync(saved).ConfigureAwait(true);
		}
	}

	/// <summary>
	/// Starts or resumes playback. Before the player's script has loaded, playback starts as soon as it has. Nothing
	/// happens without a <see cref="Src"/>.
	/// </summary>
	/// <returns>A task that completes when the request has been passed to the media element.</returns>
	public async Task PlayAsync()
	{
		if (string.IsNullOrWhiteSpace(_src))
		{
			return;
		}

		if (Module is null || _attachedKind is null)
		{
			_pendingPlay = true;
			return;
		}

		_pendingPlay = false;
		_error = null;
		// the script answers null when playback started, or the name of the error (NotAllowedError when the browser
		// blocks playback that the user did not start)
		var failure = await TryInvokeAsync<string?>("Error", "play", MediaElement).ConfigureAwait(true);
		_isPlaying = failure is null;
		await RefreshAsync().ConfigureAwait(true);
	}

	/// <summary>
	/// Pauses playback, and cancels a play request made before the player was ready.
	/// </summary>
	/// <returns>A task that completes when the request has been passed to the media element.</returns>
	public async Task PauseAsync()
	{
		_pendingPlay = false;
		_isPlaying = false;
		await TryInvokeVoidAsync("pause", MediaElement).ConfigureAwait(true);
		await RefreshAsync().ConfigureAwait(true);
	}

	/// <summary>
	/// Pauses when playing, and plays otherwise.
	/// </summary>
	/// <returns>A task that completes when the request has been passed to the media element.</returns>
	public Task TogglePlayAsync() => _isPlaying || _pendingPlay ? PauseAsync() : PlayAsync();

	/// <summary>
	/// Seeks to a position, limited to the media. Before the media has loaded, the seek happens once it has.
	/// </summary>
	/// <param name="position">The position to seek to.</param>
	/// <returns>A task that completes when the request has been passed to the media element.</returns>
	public async Task SeekAsync(TimeSpan position)
	{
		await SeekCoreAsync(position).ConfigureAwait(true);
		await RefreshAsync().ConfigureAwait(true);
	}

	/// <summary>
	/// Moves the position forward (or, with a negative offset, back), limited to the media.
	/// </summary>
	/// <param name="offset">How far to move.</param>
	/// <returns>A task that completes when the request has been passed to the media element.</returns>
	public Task SkipAsync(TimeSpan offset) => SeekAsync((_pendingSeek ?? _position) + offset);

	/// <summary>
	/// Sets the playback speed, limited to 0.5 to 4 (a value that is not a number gives 1), and saves it under
	/// <see cref="PersistKey"/> when one is set.
	/// </summary>
	/// <param name="rate">The speed.</param>
	/// <returns>A task that completes when the speed has been applied.</returns>
	public async Task SetPlaybackRateAsync(double rate)
	{
		var clamped = MediaPlaybackRates.Clamp(rate);
		var changed = !clamped.Equals(_rate);
		_rate = clamped;
		await TryInvokeVoidAsync("setRate", MediaElement, _rate).ConfigureAwait(true);
		if (changed)
		{
			await PlaybackRateChanged.InvokeAsync(_rate).ConfigureAwait(true);
			await SaveRateAsync().ConfigureAwait(true);
		}

		await RefreshAsync().ConfigureAwait(true);
	}

	/// <summary>
	/// Sets the volume, limited to 0 to 1 (a value that is not a number gives 1).
	/// </summary>
	/// <param name="volume">The volume.</param>
	/// <returns>A task that completes when the volume has been applied.</returns>
	public async Task SetVolumeAsync(double volume)
	{
		var clamped = ClampVolume(volume);
		var changed = !clamped.Equals(_volume);
		_volume = clamped;
		await TryInvokeVoidAsync("setVolume", MediaElement, _volume, _muted).ConfigureAwait(true);
		if (changed)
		{
			await VolumeChanged.InvokeAsync(_volume).ConfigureAwait(true);
		}

		await RefreshAsync().ConfigureAwait(true);
	}

	/// <summary>
	/// Mutes or unmutes the sound.
	/// </summary>
	/// <param name="muted">Whether to mute.</param>
	/// <returns>A task that completes when the change has been applied.</returns>
	public async Task SetMutedAsync(bool muted)
	{
		var changed = muted != _muted;
		_muted = muted;
		await TryInvokeVoidAsync("setVolume", MediaElement, _volume, _muted).ConfigureAwait(true);
		if (changed)
		{
			await MutedChanged.InvokeAsync(_muted).ConfigureAwait(true);
		}

		await RefreshAsync().ConfigureAwait(true);
	}

	/// <summary>
	/// Mutes the sound when it is audible, and unmutes it otherwise.
	/// </summary>
	/// <returns>A task that completes when the change has been applied.</returns>
	public Task ToggleMuteAsync() => SetMutedAsync(!_muted);

	private static double ClampVolume(double volume) => double.IsFinite(volume) ? Math.Clamp(volume, 0, 1) : 1;

	private async Task SeekCoreAsync(TimeSpan position)
	{
		var target = MediaTimelineLayout.ClampPosition(position, _duration);
		_scrubPosition = null;
		_position = target;
		if (_isReady && Module is not null && _attachedKind is not null)
		{
			_pendingSeek = null;
			await TryInvokeVoidAsync("seek", MediaElement, target.TotalSeconds).ConfigureAwait(true);
		}
		else
		{
			_pendingSeek = target;
		}

		await RaisePositionChangedAsync().ConfigureAwait(true);
	}

	private async Task RaisePositionChangedAsync()
	{
		_reportedPositions.Enqueue(_position);
		if (_reportedPositions.Count > MaxReportedPositions)
		{
			_reportedPositions.Dequeue();
		}

		await PositionChanged.InvokeAsync(_position).ConfigureAwait(true);
	}

	private void ResetForNewSource()
	{
		_position = TimeSpan.Zero;
		_duration = null;
		_scrubPosition = null;
		_isLive = false;
		_isReady = false;
		_isPlaying = false;
		_buffered = null;
		_error = null;
		_pendingSeek = null;
		_wasInRange = null;
		_reportedPositions.Clear();
		InvalidateTicks();
	}

	private Task RefreshAsync() => _disposed ? Task.CompletedTask : InvokeAsync(StateHasChanged);
}
