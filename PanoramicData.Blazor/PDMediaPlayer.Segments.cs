using PanoramicData.Blazor.Helpers;

namespace PanoramicData.Blazor;

public partial class PDMediaPlayer
{
	// how close to its end a played segment counts as finished, allowing for the gap between time updates
	private static readonly TimeSpan _segmentEndAllowance = TimeSpan.FromSeconds(0.05);

	// how far outside a played segment a seek must land to cancel playing it
	private static readonly TimeSpan _segmentLeaveAllowance = TimeSpan.FromSeconds(1);

	private IEnumerable<PDMediaSegment>? _lastSegments;
	private int _lastSegmentCount = -1;
	private IReadOnlyList<PDMediaSegment> _sortedSegments = [];
	private IReadOnlyList<KeyValuePair<string, int>> _segmentKindCounts = [];
	private List<PDMediaSegment> _visibleSegments = [];
	private bool _segmentKindsInitialized;
	private IReadOnlySet<string>? _lastVisibleSegmentKindsParameter;
	private HashSet<string>? _visibleSegmentKinds;
	private IReadOnlyList<MediaSegmentBar>? _bars;
	private int _lastMaxSegmentLanes;
	private HashSet<PDMediaSegment> _activeSegments = [];
	private PDMediaSegment? _playingSegment;
	private bool _loopSegment;
	private bool _isSegmentListExpanded = true;
	private bool? _lastSegmentListExpandedParameter;
	private bool _isTranscriptExpanded;
	private bool? _lastTranscriptExpandedParameter;
	private PDMediaTextLine? _revealedLine;

	/// <summary>
	/// Gets the valid segments, sorted by start.
	/// </summary>
	public IReadOnlyList<PDMediaSegment> SortedSegments => _sortedSegments;

	/// <summary>
	/// Gets the segments of the visible kinds, sorted by start.
	/// </summary>
	public IReadOnlyList<PDMediaSegment> VisibleSegments => _visibleSegments;

	/// <summary>
	/// Gets the visible segments that contain the current position plus <see cref="SeekLead"/>, in time order. The lead is added, as it
	/// is for <see cref="CurrentMarker"/>, so that a segment that has just been selected (which puts the playhead the lead before it)
	/// is the current one rather than the segment before it.
	/// </summary>
	public IReadOnlyList<PDMediaSegment> CurrentSegments => MediaSegmentNavigator.Active(_visibleSegments, MarkerCursor);

	/// <summary>
	/// Gets the current segment: the latest-starting visible segment that contains the current position plus
	/// <see cref="SeekLead"/>, or <c>null</c> between segments.
	/// </summary>
	public PDMediaSegment? CurrentSegment => CurrentSegments is { Count: > 0 } active ? active[^1] : null;

	/// <summary>
	/// Gets the segment being played on its own by <see cref="PlaySegmentAsync(PDMediaSegment, bool)"/>, if any.
	/// </summary>
	public PDMediaSegment? PlayingSegment => _playingSegment;

	/// <summary>
	/// Gets whether <see cref="PlayingSegment"/> starts again when it ends.
	/// </summary>
	public bool IsLoopingSegment => _loopSegment;

	/// <summary>
	/// Seeks to a segment, <see cref="SeekLead"/> before its start, and raises <see cref="OnSegmentSelected"/>.
	/// </summary>
	/// <param name="segment">The segment; it need not be one of <see cref="Segments"/>.</param>
	/// <returns>A task that completes when the seek has been requested and the event handled.</returns>
	public async Task SeekToSegmentAsync(PDMediaSegment segment)
	{
		ArgumentNullException.ThrowIfNull(segment);
		await SeekCoreAsync(segment.Start - NonNegativeSeekLead).ConfigureAwait(true);
		await OnSegmentSelected.InvokeAsync(segment).ConfigureAwait(true);
		await RefreshAsync().ConfigureAwait(true);
	}

