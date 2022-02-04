using PanoramicData.Blazor.Extensions;
using System;
using System.Globalization;

namespace PanoramicData.Blazor.Models
{
	public class TimelineScale
	{
		private CultureInfo _cultureInfo;
		private Calendar _calendar;

		public TimelineScale(string name, TimelineUnits unitType, int unitCount)
		{
			_cultureInfo = CultureInfo.CurrentUICulture;
			_calendar = _cultureInfo.Calendar;
			Name = name;
			UnitType = unitType;
			UnitCount = unitCount;
		}

		public string Name { get; private set; }

		public TimelineUnits UnitType { get; private set; }

		public int UnitCount { get; private set; }

		public DateTime AddPeriods(DateTime dateTime, int periods)
		{
			if (periods == 0)
			{
				return dateTime;
			}
			return UnitType switch
			{
				TimelineUnits.Milliseconds => _calendar.AddMilliseconds(dateTime, periods * UnitCount),
				TimelineUnits.Seconds => _calendar.AddSeconds(dateTime, periods * UnitCount),
				TimelineUnits.Minutes => _calendar.AddMinutes(dateTime, periods * UnitCount),
				TimelineUnits.Hours => _calendar.AddHours(dateTime, periods * UnitCount),
				TimelineUnits.Days => _calendar.AddDays(dateTime, periods * UnitCount),
				TimelineUnits.Weeks => _calendar.AddWeeks(dateTime, periods * UnitCount),
				TimelineUnits.Months => _calendar.AddMonths(dateTime, periods * UnitCount),
				_ => dateTime.AddYears(periods * UnitCount)
			};
		}

		public int PeriodsBetween(DateTime start, DateTime end)
		{
			var temp = UnitType switch
			{
				TimelineUnits.Milliseconds => end.Subtract(start).TotalMilliseconds,
				TimelineUnits.Seconds => end.Subtract(start).TotalSeconds,
				TimelineUnits.Minutes => end.Subtract(start).TotalMinutes,
				TimelineUnits.Hours => end.Subtract(start).TotalHours / UnitCount,
				TimelineUnits.Days => end.Subtract(start).TotalDays,
				TimelineUnits.Weeks => end.Subtract(start).TotalDays / 7,
				TimelineUnits.Months => end.TotalMonthsSince(start),
				_ => end.TotalYearsSince(start)
			};
			return (int)Math.Ceiling(temp);
		}

		public DateTime PeriodEnd(DateTime pointInTime)
		{
			return UnitType switch
			{
				TimelineUnits.Milliseconds => _calendar.AddMilliseconds(PeriodStart(pointInTime), UnitCount),
				TimelineUnits.Seconds => _calendar.AddSeconds(PeriodStart(pointInTime), UnitCount),
				TimelineUnits.Minutes => _calendar.AddMinutes(PeriodStart(pointInTime), UnitCount),
				TimelineUnits.Hours => _calendar.AddHours(PeriodStart(pointInTime), UnitCount),
				TimelineUnits.Days => _calendar.AddDays(PeriodStart(pointInTime), UnitCount),
				TimelineUnits.Weeks => _calendar.AddWeeks(PeriodStart(pointInTime), UnitCount),
				TimelineUnits.Months => _calendar.AddMonths(PeriodStart(pointInTime), UnitCount),
				_ => _calendar.AddYears(PeriodStart(pointInTime), UnitCount),
			};
		}

		public DateTime PeriodStart(DateTime pointInTime)
		{
			return UnitType switch
			{
				TimelineUnits.Milliseconds => new DateTime(pointInTime.Year,
												pointInTime.Month,
												pointInTime.Day,
												pointInTime.Hour,
												pointInTime.Minute,
												pointInTime.Second,
												Round((int)_calendar.GetMilliseconds(pointInTime))),
				TimelineUnits.Seconds => new DateTime(pointInTime.Year,
												pointInTime.Month,
												pointInTime.Day,
												pointInTime.Hour,
												pointInTime.Minute,
												Round(_calendar.GetSecond(pointInTime))),
				TimelineUnits.Minutes => new DateTime(pointInTime.Year,
												pointInTime.Month,
												pointInTime.Day,
												pointInTime.Hour,
												Round(_calendar.GetMinute(pointInTime)), 0),
				TimelineUnits.Hours => new DateTime(pointInTime.Year,
												pointInTime.Month,
												pointInTime.Day,
												Round(_calendar.GetHour(pointInTime)), 0, 0),
				TimelineUnits.Days => new DateTime(pointInTime.Year,
												pointInTime.Month,
												Round(_calendar.GetDayOfMonth(pointInTime))),
				TimelineUnits.Weeks => _calendar.AddWeeks(new DateTime(pointInTime.Year, 1, 1), Round(_calendar.GetWeekOfYear(pointInTime, CalendarWeekRule.FirstDay, DayOfWeek.Sunday))),
				TimelineUnits.Months => new DateTime(pointInTime.Year, Round(_calendar.GetMonth(pointInTime)), 1),
				_ => new DateTime(Round(_calendar.GetYear(pointInTime)), 1, 1),
			};
		}

		private int Round(int value)
		{
			return UnitCount == 1 ? value : (value / UnitCount) * UnitCount;
		}

		public override string ToString()
		{
			return Name;
		}

		#region Class Members

		public static TimelineScale Years => new TimelineScale("1 Year", TimelineUnits.Years, 1);
		public static TimelineScale Months => new TimelineScale("1 Month", TimelineUnits.Months, 1);
		public static TimelineScale Weeks => new TimelineScale("1 Week", TimelineUnits.Weeks, 1);
		public static TimelineScale Days => new TimelineScale("1 Day", TimelineUnits.Days, 1);
		public static TimelineScale Hours => new TimelineScale("1 Hour", TimelineUnits.Hours, 1);
		public static TimelineScale Hours4 => new TimelineScale("4 Hours", TimelineUnits.Hours, 4);
		public static TimelineScale Hours6 => new TimelineScale("6 Hours", TimelineUnits.Hours, 6);
		public static TimelineScale Hours8 => new TimelineScale("8 Hours", TimelineUnits.Hours, 8);
		public static TimelineScale Hours12 => new TimelineScale("12 Hours", TimelineUnits.Hours,12);
		public static TimelineScale Minutes => new TimelineScale("1 Minute", TimelineUnits.Minutes, 1);

		#endregion
	}
}
