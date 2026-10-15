using PanoramicData.Blazor.Helpers;

namespace PanoramicData.Blazor;

public partial class PDMediaPlayer
{
	private IEnumerable<PDMediaMarker>? _lastMarkers;
	private int _lastMarkerCount = -1;
	private IReadOnlySet<string>? _lastVisibleKindsParameter;
	private bool _visibleKindsInitialized;
	private HashSet<string>? _visibleKinds;
	private IReadOnlyList<PDMediaMarker> _sortedMarkers = [];
	private IReadOnlyList<KeyValuePair<string, int>> _kindCounts = [];
	private List<PDMediaMarker> _visibleMarkers = [];
	private IReadOnlyList<MediaMarkerTick>? _ticks;
	private IReadOnlyDictionary<string, string>? _lastKindColors;
	private int _lastTickResolution;

	/// <summary>
	/// Gets the markers, sorted by position.
	/// </summary>
	public IReadOnlyList<PDMediaMarker> SortedMarkers => _sortedMarkers;

	/// <summary>
	/// Gets the markers of the visible kinds, sorted by position.
	/// </summary>
	public IReadOnlyList<PDMediaMarker> VisibleMarkers => _visibleMarkers;

	/// <summary>
	/// Gets the current marker: the last visible marker that starts at or before the position plus
	/// <see cref="SeekLead"/>, or <c>null</c> before the first.
	/// </summary>
	public PDMediaMarker? CurrentMarker => MediaMarkerNavigator.Current(_visibleMarkers, MarkerCursor);

	private TimeSpan MarkerCursor => (_scrubPosition ?? _pendingSeek ?? _position) + NonNegativeSeekLead;

	private TimeSpan NonNegativeSeekLead => SeekLead > TimeSpan.Zero ? SeekLead : TimeSpan.Zero;

	/// <summary>
	/// Seeks to a marker, <see cref="SeekLead"/> before its start, and raises <see cref="OnMarkerSelected"/>.
	/// </summary>
	/// <param name="marker">The marker; it need not be one of <see cref="Markers"/>.</param>
	/// <returns>A task that completes when the seek has been requested and the event handled.</returns>
	public async Task SeekToMarkerAsync(PDMediaMarker marker)
	{
		ArgumentNullException.ThrowIfNull(marker);
		await SeekCoreAsync(marker.Position - NonNegativeSeekLead).ConfigureAwait(true);
		await OnMarkerSelected.InvokeAsync(marker).ConfigureAwait(true);
		await RefreshAsync().ConfigureAwait(true);
	}

	/// <summary>
	/// Seeks to the next visible marker after the current position (see <see cref="MarkerTolerance"/>).
	/// </summary>
	/// <returns>The marker sought to, or <c>null</c> when there is none.</returns>
	public Task<PDMediaMarker?> NextMarkerAsync() => NextMarkerAsync(null);

	/// <summary>
	/// Seeks to the next marker after the current position (see <see cref="MarkerTolerance"/>).
	/// </summary>
	/// <param name="kind">The kind of marker to go to, whether or not it is visible; <c>null</c> for any visible kind.</param>
	/// <returns>The marker sought to, or <c>null</c> when there is none.</returns>
	public async Task<PDMediaMarker?> NextMarkerAsync(string? kind)
	{
		var markers = GetNavigableMarkers(kind);
		var position = _scrubPosition ?? _pendingSeek ?? _position;
		var marker = MediaMarkerNavigator.Next(markers, MarkerCursor, MarkerTolerance);

		// markers past the end of the media all seek to the end, so once there they are not "next" any more
		while (marker is not null && MediaTimelineLayout.ClampPosition(marker.Position - NonNegativeSeekLead, _duration) <= position)
		{
			marker = MediaMarkerNavigator.Next(markers, marker.Position, MarkerTolerance);
		}

		if (marker is not null)
		{
			await SeekToMarkerAsync(marker).ConfigureAwait(true);
		}

		return marker;
	}

	/// <summary>
	/// Seeks to the previous visible marker: the start of the current marker when more than
	/// <see cref="MarkerTolerance"/> into it, otherwise the marker before.
	/// </summary>
	/// <returns>The marker sought to, or <c>null</c> when there is none.</returns>
	public Task<PDMediaMarker?> PreviousMarkerAsync() => PreviousMarkerAsync(null);

	/// <summary>
	/// Seeks to the previous marker: the start of the current marker when more than <see cref="MarkerTolerance"/>
	/// into it, otherwise the marker before.
	/// </summary>
	/// <param name="kind">The kind of marker to go to, whether or not it is visible; <c>null</c> for any visible kind.</param>
	/// <returns>The marker sought to, or <c>null</c> when there is none.</returns>
	public async Task<PDMediaMarker?> PreviousMarkerAsync(string? kind)
	{
		var marker = MediaMarkerNavigator.Previous(GetNavigableMarkers(kind), MarkerCursor, MarkerTolerance);
		if (marker is not null)
		{
			await SeekToMarkerAsync(marker).ConfigureAwait(true);
		}

		return marker;
	}

	/// <summary>
	/// Gets whether markers of a kind are shown.
	/// </summary>
	/// <param name="kind">The kind.</param>
	/// <returns>Whether the kind is visible.</returns>
	public bool IsKindVisible(string kind) => _visibleKinds is null || _visibleKinds.Contains(kind?.Trim() ?? string.Empty);