	/// <summary>
	/// Plays just one segment: seeks to its start, plays, and pauses at its end.
	/// </summary>
	/// <param name="segment">The segment.</param>
	/// <returns>A task that completes when playback has been requested.</returns>
	public Task PlaySegmentAsync(PDMediaSegment segment) => PlaySegmentAsync(segment, false);

	/// <summary>
	/// Plays just one segment: seeks to its start and plays; at its end, pauses or, when <paramref name="loop"/> is
	/// true, starts the segment again. The end is noticed at the next position report, so playback stops within
	/// about one <see cref="TimeUpdateRate"/> interval of it. Seeking well outside the segment cancels this.
	/// </summary>
	/// <param name="segment">The segment.</param>
	/// <param name="loop">Whether to play the segment over and over.</param>
	/// <returns>A task that completes when playback has been requested.</returns>
	public async Task PlaySegmentAsync(PDMediaSegment segment, bool loop)
	{
		ArgumentNullException.ThrowIfNull(segment);
		_playingSegment = segment;
		_loopSegment = loop;
		await SeekCoreAsync(segment.Start).ConfigureAwait(true);
		await OnSegmentSelected.InvokeAsync(segment).ConfigureAwait(true);
		await PlayAsync().ConfigureAwait(true);
	}

	/// <summary>
	/// Turns looping of the current segment on or off. Turning it on loops <see cref="PlayingSegment"/>, or else the
	/// current segment, without seeking; turning it off lets playback carry on past the segment's end.
	/// </summary>
	/// <param name="loop">Whether to loop.</param>
	/// <returns>A task that completes when the change has been applied.</returns>
	public Task SetSegmentLoopAsync(bool loop)
	{
		_playingSegment = loop ? _playingSegment ?? CurrentSegment : null;
		_loopSegment = loop && _playingSegment is not null;
		return RefreshAsync();
	}

	/// <summary>
	/// Seeks to the start of the next visible segment after the current position (see <see cref="MarkerTolerance"/>).
	/// </summary>
	/// <returns>The segment sought to, or <c>null</c> when there is none.</returns>
	public Task<PDMediaSegment?> NextSegmentAsync() => NextSegmentAsync(null);

	/// <summary>
	/// Seeks to the start of the next segment after the current position.
	/// </summary>
	/// <param name="kind">The kind of segment to go to, whether or not it is visible; <c>null</c> for any visible kind.</param>
	/// <returns>The segment sought to, or <c>null</c> when there is none.</returns>
	public async Task<PDMediaSegment?> NextSegmentAsync(string? kind)
	{
		var segment = MediaSegmentNavigator.Next(GetNavigableSegments(kind), MarkerCursor, MarkerTolerance);
		while (segment is not null && MediaTimelineLayout.ClampPosition(segment.Start - NonNegativeSeekLead, _duration) <= CursorPosition)
		{
			// segments starting past the end of the media all seek to the end, so once there they are not next
			segment = MediaSegmentNavigator.Next(GetNavigableSegments(kind), segment.Start, MarkerTolerance);
		}

		return await SelectSegmentAsync(segment).ConfigureAwait(true);
	}

	/// <summary>
	/// Seeks to the start of the previous visible segment: the start of the current one when more than
	/// <see cref="MarkerTolerance"/> into it, otherwise the one before.
	/// </summary>
	/// <returns>The segment sought to, or <c>null</c> when there is none.</returns>
	public Task<PDMediaSegment?> PreviousSegmentAsync() => PreviousSegmentAsync(null);

	/// <summary>
	/// Seeks to the start of the previous segment.
	/// </summary>
	/// <param name="kind">The kind of segment to go to, whether or not it is visible; <c>null</c> for any visible kind.</param>
	/// <returns>The segment sought to, or <c>null</c> when there is none.</returns>
	public Task<PDMediaSegment?> PreviousSegmentAsync(string? kind)
		=> SelectSegmentAsync(MediaSegmentNavigator.Previous(GetNavigableSegments(kind), MarkerCursor, MarkerTolerance));

