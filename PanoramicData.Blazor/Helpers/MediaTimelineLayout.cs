namespace PanoramicData.Blazor.Helpers;

/// <summary>
/// The arithmetic behind the seek bar of a <see cref="PDMediaPlayer"/>: percentages, buffered ranges, the shaded
/// range, marker ticks and colours.
/// </summary>
internal static partial class MediaTimelineLayout
{
	/// <summary>The default number of slots the seek bar is divided into when merging marker ticks.</summary>
	internal const int DefaultTickResolution = 400;

	/// <summary>The colours given to kinds that have no colour of their own, in order of kind.</summary>
	internal static IReadOnlyList<string> Palette { get; } =
	[
		"#0d6efd",
		"#dc3545",
		"#fd7e14",
		"#198754",
		"#6f42c1",
		"#d63384",
		"#20c997",
		"#ffc107",
		"#0dcaf0",
		"#6c757d"
	];

	/// <summary>
	/// Converts a position to a percentage of the duration, limited to 0 to 100.
	/// </summary>
	/// <param name="position">The position.</param>
	/// <param name="duration">The duration; zero or negative gives 0.</param>
	/// <returns>The percentage.</returns>
	internal static double ToPercent(TimeSpan position, TimeSpan duration)
		=> duration <= TimeSpan.Zero ? 0 : Math.Clamp(position.TotalSeconds / duration.TotalSeconds * 100, 0, 100);

	/// <summary>
	/// Limits a position to the media: not before the start and, when the duration is known, not after the end.
	/// </summary>
	/// <param name="position">The position.</param>
	/// <param name="duration">The duration, if known.</param>
	/// <returns>The limited position.</returns>
	internal static TimeSpan ClampPosition(TimeSpan position, TimeSpan? duration)
	{
		if (position < TimeSpan.Zero)
		{
			return TimeSpan.Zero;
		}

		return duration is { } d && position > d ? d : position;
	}

	/// <summary>
	/// Converts the flat list of buffered ranges reported by the browser to bar segments.
	/// </summary>
	/// <param name="buffered">Start and end pairs, in seconds.</param>
	/// <param name="duration">The duration, if known; without it nothing is drawn.</param>
	/// <returns>The segments.</returns>
	internal static IReadOnlyList<MediaBarSegment> GetBufferedSegments(IReadOnlyList<double>? buffered, TimeSpan? duration)
	{
		if (buffered is null || duration is not { } d || d <= TimeSpan.Zero)
		{
			return [];
		}

		var segments = new List<MediaBarSegment>(buffered.Count / 2);
		for (var i = 0; i + 1 < buffered.Count; i += 2)
		{
			if (!double.IsFinite(buffered[i]) || !double.IsFinite(buffered[i + 1]))
			{
				continue;
			}

			var left = ToPercent(TimeSpan.FromSeconds(Math.Max(0, buffered[i])), d);
			var right = ToPercent(TimeSpan.FromSeconds(Math.Max(0, buffered[i + 1])), d);
			if (right > left)
			{
				segments.Add(new MediaBarSegment(left, right - left));
			}
		}

		return segments;
	}

	/// <summary>
	/// Gets the shaded segment for a range. A missing start means the start of the media and a missing end its end.
	/// </summary>
	/// <param name="start">The start of the range.</param>
	/// <param name="end">The end of the range.</param>
	/// <param name="duration">The duration, if known.</param>
	/// <returns>The segment, or <c>null</c> when there is no range, no known duration or the range is empty.</returns>
	internal static MediaBarSegment? GetRangeSegment(TimeSpan? start, TimeSpan? end, TimeSpan? duration)
	{
		if ((start is null && end is null) || duration is not { } d || d <= TimeSpan.Zero)
		{
			return null;
		}

		var left = ToPercent(start ?? TimeSpan.Zero, d);
		var right = ToPercent(end ?? d, d);
		return right > left ? new MediaBarSegment(left, right - left) : null;
	}

	/// <summary>
	/// Determines whether a position is inside a range, the start included and the end excluded.
	/// </summary>
	/// <param name="position">The position.</param>
	/// <param name="start">The start of the range; a missing start means the start of the media.</param>
	/// <param name="end">The end of the range; a missing end means the end of the media.</param>
	/// <returns>Whether the position is inside, or <c>null</c> when no range is set.</returns>
	internal static bool? IsInRange(TimeSpan position, TimeSpan? start, TimeSpan? end)
	{
		if (start is null && end is null)
		{
			return null;
		}

		return position >= (start ?? TimeSpan.Zero) && (end is null || position < end.Value);
	}

