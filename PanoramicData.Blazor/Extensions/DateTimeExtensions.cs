namespace PanoramicData.Blazor.Extensions;

/// <summary>
/// Extension methods for <see cref="System.DateTime"/> values.
/// </summary>
public static class DateTimeExtensions
{
	/// <summary>
	/// Returns the number of complete calendar months from <paramref name="start"/> up to but not including <paramref name="end"/>.
	/// Returns 0 when <paramref name="start"/> is on or after <paramref name="end"/>.
	/// </summary>
	/// <param name="end">The later date that defines the end of the range.</param>
	/// <param name="start">The earlier date that defines the start of the range.</param>
	/// <returns>The number of complete months between <paramref name="start"/> and <paramref name="end"/>, or 0 if <paramref name="start"/> >= <paramref name="end"/>.</returns>
	public static int TotalMonthsSince(this DateTime end, DateTime start)
	{
		// start must be before end
		if (start < end)
		{
			// check for same month
			if (new DateTime(end.Year, end.Month, 1) == new DateTime(start.Year, start.Month, 1))
			{
				return 0;
			}

			// step through months
			var d = start;
			var months = 0;
			while (d < end)
			{
				d = d.AddMonths(1);
				months++;
			}

			return months;
		}
		else
		{
			return 0;
		}
	}

	/// <summary>
	/// Returns the number of complete calendar years from <paramref name="start"/> up to but not including <paramref name="end"/>.
	/// Returns 0 when <paramref name="start"/> is on or after <paramref name="end"/>.
	/// </summary>
	/// <param name="end">The later date that defines the end of the range.</param>
	/// <param name="start">The earlier date that defines the start of the range.</param>
	/// <returns>The number of complete years between <paramref name="start"/> and <paramref name="end"/>, or 0 if <paramref name="start"/> >= <paramref name="end"/>.</returns>
	public static int TotalYearsSince(this DateTime end, DateTime start)
	{
		// start must be before end
		if (start < end)
		{
			// check for same year
			if (new DateTime(end.Year, 1, 1) == new DateTime(start.Year, 1, 1))
			{
				return 0;
			}

			// step through years
			var d = start;
			var years = 0;
			while (d < end)
			{
				d = d.AddYears(1);
				years++;
			}

			return years;
		}
		else
		{
			return 0;
		}
	}
}
