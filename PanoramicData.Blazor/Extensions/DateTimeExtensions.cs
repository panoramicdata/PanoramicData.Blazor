using PanoramicData.Blazor.Models;
using System;

namespace PanoramicData.Blazor.Extensions
{
	public static class DateTimeExtensions
	{
		public static DateTime AddPeriods(this DateTime dateTime, TimelineScales scale, int periods)
		{
			if(periods == 0)
			{
				return dateTime;
			}
			return scale switch
			{
				TimelineScales.Minutes => dateTime.AddMinutes(periods),
				TimelineScales.Hours => dateTime.AddHours(periods),
				TimelineScales.Hours4 => dateTime.AddHours(periods * 4),
				TimelineScales.Hours6 => dateTime.AddHours(periods * 6),
				TimelineScales.Hours8 => dateTime.AddHours(periods * 8),
				TimelineScales.Hours12 => dateTime.AddHours(periods * 12),
				TimelineScales.Weeks => dateTime.AddDays(periods * 7),
				TimelineScales.Months => dateTime.AddMonths(periods),
				TimelineScales.Years => dateTime.AddYears(periods),
				_ => dateTime.AddDays(periods)
			};
		}

		public static DateTime PeriodStart(this DateTime dateTime, TimelineScales scale)
		{
			switch (scale)
			{
				case TimelineScales.Minutes:
					return new DateTime(dateTime.Year, dateTime.Month, dateTime.Day, dateTime.Hour, dateTime.Minute, 0);

				case TimelineScales.Hours:
					return new DateTime(dateTime.Year, dateTime.Month, dateTime.Day, dateTime.Hour, 0, 0);

				case TimelineScales.Hours4:
					return new DateTime(dateTime.Year, dateTime.Month, dateTime.Day, (dateTime.Hour / 4) * 4, 0, 0);

				case TimelineScales.Hours6:
					return new DateTime(dateTime.Year, dateTime.Month, dateTime.Day, (dateTime.Hour / 6) * 6, 0, 0);

				case TimelineScales.Hours8:
					return new DateTime(dateTime.Year, dateTime.Month, dateTime.Day, (dateTime.Hour / 8) * 8, 0, 0);

				case TimelineScales.Hours12:
					return new DateTime(dateTime.Year, dateTime.Month, dateTime.Day, (dateTime.Hour / 12) * 12, 0, 0);

				case TimelineScales.Weeks:
					return new DateTime(dateTime.Year, dateTime.Month, dateTime.Day).AddDays(-(int)dateTime.DayOfWeek);

				case TimelineScales.Months:
					return new DateTime(dateTime.Year, dateTime.Month, 1);

				case TimelineScales.Years:
					return new DateTime(dateTime.Year, 1, 1);

				default:
					return new DateTime(dateTime.Year, dateTime.Month, dateTime.Day);
			}
		}

		public static DateTime PeriodEnd(this DateTime dateTime, TimelineScales scale)
		{
			switch (scale)
			{
				case TimelineScales.Minutes:
					return dateTime.PeriodStart(scale).AddMinutes(1);

				case TimelineScales.Hours:
					return dateTime.PeriodStart(scale).AddHours(1);

				case TimelineScales.Hours4:
					return dateTime.PeriodStart(scale).AddHours(4);

				case TimelineScales.Hours6:
					return dateTime.PeriodStart(scale).AddHours(6);

				case TimelineScales.Hours8:
					return dateTime.PeriodStart(scale).AddHours(8);

				case TimelineScales.Hours12:
					return dateTime.PeriodStart(scale).AddHours(12);

				case TimelineScales.Weeks:
					return dateTime.PeriodStart(scale).AddDays(7);

				case TimelineScales.Months:
					return dateTime.PeriodStart(scale).AddMonths(1);

				case TimelineScales.Years:
					return dateTime.PeriodStart(scale).AddYears(1);

				default:
					return dateTime.PeriodStart(scale).AddDays(1);
			}
		}

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

		public static int TotalPeriodsSince(this DateTime end, DateTime start, TimelineScales scale)
		{
			var temp = scale switch
			{
				TimelineScales.Minutes => end.Subtract(start).TotalMinutes,
				TimelineScales.Hours => end.Subtract(start).TotalHours,
				TimelineScales.Hours4 => end.Subtract(start).TotalHours / 4,
				TimelineScales.Hours6 => end.Subtract(start).TotalHours / 6,
				TimelineScales.Hours8 => end.Subtract(start).TotalHours / 8,
				TimelineScales.Hours12 => end.Subtract(start).TotalHours / 12,
				TimelineScales.Weeks => end.Subtract(start).TotalDays / 7,
				TimelineScales.Months => end.TotalMonthsSince(start),
				TimelineScales.Years => end.TotalYearsSince(start),
				_ => end.Subtract(start).TotalDays
			};
			return (int)Math.Ceiling(temp);
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
}
