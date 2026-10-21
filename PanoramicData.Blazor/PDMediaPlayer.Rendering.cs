using PanoramicData.Blazor.Helpers;

namespace PanoramicData.Blazor;

public partial class PDMediaPlayer
{
	private const string KeyShortcuts = "K J L [ ] M < > , . P";

	private string MarkerListId => $"{Id}-markers";

	private string AccessibleName => string.IsNullOrWhiteSpace(Title) ? "Media player" : Title;

	private bool HasSource => !string.IsNullOrWhiteSpace(_src);

	private bool CanSeek => HasSource && _isReady && !_isLive && _duration is { } d && d > TimeSpan.Zero;

	private bool CanNavigateMarkers => HasSource && _visibleMarkers.Count > 0;

	private TimeSpan DisplayPosition => _scrubPosition ?? _pendingSeek ?? _position;

	private string PositionText => MediaTimeFormatter.Format(DisplayPosition, _duration);

	private string DurationText => _isLive ? "Live" : _duration is { } d ? MediaTimeFormatter.Format(d, d) : "-:--";

	private double DurationSeconds => _duration?.TotalSeconds ?? 0;

	private double ProgressPercent => _duration is { } d ? MediaTimelineLayout.ToPercent(DisplayPosition, d) : 0;

	private string SeekValueText => _duration is { } d
		? $"{MediaTimeFormatter.ToSpokenText(DisplayPosition)} of {MediaTimeFormatter.ToSpokenText(d)}"
		: MediaTimeFormatter.ToSpokenText(DisplayPosition);

	private string SeekStepText => MediaTimelineLayout.GetSeekStep(_duration).ToString(CultureInfo.InvariantCulture);

	private string RateValue => FormatNumber(_rate);

	private string VolumePercentText => string.Create(CultureInfo.InvariantCulture, $"{Math.Round(_volume * 100)}%");

	private string DownloadHref => string.IsNullOrWhiteSpace(DownloadUrl) ? _src ?? string.Empty : DownloadUrl;

	private IReadOnlyList<MediaBarSegment> BufferedSegments => MediaTimelineLayout.GetBufferedSegments(_buffered, _duration);

	private MediaBarSegment? RangeSegment => MediaTimelineLayout.GetRangeSegment(RangeStart, RangeEnd, _duration);

	private string PreloadValue => Preload switch
	{
		MediaPreload.None => "none",
		MediaPreload.Auto => "auto",
		_ => "metadata"
	};

	private string GetRootClass()
	{
		var classes = new StringBuilder("pd-media-player");
		classes.Append(MarkerListPlacement == MediaMarkerListPlacement.Side ? " pdmp-side" : " pdmp-below");
		classes.Append(_kind == MediaKind.Video ? " pdmp-video-player" : " pdmp-audio-player");
		if (_isPlaying)
		{
			classes.Append(" pdmp-playing");
		}

		if (!string.IsNullOrWhiteSpace(CssClass))
		{
			classes.Append(' ').Append(CssClass);
		}

		return classes.ToString();
	}

	private static string FormatNumber(double value) => value.ToString("0.###", CultureInfo.InvariantCulture);

	private static string Percent(double value) => FormatNumber(value) + "%";

	private static string GetSegmentStyle(MediaBarSegment segment) => $"left:{Percent(segment.LeftPercent)};width:{Percent(segment.WidthPercent)}";

	private static string GetTickStyle(MediaMarkerTick tick)
		=> $"left:{Percent(tick.LeftPercent)};width:{Percent(tick.WidthPercent)};--pdmp-marker-color:{tick.Color}";

	private static string GetTickClass(MediaMarkerTick tick)
		=> $"pdmp-tick{(tick.IsSpan ? " pdmp-tick-span" : string.Empty)}{(tick.Count > 1 ? " pdmp-tick-merged" : string.Empty)} {tick.CssClass}".TrimEnd();

	private string GetTickTitle(MediaMarkerTick tick) => MediaTimelineLayout.DescribeTick(tick, GetKindLabel(tick.Kind), _duration);

	private string GetMarkerTimeText(PDMediaMarker marker)
	{
		var start = MediaTimeFormatter.Format(MediaTimelineLayout.ClampPosition(marker.Position, null), _duration);
		return marker.IsSpan ? $"{start} to {MediaTimeFormatter.Format(marker.End, _duration)}" : start;
	}

	private string GetMarkerStyle(PDMediaMarker marker) => $"--pdmp-marker-color:{GetMarkerColor(marker)}";

	private string GetKindChipStyle(string kind) => $"--pdmp-marker-color:{GetKindColor(kind)}";

