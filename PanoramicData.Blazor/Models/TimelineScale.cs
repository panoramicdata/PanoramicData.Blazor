﻿namespace PanoramicData.Blazor.Models;

public class TimelineScale : IComparable
{
	private readonly CultureInfo _cultureInfo;
	private readonly Calendar _calendar;

	public TimelineScale(string name, TimelineUnits unitType, int unitCount)
	{
		// validate
		if (unitType > TimelineUnits.Hours && unitCount > 1)
		{
			throw new ArgumentOutOfRangeException(nameof(unitCount), "Unit Count can only be 1 when Unit Type is greater than hours");
		}

		_cultureInfo = CultureInfo.CurrentUICulture;
		_calendar = _cultureInfo.Calendar;
		Name = name;
		UnitType = unitType;
		UnitCount = unitCount;
	}

	public virtual CalendarWeekRule CalendarWeekRule { get; set; } = CalendarWeekRule.FirstDay;

	public virtual DayOfWeek CalendarDayOfWeek { get; set; } = DayOfWeek.Sunday;

	public string Name { get; private set; }

	public TimelineUnits UnitType { get; private set; }

	public int UnitCount { get; private set; }

	public DateTime AddPeriods(DateTime dateTime, int periods)
	{
		if (periods == 0 || dateTime == DateTime.MinValue)
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

	public virtual string FormatPattern()
	{
		return FormatPattern("d");
	}

	public virtual string FormatPattern(string dateFormat) => UnitType switch
	{
		TimelineUnits.Years => "yyyy",
		TimelineUnits.Months => "MMM yyyy",
		TimelineUnits.Hours => $"{dateFormat} HH:00",
		TimelineUnits.Minutes => $"{dateFormat} HH:mm",
		_ => dateFormat,
	};


	public virtual bool IsMajorTick(DateTime dateTime) => UnitType switch
	{
		TimelineUnits.Years => dateTime.Year % 2 == 0,
		TimelineUnits.Months => dateTime.Month == 1,
		TimelineUnits.Weeks => dateTime.Month == 1 && dateTime.Day <= 7,
		TimelineUnits.Days => dateTime.Day == 1,
		TimelineUnits.Hours => UnitCount < 12 ? dateTime.Hour == 0 : dateTime.Hour == 0 && (dateTime.DayOfYear) % 2 == 0,
		TimelineUnits.Minutes => dateTime.Minute == 0,
		TimelineUnits.Milliseconds => dateTime.Second == 0,
		_ => false
	};

	public int PeriodsBetween(DateTime start, DateTime end)
		=> PeriodsBetween(start, end, true);

	public int PeriodsBetween(DateTime start, DateTime end, bool roundUp)
	{
		var temp = UnitType switch
		{
			TimelineUnits.Milliseconds => end.Subtract(start).TotalMilliseconds,
			TimelineUnits.Seconds => end.Subtract(start).TotalSeconds,
			TimelineUnits.Minutes => end.Subtract(start).TotalMinutes,
			TimelineUnits.Hours => end.Subtract(start).TotalHours,
			TimelineUnits.Days => end.Subtract(start).TotalDays,
			TimelineUnits.Weeks => end.Subtract(start).TotalDays / 7,
			TimelineUnits.Months => end.TotalMonthsSince(start),
			_ => end.TotalYearsSince(start)
		};
		temp /= UnitCount;
		return (int)(roundUp ? Math.Ceiling(temp) : Math.Floor(temp));
	}

	public DateTime PeriodEnd(DateTime dateTime) => UnitType switch
	{
		TimelineUnits.Milliseconds => _calendar.AddMilliseconds(PeriodStart(dateTime), UnitCount),
		TimelineUnits.Seconds => _calendar.AddSeconds(PeriodStart(dateTime), UnitCount),
		TimelineUnits.Minutes => _calendar.AddMinutes(PeriodStart(dateTime), UnitCount),
		TimelineUnits.Hours => _calendar.AddHours(PeriodStart(dateTime), UnitCount),
		TimelineUnits.Days => _calendar.AddDays(PeriodStart(dateTime), UnitCount),
		TimelineUnits.Weeks => _calendar.AddWeeks(PeriodStart(dateTime), UnitCount),
		TimelineUnits.Months => _calendar.AddMonths(PeriodStart(dateTime), UnitCount),
		_ => _calendar.AddYears(PeriodStart(dateTime), UnitCount),
	};

	public DateTime PeriodStart(DateTime dateTime)
	{
		return UnitType switch
		{
			TimelineUnits.Milliseconds => new DateTime(dateTime.Year,
											dateTime.Month,
											dateTime.Day,
											dateTime.Hour,
											dateTime.Minute,
											dateTime.Second,
											Round((int)_calendar.GetMilliseconds(dateTime))),
			TimelineUnits.Seconds => new DateTime(dateTime.Year,
											dateTime.Month,
											dateTime.Day,
											dateTime.Hour,
											dateTime.Minute,
											Round(_calendar.GetSecond(dateTime))),
			TimelineUnits.Minutes => new DateTime(dateTime.Year,
											dateTime.Month,
											dateTime.Day,
											dateTime.Hour,
											Round(_calendar.GetMinute(dateTime)), 0),
			TimelineUnits.Hours => new DateTime(dateTime.Year,
											dateTime.Month,
											dateTime.Day,
											Round(_calendar.GetHour(dateTime)), 0, 0),
			TimelineUnits.Days => new DateTime(dateTime.Year,
											dateTime.Month,
											Round(_calendar.GetDayOfMonth(dateTime))),
			TimelineUnits.Weeks => dateTime.Date.AddDays(-(int)dateTime.DayOfWeek),
			TimelineUnits.Months => new DateTime(dateTime.Year, Round(_calendar.GetMonth(dateTime)), 1),
			_ => new DateTime(Round(_calendar.GetYear(dateTime)), 1, 1),
		};
	}


	private int Round(int value) => UnitCount == 1 ? value : (value / UnitCount) * UnitCount;

	public virtual string TickLabelMajor(DateTime dateTime)
		=> TickLabelMajor(dateTime, "d");

	public virtual string TickLabelMajor(DateTime dateTime, string dateFormat)
	{
		var pattern = UnitType switch
		{
			TimelineUnits.Milliseconds => $"{dateFormat} HH:mm:ss",
			TimelineUnits.Seconds => $"{dateFormat} HH:mm:ss",
			TimelineUnits.Minutes => $"{dateFormat} HH:00",
			TimelineUnits.Hours => dateFormat,
			TimelineUnits.Days => $"yyyy-MM",
			TimelineUnits.Weeks => "yyyy",
			TimelineUnits.Months => "yyyy",
			TimelineUnits.Years => "yyyy",
			_ => ""
		};
		return dateTime.ToString(pattern, CultureInfo.InvariantCulture);
	}

	public virtual string TickLabelMinor(DateTime dateTime)
	{
		var pattern = UnitType switch
		{
			TimelineUnits.Milliseconds => "fff",
			TimelineUnits.Seconds => "ss",
			TimelineUnits.Minutes => "mm",
			TimelineUnits.Hours => "HH",
			TimelineUnits.Days => "dd",
			TimelineUnits.Months => "MM",
			_ => "yy"
		};
		if (UnitType == TimelineUnits.Weeks)
		{
			var woy = _calendar.GetWeekOfYear(dateTime, CalendarWeekRule, CalendarDayOfWeek);
			return woy.ToString("00", CultureInfo.InvariantCulture);
		}

		return dateTime.ToString(pattern, CultureInfo.InvariantCulture);
	}

	public override string ToString() => Name;

	#region IComparable

	public int CompareTo(object? obj)
	{
		if (obj is TimelineScale ts)
		{
			// check for equality
			if (UnitType == ts.UnitType && UnitCount == ts.UnitCount)
			{
				return 0;
			}

			// check if current instance precedes given scale
			if (UnitType < ts.UnitType || (UnitType == ts.UnitType && UnitCount < ts.UnitCount))
			{
				return -1;
			}

			// current instance must follow given scale
			return 1;
		}

		throw new ArgumentException("Object is not a TimelineScale");
	}

	#endregion

	#region Class Members

	public static TimelineScale Years => new("Years", TimelineUnits.Years, 1);
	public static TimelineScale Months => new("Months", TimelineUnits.Months, 1);
	public static TimelineScale Weeks => new("Weeks", TimelineUnits.Weeks, 1);
	public static TimelineScale Days => new("Days", TimelineUnits.Days, 1);
	public static TimelineScale Hours => new("Hours", TimelineUnits.Hours, 1);
	public static TimelineScale Hours4 => new("4 Hours", TimelineUnits.Hours, 4);
	public static TimelineScale Hours6 => new("6 Hours", TimelineUnits.Hours, 6);
	public static TimelineScale Hours8 => new("8 Hours", TimelineUnits.Hours, 8);
	public static TimelineScale Hours12 => new("12 Hours", TimelineUnits.Hours, 12);
	public static TimelineScale Minutes => new("Minutes", TimelineUnits.Minutes, 1);
	public static TimelineScale Minutes5 => new("5 Minutes", TimelineUnits.Minutes, 5);
	public static TimelineScale Minutes10 => new("10 Minutes", TimelineUnits.Minutes, 10);
	public static TimelineScale Minutes15 => new("15 Minutes", TimelineUnits.Minutes, 15);
	public static TimelineScale Seconds => new("Seconds", TimelineUnits.Seconds, 1);

	#endregion
}
