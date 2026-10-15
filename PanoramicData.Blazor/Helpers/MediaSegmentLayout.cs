namespace PanoramicData.Blazor.Helpers;

/// <summary>
/// Lays out the segment lanes of a <see cref="PDMediaPlayer"/>: packs overlapping segments into lanes and builds the
/// bars to draw, merging very short neighbours so that thousands of segments stay cheap to render.
/// </summary>
internal static class MediaSegmentLayout
{
	/// <summary>
	/// Assigns each segment a lane: its own <see cref="PDMediaSegment.Lane"/> when set, otherwise the first lane whose
	/// previous segment has ended. Lanes beyond <paramref name="maxLanes"/> fold into the last lane.
	/// </summary>
	/// <param name="sorted">The segments, sorted by start.</param>
	/// <param name="maxLanes">The most lanes to use; values below 1 count as 1.</param>
	/// <returns>The lane of each segment, in the same order.</returns>
	internal static IReadOnlyList<int> PackLanes(IReadOnlyList<PDMediaSegment> sorted, int maxLanes)
	{
		maxLanes = Math.Max(1, maxLanes);
		var laneEnds = new List<TimeSpan>();
		var lanes = new List<int>(sorted.Count);
		foreach (var segment in sorted)
		{
			var lane = Math.Min(maxLanes - 1, segment.Lane is { } forced and >= 0 ? forced : FirstFreeLane(laneEnds, segment.Start));
			while (laneEnds.Count <= lane)
			{
				laneEnds.Add(TimeSpan.MinValue);
			}

			laneEnds[lane] = laneEnds[lane] > segment.End ? laneEnds[lane] : segment.End;
			lanes.Add(lane);
		}

		return lanes;
	}

	/// <summary>
	/// Builds the bars for the segments. Each lane is divided into <paramref name="resolution"/> slots; segments
	/// shorter than a slot that start in the slot where the previous short segment of the same lane and kind ends
	/// are merged into one bar. Segments past the end of the media are drawn up to the end.
	/// </summary>
	/// <param name="sorted">The visible segments, sorted by start.</param>
	/// <param name="duration">The duration, if known; without it nothing is drawn.</param>
	/// <param name="maxLanes">The most lanes to use.</param>
	/// <param name="resolution">The number of slots; values below 1 count as 1.</param>
	/// <param name="getColor">Gets the colour of a segment.</param>
	/// <returns>The bars.</returns>
	internal static IReadOnlyList<MediaSegmentBar> GetBars(
		IReadOnlyList<PDMediaSegment> sorted,
		TimeSpan? duration,
		int maxLanes,
		int resolution,
		Func<PDMediaSegment, string> getColor)
	{
		if (duration is not { } d || d <= TimeSpan.Zero || sorted.Count == 0)
		{
			return [];
		}

		var slot = TimeSpan.FromTicks(Math.Max(1, d.Ticks / Math.Max(1, resolution)));
		var lanes = PackLanes(sorted, maxLanes);
		var bars = new List<MediaSegmentBar>();
		var lastByLane = new Dictionary<int, int>();
		for (var i = 0; i < sorted.Count; i++)
		{
			var bar = ToBar(sorted[i], lanes[i], d, getColor(sorted[i]));
			if (bar is null)
			{
				continue;
			}

			if (lastByLane.TryGetValue(bar.Lane, out var index) && CanMerge(bars[index], bar, slot))
			{
				bars[index] = Merge(bars[index], bar, d);
				continue;
			}

			lastByLane[bar.Lane] = bars.Count;
			bars.Add(bar);
		}

		return bars;
	}

	/// <summary>
	/// Describes a bar for its tooltip: the label, kind and time range of one segment (with its confidence and
	/// source when known), or the number and time span of merged segments.
	/// </summary>
	/// <param name="bar">The bar.</param>
	/// <param name="kindLabel">The display name of the bar's kind.</param>
	/// <param name="duration">The duration, used to decide whether to show hours.</param>
	/// <returns>The description.</returns>
	internal static string DescribeBar(MediaSegmentBar bar, string kindLabel, TimeSpan? duration)
	{
		var range = $"{MediaTimeFormatter.Format(bar.Start, duration)} to {MediaTimeFormatter.Format(bar.End, duration)}";
		if (bar.Count > 1)
		{
			return string.Create(CultureInfo.InvariantCulture, $"{bar.Count} {kindLabel} segments, {range}");
		}

		var text = new StringBuilder(string.IsNullOrWhiteSpace(bar.First.Label) ? kindLabel : $"{bar.First.Label} ({kindLabel})");
		text.Append(", ").Append(range);
		AppendDetails(text, bar.First);
		return text.ToString();
	}

	private static void AppendDetails(StringBuilder text, PDMediaSegment segment)
	{
		if (segment.Confidence is { } confidence)
		{
			text.Append(CultureInfo.InvariantCulture, $", {Math.Round(Math.Clamp(confidence, 0, 1) * 100)}% confidence");
		}

		if (!string.IsNullOrWhiteSpace(segment.Source))
		{
			text.Append(", from ").Append(segment.Source);
		}
	}

	private static int FirstFreeLane(List<TimeSpan> laneEnds, TimeSpan start)
	{
		var free = laneEnds.FindIndex(end => end <= start);
		return free >= 0 ? free : laneEnds.Count;
	}

	private static MediaSegmentBar? ToBar(PDMediaSegment segment, int lane, TimeSpan duration, string color)
	{
		var start = MediaTimelineLayout.ClampPosition(segment.Start, duration);
		var end = MediaTimelineLayout.ClampPosition(segment.End, duration);
		if (end <= start)
		{
			return null;
		}

		var left = MediaTimelineLayout.ToPercent(start, duration);
		return new MediaSegmentBar(lane, left, MediaTimelineLayout.ToPercent(end, duration) - left, color, segment, 1, start, end);
	}

	// two short bars of the same kind and colour merge when the second starts within a slot of the first's end
	private static bool CanMerge(MediaSegmentBar current, MediaSegmentBar next, TimeSpan slot)
		=> current.End - current.Start < slot
			&& next.End - next.Start < slot
			&& next.Start - current.End < slot
			&& current.Color == next.Color
			&& MediaMarkerNavigator.KindComparer.Equals(MediaSegmentNavigator.GetKind(current.First), MediaSegmentNavigator.GetKind(next.First));

	private static MediaSegmentBar Merge(MediaSegmentBar current, MediaSegmentBar next, TimeSpan duration)
	{
		var end = current.End > next.End ? current.End : next.End;
		return current with
		{
			WidthPercent = MediaTimelineLayout.ToPercent(end, duration) - current.LeftPercent,
			Count = current.Count + next.Count,
			End = end
		};
	}
}