	/// <summary>
	/// Shows or hides the markers of a kind, as the kind's toggle chip does, and raises
	/// <see cref="VisibleKindsChanged"/>.
	/// </summary>
	/// <param name="kind">The kind.</param>
	/// <param name="visible">Whether to show it.</param>
	/// <returns>A task that completes when the change has been applied.</returns>
	public async Task SetKindVisibleAsync(string kind, bool visible)
	{
		var normalized = kind?.Trim() ?? string.Empty;
		if (IsKindVisible(normalized) == visible)
		{
			return;
		}

		var kinds = new HashSet<string>(_visibleKinds ?? _kindCounts.Select(k => k.Key), MediaMarkerNavigator.KindComparer);
		if (visible)
		{
			kinds.Add(normalized);
		}
		else
		{
			kinds.Remove(normalized);
		}

		_visibleKinds = kinds;
		_lastVisibleKindsParameter = kinds;
		UpdateVisibleMarkers();
		await VisibleKindsChanged.InvokeAsync(kinds).ConfigureAwait(true);
		await RefreshAsync().ConfigureAwait(true);
	}

	private Task ToggleKindAsync(string kind) => SetKindVisibleAsync(kind, !IsKindVisible(kind));

	private IReadOnlyList<PDMediaMarker> GetNavigableMarkers(string? kind)
		=> kind is null ? _visibleMarkers : MediaMarkerNavigator.Filter(_sortedMarkers, kind);

	private void ApplyMarkerParameters()
	{
		var markersChanged = ApplyMarkersParameter();
		var kindsChanged = ApplyVisibleKindsParameter();
		if (markersChanged || kindsChanged)
		{
			UpdateVisibleMarkers();
		}

		if (!ReferenceEquals(KindColors, _lastKindColors) || MarkerTickResolution != _lastTickResolution)
		{
			_lastKindColors = KindColors;
			_lastTickResolution = MarkerTickResolution;
			InvalidateTicks();
		}
	}

	// re-sorts the markers when the host passes a new collection, or the same list with a different count
	private bool ApplyMarkersParameter()
	{
		if (ReferenceEquals(Markers, _lastMarkers) && !HasMarkerCountChanged())
		{
			return false;
		}

		_lastMarkers = Markers;
		_sortedMarkers = MediaMarkerNavigator.Sort(Markers);
		_lastMarkerCount = _sortedMarkers.Count;
		_kindCounts = MediaMarkerNavigator.CountByKind(_sortedMarkers);
		return true;
	}

	private bool HasMarkerCountChanged()
		=> Markers is not null && Markers.TryGetNonEnumeratedCount(out var count) && count != _lastMarkerCount;

	private bool ApplyVisibleKindsParameter()
	{
		if (_visibleKindsInitialized && ReferenceEquals(VisibleKinds, _lastVisibleKindsParameter))
		{
			return false;
		}

		_visibleKindsInitialized = true;
		_lastVisibleKindsParameter = VisibleKinds;
		_visibleKinds = VisibleKinds is null ? null : new HashSet<string>(VisibleKinds.Select(NormalizeKind), MediaMarkerNavigator.KindComparer);
		return true;
	}

	private static string NormalizeKind(string? kind) => kind?.Trim() ?? string.Empty;

	private void UpdateVisibleMarkers()
	{
		_visibleMarkers = [.. MediaMarkerNavigator.Filter(_sortedMarkers, _visibleKinds)];
		InvalidateTicks();
	}

	private void InvalidateTicks() => _ticks = null;

	private IReadOnlyList<MediaMarkerTick> GetTicks()
		=> _ticks ??= MediaTimelineLayout.GetMarkerTicks(_visibleMarkers, _duration, MarkerTickResolution, GetMarkerColor);

	private string GetKindLabel(string kind)
	{
		var label = Lookup(KindLabels, kind);
		if (!string.IsNullOrWhiteSpace(label))
		{
			return label;
		}

		return string.IsNullOrEmpty(kind) ? DefaultKindLabel : kind;
	}

	private string GetKindColor(string kind)
	{
		var color = MediaTimelineLayout.SanitizeColor(Lookup(KindColors, kind));
		if (color is not null)
		{
			return color;
		}

		var index = 0;
		for (var i = 0; i < _kindCounts.Count; i++)
		{
			if (MediaMarkerNavigator.KindComparer.Equals(_kindCounts[i].Key, kind))
			{
				index = i;
				break;
			}
		}

		return MediaTimelineLayout.Palette[index % MediaTimelineLayout.Palette.Count];
	}

	private string GetMarkerColor(PDMediaMarker marker)
		=> MediaTimelineLayout.SanitizeColor(marker.Color) ?? GetKindColor(MediaMarkerNavigator.GetKind(marker));

	private static string? Lookup(IReadOnlyDictionary<string, string>? map, string kind)
	{
		if (map is null)
		{
			return null;
		}

		if (map.TryGetValue(kind, out var value))
		{
			return value;
		}

		foreach (var pair in map)
		{
			if (MediaMarkerNavigator.KindComparer.Equals(pair.Key?.Trim(), kind))
			{
				return pair.Value;
			}
		}

		return null;
	}
}
