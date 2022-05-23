namespace PanoramicData.Blazor.Extensions;

public static class DateTimeExtensions
{
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
