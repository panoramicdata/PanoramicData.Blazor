using PanoramicData.Blazor.Helpers;

namespace PanoramicData.Blazor;

public partial class PDMediaPlayer
{
	private const string KeyShortcuts = "K J L [ ] M < >";

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

	private async Task OnKeyDownAsync(KeyboardEventArgs args)
	{
		switch (MediaKeyboard.Resolve(args))
		{
			case MediaShortcut.TogglePlay:
				await TogglePlayAsync().ConfigureAwait(true);
				break;
			case MediaShortcut.PreviousMarker:
				await PreviousMarkerAsync().ConfigureAwait(true);
				break;
			case MediaShortcut.NextMarker:
				await NextMarkerAsync().ConfigureAwait(true);
				break;
			case MediaShortcut.ToggleMute:
				await ToggleMuteAsync().ConfigureAwait(true);
				break;
			case MediaShortcut.Slower:
				await SetPlaybackRateAsync(MediaPlaybackRates.Step(_rates, _rate, faster: false)).ConfigureAwait(true);
				break;
			case MediaShortcut.Faster:
				await SetPlaybackRateAsync(MediaPlaybackRates.Step(_rates, _rate, faster: true)).ConfigureAwait(true);
				break;
			default:
				// not a shortcut: the key keeps its usual meaning
				break;
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
}
