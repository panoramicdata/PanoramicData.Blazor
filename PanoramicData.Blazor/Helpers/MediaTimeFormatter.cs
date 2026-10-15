namespace PanoramicData.Blazor.Helpers;

/// <summary>
/// Formats media positions and durations for display and for screen readers.
/// </summary>
public static class MediaTimeFormatter
{
	/// <summary>
	/// Formats a time as <c>h:mm:ss</c> when <paramref name="includeHours"/> is true or the time is an hour or
	/// more, and otherwise as <c>m:ss</c>. Fractions of a second are dropped and negative times show as zero.
	/// </summary>
	/// <param name="value">The time to format.</param>
	/// <param name="includeHours">Whether to show the hours even when they are zero.</param>
	/// <returns>The formatted time, for example <c>1:02:03</c> or <c>2:03</c>.</returns>
	public static string Format(TimeSpan value, bool includeHours)
	{
		var totalSeconds = value <= TimeSpan.Zero ? 0L : (long)Math.Floor(value.TotalSeconds);
		var hours = totalSeconds / 3600;
		var minutes = totalSeconds / 60 % 60;
		var seconds = totalSeconds % 60;
		return includeHours || hours > 0
			? string.Create(CultureInfo.InvariantCulture, $"{hours}:{minutes:00}:{seconds:00}")
			: string.Create(CultureInfo.InvariantCulture, $"{minutes}:{seconds:00}");
	}

	/// <summary>
	/// Formats a time, showing the hours when the time or the duration it belongs to is an hour or more, so that the
	/// position and the duration line up.
	/// </summary>
	/// <param name="value">The time to format.</param>
	/// <param name="duration">The duration of the media, if known.</param>
	/// <returns>The formatted time.</returns>
	public static string Format(TimeSpan value, TimeSpan? duration) => Format(value, duration is { } d && d >= TimeSpan.FromHours(1));

	/// <summary>
	/// Converts seconds reported by a media element to a time, or <c>null</c> when the value is missing, not a
	/// number or infinite. Negative values become zero.
	/// </summary>
	/// <param name="seconds">The seconds.</param>
	/// <returns>The time, or <c>null</c>.</returns>
	public static TimeSpan? FromSeconds(double? seconds)
	{
		if (seconds is not { } value || !double.IsFinite(value))
		{
			return null;
		}

		return value <= 0 ? TimeSpan.Zero : TimeSpan.FromSeconds(Math.Min(value, TimeSpan.MaxValue.TotalSeconds / 2));
	}

	/// <summary>
	/// Describes a time in words for assistive technology, for example <c>1 hour 2 minutes 3 seconds</c>.
	/// </summary>
	/// <param name="value">The time to describe.</param>
	/// <returns>The description.</returns>
	public static string ToSpokenText(TimeSpan value)
	{
		var totalSeconds = value <= TimeSpan.Zero ? 0L : (long)Math.Floor(value.TotalSeconds);
		var hours = totalSeconds / 3600;
		var minutes = totalSeconds / 60 % 60;
		var seconds = totalSeconds % 60;
		var parts = new List<string>(3);
		if (hours > 0)
		{
			parts.Add(Plural(hours, "hour"));
		}

		if (minutes > 0)
		{
			parts.Add(Plural(minutes, "minute"));
		}

		if (seconds > 0 || parts.Count == 0)
		{
			parts.Add(Plural(seconds, "second"));
		}

		return string.Join(' ', parts);
	}

	private static string Plural(long count, string unit)
		=> string.Create(CultureInfo.InvariantCulture, $"{count} {unit}{(count == 1 ? string.Empty : "s")}");
}