	// what each keyboard shortcut does
	private static readonly Dictionary<MediaShortcut, Func<PDMediaPlayer, Task>> _shortcutActions = new()
	{
		[MediaShortcut.TogglePlay] = player => player.TogglePlayAsync(),
		[MediaShortcut.PreviousMarker] = player => player.PreviousMarkerAsync(),
		[MediaShortcut.NextMarker] = player => player.NextMarkerAsync(),
		[MediaShortcut.ToggleMute] = player => player.ToggleMuteAsync(),
		[MediaShortcut.Slower] = player => player.SetPlaybackRateAsync(MediaPlaybackRates.Step(player._rates, player._rate, faster: false)),
		[MediaShortcut.Faster] = player => player.SetPlaybackRateAsync(MediaPlaybackRates.Step(player._rates, player._rate, faster: true)),
		[MediaShortcut.PreviousSegment] = player => player.PreviousSegmentAsync(),
		[MediaShortcut.NextSegment] = player => player.NextSegmentAsync(),
		[MediaShortcut.PlaySegment] = player => player.PlayCurrentSegmentAsync()
	};

	private async Task OnKeyDownAsync(KeyboardEventArgs args)
	{
		// a key that is not a shortcut keeps its usual meaning
		if (_shortcutActions.TryGetValue(MediaKeyboard.Resolve(args), out var action))
		{
			await action(this).ConfigureAwait(true);
		}
	}

	private void OnSeekInput(ChangeEventArgs args)
	{
		if (TryParse(args.Value, out var seconds))
		{
			_scrubPosition = MediaTimelineLayout.ClampPosition(TimeSpan.FromSeconds(seconds), _duration);
		}
	}

	private async Task OnSeekChangeAsync(ChangeEventArgs args)
	{
		if (TryParse(args.Value, out var seconds))
		{
			await SeekAsync(TimeSpan.FromSeconds(seconds)).ConfigureAwait(true);
		}

		_scrubPosition = null;
	}

	private async Task OnVolumeInputAsync(ChangeEventArgs args)
	{
		if (TryParse(args.Value, out var volume))
		{
			await SetVolumeAsync(volume).ConfigureAwait(true);
		}
	}

	private async Task OnRateChangeAsync(ChangeEventArgs args)
	{
		if (TryParse(args.Value, out var rate))
		{
			await SetPlaybackRateAsync(rate).ConfigureAwait(true);
		}
	}

	private void ToggleMarkerList() => _isMarkerListExpanded = !_isMarkerListExpanded;

	private Task SkipBackAsync() => SkipAsync(TimeSpan.FromSeconds(-Math.Abs(SkipBackSeconds)));

	private Task SkipForwardAsync() => SkipAsync(TimeSpan.FromSeconds(Math.Abs(SkipForwardSeconds)));

	private static bool TryParse(object? value, out double result)
	{
		result = 0;
		return value?.ToString() is { } text
			&& double.TryParse(text, NumberStyles.Float, CultureInfo.InvariantCulture, out result)
			&& double.IsFinite(result)
			&& Math.Abs(result) < TimeSpan.MaxValue.TotalSeconds / 2;
	}

	private string SegmentListId => $"{Id}-segments";

	private string TranscriptId => $"{Id}-transcript";

	private int SegmentLaneCount => GetBars().Select(b => b.Lane + 1).DefaultIfEmpty(0).Max();

	private bool CanNavigateSegments => HasSource && _visibleSegments.Count > 0;

	private static string GetBarStyle(MediaSegmentBar bar)
		=> $"left:{Percent(bar.LeftPercent)};width:{Percent(bar.WidthPercent)};--pdmp-segment-lane:{bar.Lane};--pdmp-marker-color:{bar.Color}";

	private string GetBarClass(MediaSegmentBar bar)
	{
		var classes = new StringBuilder("pdmp-segment-bar");
		classes.Append(bar.Start <= MarkerCursor && MarkerCursor < bar.End ? " pdmp-segment-current" : string.Empty);
		classes.Append(bar.Count > 1 ? " pdmp-segment-merged" : string.Empty);
		return $"{classes} {bar.First.CssClass}".TrimEnd();
	}

	private string GetBarTitle(MediaSegmentBar bar)
		=> MediaSegmentLayout.DescribeBar(bar, GetKindLabel(MediaSegmentNavigator.GetKind(bar.First)), _duration);

	private static string GetLanesStyle(int lanes) => $"--pdmp-segment-lanes:{lanes}";

	private string GetSegmentTimeText(PDMediaSegment segment)
		=> $"{MediaTimeFormatter.Format(segment.Start, _duration)} to {MediaTimeFormatter.Format(segment.End, _duration)}";

	private static string GetSegmentLengthText(PDMediaSegment segment) => MediaTimeFormatter.Format(segment.Length, false);

	private string GetSegmentColorStyle(PDMediaSegment segment) => $"--pdmp-marker-color:{GetSegmentColor(segment)}";

	private string GetSegmentChipStyle(string kind) => $"--pdmp-marker-color:{GetKindColor(kind, _segmentKindCounts)}";

	private static string? GetLineSpeaker(PDMediaTextLine line) => string.IsNullOrWhiteSpace(line.Speaker) ? null : $"{line.Speaker}: ";

	private Task SeekToLineAsync(PDMediaTextLine line) => SeekAsync(line.Start);
}