	/// <summary>
	/// Gets whether segments of a kind are shown.
	/// </summary>
	/// <param name="kind">The kind.</param>
	/// <returns>Whether the kind is visible.</returns>
	public bool IsSegmentKindVisible(string kind) => _visibleSegmentKinds is null || _visibleSegmentKinds.Contains(NormalizeKind(kind));

	/// <summary>
	/// Shows or hides the segments of a kind, as the kind's toggle chip does, and raises
	/// <see cref="VisibleSegmentKindsChanged"/>.
	/// </summary>
	/// <param name="kind">The kind.</param>
	/// <param name="visible">Whether to show it.</param>
	/// <returns>A task that completes when the change has been applied.</returns>
	public async Task SetSegmentKindVisibleAsync(string kind, bool visible)
	{
		var normalized = NormalizeKind(kind);
		if (IsSegmentKindVisible(normalized) == visible)
		{
			return;
		}

		var kinds = new HashSet<string>(_visibleSegmentKinds ?? _segmentKindCounts.Select(k => k.Key), MediaMarkerNavigator.KindComparer);
		ToggleMembership(kinds, normalized, visible);
		_visibleSegmentKinds = kinds;
		UpdateVisibleSegments();
		await VisibleSegmentKindsChanged.InvokeAsync(kinds).ConfigureAwait(true);
		await RefreshAsync().ConfigureAwait(true);
	}

	private TimeSpan CursorPosition => _scrubPosition ?? _pendingSeek ?? _position;

	private static void ToggleMembership(HashSet<string> kinds, string kind, bool member)
	{
		if (member)
		{
			kinds.Add(kind);
		}
		else
		{
			kinds.Remove(kind);
		}
	}

	private Task ToggleSegmentKindAsync(string kind) => SetSegmentKindVisibleAsync(kind, !IsSegmentKindVisible(kind));

	private async Task<PDMediaSegment?> SelectSegmentAsync(PDMediaSegment? segment)
	{
		if (segment is not null)
		{
			await SeekToSegmentAsync(segment).ConfigureAwait(true);
		}

		return segment;
	}

	private IReadOnlyList<PDMediaSegment> GetNavigableSegments(string? kind)
		=> kind is null ? _visibleSegments : MediaSegmentNavigator.Filter(_sortedSegments, kind);

	private void ApplySegmentParameters()
	{
		var segmentsChanged = ApplySegmentsParameter();
		var kindsChanged = ApplyVisibleSegmentKindsParameter();
		if (segmentsChanged || kindsChanged || _lastMaxSegmentLanes != MaxSegmentLanes)
		{
			_lastMaxSegmentLanes = MaxSegmentLanes;
			UpdateVisibleSegments();
		}

		ApplySegmentPanelParameters();
	}

	private bool ApplySegmentsParameter()
	{
		if (ReferenceEquals(Segments, _lastSegments) && !HasSegmentCountChanged())
		{
			return false;
		}

		_lastSegments = Segments;
		_sortedSegments = MediaSegmentNavigator.Sort(Segments);
		_lastSegmentCount = Segments?.Count() ?? 0;
		_segmentKindCounts = MediaSegmentNavigator.CountByKind(_sortedSegments);
		return true;
	}

	private bool HasSegmentCountChanged()
		=> Segments is not null && Segments.TryGetNonEnumeratedCount(out var count) && count != _lastSegmentCount;

	private bool ApplyVisibleSegmentKindsParameter()
	{
		if (_segmentKindsInitialized && ReferenceEquals(VisibleSegmentKinds, _lastVisibleSegmentKindsParameter))
		{
			return false;
		}

		_segmentKindsInitialized = true;
		_lastVisibleSegmentKindsParameter = VisibleSegmentKinds;
		_visibleSegmentKinds = VisibleSegmentKinds is null ? null : new HashSet<string>(VisibleSegmentKinds.Select(NormalizeKind), MediaMarkerNavigator.KindComparer);
		return true;
	}