	/// <summary>
	/// Builds the ticks for the markers on the seek bar. The bar is divided into <paramref name="resolution"/>
	/// slots; markers of the same kind, colour and class that start in the same slot, or that overlap a span already
	/// drawn, are merged, so at most <paramref name="resolution"/> ticks are drawn per kind however many markers there
	/// are. Markers past the end of the media are drawn at the end.
	/// </summary>
	/// <param name="sorted">The visible markers, sorted by position.</param>
	/// <param name="duration">The duration, if known; without it nothing is drawn.</param>
	/// <param name="resolution">The number of slots; values below 1 count as 1.</param>
	/// <param name="getColor">Gets the colour of a marker.</param>
	/// <returns>The ticks, grouped by kind and ordered by position within a kind.</returns>
	internal static IReadOnlyList<MediaMarkerTick> GetMarkerTicks(
		IReadOnlyList<PDMediaMarker> sorted,
		TimeSpan? duration,
		int resolution,
		Func<PDMediaMarker, string> getColor)
	{
		if (duration is not { } d || d <= TimeSpan.Zero || sorted.Count == 0)
		{
			return [];
		}

		resolution = Math.Max(1, resolution);
		var ticks = new List<MediaMarkerTick>();
		foreach (var group in sorted.GroupBy(m => (Kind: MediaMarkerNavigator.GetKind(m).ToUpperInvariant(), Color: getColor(m), m.CssClass)))
		{
			TickBuilder? current = null;
			foreach (var marker in group)
			{
				var start = ClampPosition(marker.Position, d);
				var end = ClampPosition(marker.End, d);
				var startPercent = ToPercent(start, d);
				var endPercent = ToPercent(end, d);
				var startSlot = Math.Min(resolution - 1, (int)Math.Floor(startPercent / 100 * resolution));
				var endSlot = Math.Min(resolution - 1, Math.Max(startSlot, (int)Math.Ceiling(endPercent / 100 * resolution) - 1));
				if (current is not null && startSlot <= current.EndSlot)
				{
					current.Add(marker, end, endPercent, endSlot);
					continue;
				}

				if (current is not null)
				{
					ticks.Add(current.Build());
				}

				current = new TickBuilder(marker, group.Key.Color, start, end, startPercent, endPercent, endSlot);
			}

			ticks.Add(current!.Build());
		}

		return ticks;
	}

	/// <summary>
	/// Gets the step of the seek slider: a tenth of a second for media shorter than a minute, otherwise a second.
	/// </summary>
	/// <param name="duration">The duration, if known.</param>
	/// <returns>The step, in seconds.</returns>
	internal static double GetSeekStep(TimeSpan? duration) => duration is { } d && d < TimeSpan.FromMinutes(1) ? 0.1 : 1;

	/// <summary>
	/// Converts a number of updates per second to the interval between them, limiting the rate to 1 to 30 a second.
	/// </summary>
	/// <param name="updatesPerSecond">The rate; a value that is not a finite number gives the default of 4.</param>
	/// <returns>The interval in milliseconds.</returns>
	internal static int GetUpdateIntervalMilliseconds(double updatesPerSecond)
	{
		var rate = double.IsFinite(updatesPerSecond) ? Math.Clamp(updatesPerSecond, 1, 30) : 4;
		return (int)Math.Round(1000 / rate);
	}

	/// <summary>
	/// Returns a CSS colour if it is safe to put in a style attribute (a <c>#</c> hex colour, a colour name, a colour
	/// function such as <c>rgb(...)</c> or <c>hsl(...)</c>, or <c>var(--name)</c>), otherwise <c>null</c>.
	/// </summary>
	/// <param name="color">The colour.</param>
	/// <returns>The colour, or <c>null</c>.</returns>
	internal static string? SanitizeColor(string? color)
		=> color?.Trim() is { Length: > 0 } trimmed && SafeColorRegex().IsMatch(trimmed) ? trimmed : null;

	/// <summary>
	/// Describes a tick for its tooltip: the time and label of a single marker, or the number and time span of merged
	/// markers.
	/// </summary>
	/// <param name="tick">The tick.</param>
	/// <param name="kindLabel">The display name of the tick's kind.</param>
	/// <param name="duration">The duration, used to decide whether to show hours.</param>
	/// <returns>The description.</returns>
	internal static string DescribeTick(MediaMarkerTick tick, string kindLabel, TimeSpan? duration)
	{
		var start = MediaTimeFormatter.Format(tick.Start, duration);
		if (tick.Count == 1)
		{
			var time = tick.IsSpan ? $"{start} to {MediaTimeFormatter.Format(tick.End, duration)}" : start;
			return string.IsNullOrWhiteSpace(tick.First.Label)
				? $"{time} {kindLabel}"
				: $"{time} {kindLabel}: {tick.First.Label}";
		}

		return string.Create(
			CultureInfo.InvariantCulture,
			$"{tick.Count} {kindLabel} markers, {start} to {MediaTimeFormatter.Format(tick.End, duration)}");
	}

	[GeneratedRegex(@"^(#[0-9a-fA-F]{3,8}|[a-zA-Z]+|(rgba?|hsla?|hwb|lab|lch|oklab|oklch)\([0-9a-zA-Z\s.,%/+\-]*\)|var\(--[\w\-]+\))$")]
	private static partial Regex SafeColorRegex();

	private sealed class TickBuilder(
		PDMediaMarker first,
		string color,
		TimeSpan start,
		TimeSpan end,
		double startPercent,
		double endPercent,
		int endSlot)
	{
		private int _count = 1;
		private bool _isSpan = first.IsSpan;

		public int EndSlot { get; private set; } = endSlot;

		public void Add(PDMediaMarker marker, TimeSpan markerEnd, double markerEndPercent, int markerEndSlot)
		{
			_count++;
			_isSpan |= marker.IsSpan;
			if (markerEnd > end)
			{
				end = markerEnd;
			}

			endPercent = Math.Max(endPercent, markerEndPercent);
			EndSlot = Math.Max(EndSlot, markerEndSlot);
		}

		public MediaMarkerTick Build() => new(
			startPercent,
			endPercent - startPercent,
			MediaMarkerNavigator.GetKind(first),
			color,
			first.CssClass,
			first,
			_count,
			start,
			end,
			_isSpan);
	}
}