	private void ApplySegmentPanelParameters()
	{
		if (_lastSegmentListExpandedParameter != SegmentListExpanded)
		{
			_lastSegmentListExpandedParameter = SegmentListExpanded;
			_isSegmentListExpanded = SegmentListExpanded;
		}

		if (_lastTranscriptExpandedParameter != TranscriptExpanded)
		{
			_lastTranscriptExpandedParameter = TranscriptExpanded;
			_isTranscriptExpanded = TranscriptExpanded;
		}
	}

	private void UpdateVisibleSegments()
	{
		_visibleSegments = [.. MediaSegmentNavigator.Filter(_sortedSegments, _visibleSegmentKinds)];
		_bars = null;
	}

	private IReadOnlyList<MediaSegmentBar> GetBars()
		=> _bars ??= MediaSegmentLayout.GetBars(_visibleSegments, _duration, MaxSegmentLanes, MarkerTickResolution, GetSegmentColor);

	private string GetSegmentColor(PDMediaSegment segment)
		=> MediaTimelineLayout.SanitizeColor(segment.Color) ?? GetKindColor(MediaSegmentNavigator.GetKind(segment), _segmentKindCounts);

	// raises the entered and left events for the segments whose boundaries playback (or a seek) has crossed
	private async Task CheckSegmentCrossingsAsync()
	{
		var now = MediaSegmentNavigator.Active(_sortedSegments, _position).ToHashSet();
		var left = _activeSegments.Where(s => !now.Contains(s)).OrderBy(s => s.Start).ToList();
		var entered = now.Where(s => !_activeSegments.Contains(s)).OrderBy(s => s.Start).ToList();
		_activeSegments = now;
		foreach (var segment in left)
		{
			await OnSegmentLeft.InvokeAsync(segment).ConfigureAwait(true);
		}

		foreach (var segment in entered)
		{
			await OnSegmentEntered.InvokeAsync(segment).ConfigureAwait(true);
		}
	}

	// pauses (or loops) a segment played on its own once playback reaches its end
	private async Task CheckSegmentPlaybackAsync()
	{
		if (_playingSegment is not { } segment)
		{
			return;
		}

		if (_position < segment.Start - _segmentLeaveAllowance || _position > segment.End + _segmentLeaveAllowance)
		{
			_playingSegment = null;
			_loopSegment = false;
		}
		else if (_position >= segment.End - _segmentEndAllowance)
		{
			await FinishSegmentAsync(segment).ConfigureAwait(true);
		}
	}

	private async Task FinishSegmentAsync(PDMediaSegment segment)
	{
		if (_loopSegment)
		{
			await SeekCoreAsync(segment.Start).ConfigureAwait(true);
			return;
		}

		_playingSegment = null;
		await PauseAsync().ConfigureAwait(true);
	}

	private void ResetSegmentPlayback()
	{
		_activeSegments = [];
		_playingSegment = null;
		_loopSegment = false;
		_revealedLine = null;
	}

	private void ToggleSegmentList() => _isSegmentListExpanded = !_isSegmentListExpanded;

	private void ToggleTranscript() => _isTranscriptExpanded = !_isTranscriptExpanded;

	private Task ToggleSegmentLoopAsync() => SetSegmentLoopAsync(!_loopSegment);

	private async Task PlayCurrentSegmentAsync()
	{
		if (CurrentSegment is { } segment)
		{
			await PlaySegmentAsync(segment).ConfigureAwait(true);
		}
	}

	// keeps the line being spoken in view in the transcript pane, asking the script only when the line changes
	private async Task RevealActiveLineAsync()
	{
		var line = _isTranscriptExpanded ? MediaSegmentNavigator.ActiveLine(CurrentSegment?.Lines, DisplayPosition) : null;
		if (line is null || ReferenceEquals(line, _revealedLine))
		{
			return;
		}

		_revealedLine = line;
		await TryInvokeVoidAsync("reveal", TranscriptElement).ConfigureAwait(true);
	}
}
