namespace PanoramicData.Blazor.Models;

#pragma warning disable CA1036 // Comparison operators not needed for model class
#pragma warning disable S1210 // "Equals" and the comparison operators should be overridden when implementing "IComparable"
/// <summary>
/// Describes a timeline scale and provides range/tick calculations for that scale.
/// </summary>
public class TimelineScale : IComparable
#pragma warning restore S1210 // "Equals" and the comparison operators should be overridden when implementing "IComparable"
#pragma warning restore CA1036
{
	private readonly CultureInfo _cultureInfo;
	private readonly Calendar _calendar;

	/// <summary>
	/// Initializes a new timeline scale.
	/// </summary>
	/// <param name="name">Display name.</param>
	/// <param name="unitType">Underlying unit type.</param>
	/// <param name="unitCount">Number of units per scale step.</param>
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

	/// <summary>
	/// Gets or sets the week rule used for week-based calculations.
	/// </summary>
	public virtual CalendarWeekRule CalendarWeekRule { get; set; } = CalendarWeekRule.FirstDay;

	/// <summary>
	/// Gets or sets the first day of week used for week-based calculations.
	/// </summary>
	public virtual DayOfWeek CalendarDayOfWeek { get; set; } = DayOfWeek.Sunday;

	/// <summary>
	/// Gets the display name of this scale.
	/// </summary>
	public string Name { get; private set; }

	/// <summary>
	/// Gets the underlying unit type.
	/// </summary>
	public TimelineUnits UnitType { get; private set; }

	/// <summary>
	/// Gets the number of units per scale step.
	/// </summary>
	public int UnitCount { get; private set; }

	/// <summary>
	/// Adds a number of periods to the supplied date/time.
	/// </summary>
	/// <param name="dateTime">Input date/time.</param>
	/// <param name="periods">Number of periods to add.</param>
	/// <returns>The adjusted date/time.</returns>
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

	/// <summary>
	/// Gets the preferred date format pattern for this scale.
	/// </summary>
	/// <returns>Format pattern string.</returns>
	public virtual string FormatPattern()
	{
		return FormatPattern("d");
	}

	/// <summary>
	/// Gets the preferred date format pattern for this scale using a custom base format.
	/// </summary>
	/// <param name="dateFormat">Base date format.</param>
	/// <returns>Format pattern string.</returns>
	public virtual string FormatPattern(string dateFormat) => UnitType switch
	{
		TimelineUnits.Years => "yyyy",
		TimelineUnits.Months => "MMM yyyy",
		TimelineUnits.Hours => $"{dateFormat} HH:00",
		TimelineUnits.Minutes => $"{dateFormat} HH:mm",
		_ => dateFormat,
	};


	/// <summary>
	/// Determines whether a date/time is a major tick for this scale.
	/// </summary>
	/// <param name="dateTime">Date/time to test.</param>
	/// <returns>True if the date/time is a major tick.</returns>
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

	/// <summary>
	/// Calculates the number of periods between two dates.
	/// </summary>
	/// <param name="start">Start date.</param>
	/// <param name="end">End date.</param>
	/// <returns>Rounded-up periods count.</returns>
	public int PeriodsBetween(DateTime start, DateTime end)
		=> PeriodsBetween(start, end, true);

	/// <summary>
	/// Calculates the number of periods between two dates.
	/// </summary>
	/// <param name="start">Start date.</param>
	/// <param name="end">End date.</param>
	/// <param name="roundUp">True to round up, false to round down.</param>
	/// <returns>Periods count.</returns>
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

	/// <summary>
	/// Gets the end of the current period that contains the supplied date/time.
	/// </summary>
	/// <param name="dateTime">Input date/time.</param>
	/// <returns>Period end date/time.</returns>
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

	/// <summary>
	/// Gets the start of the current period that contains the supplied date/time.
	/// </summary>
	/// <param name="dateTime">Input date/time.</param>
	/// <returns>Period start date/time.</returns>
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

	/// <summary>
	/// Gets the major tick label for a date/time using the default date format.
	/// </summary>
	/// <param name="dateTime">Input date/time.</param>
	/// <returns>Major tick label.</returns>
	public virtual string TickLabelMajor(DateTime dateTime)
		=> TickLabelMajor(dateTime, "d");

	/// <summary>
	/// Gets the major tick label for a date/time.
	/// </summary>
	/// <param name="dateTime">Input date/time.</param>
	/// <param name="dateFormat">Base date format.</param>
	/// <returns>Major tick label.</returns>
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

	/// <summary>
	/// Gets the minor tick label for a date/time.
	/// </summary>
	/// <param name="dateTime">Input date/time.</param>
	/// <returns>Minor tick label.</returns>
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

	/// <summary>
	/// Returns the display name of this scale.
	/// </summary>
	/// <returns>Scale name.</returns>
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

	/// <summary>
	/// Gets the yearly timeline scale.
	/// </summary>
	public static TimelineScale Years => new("Years", TimelineUnits.Years, 1);
	/// <summary>
	/// Gets the monthly timeline scale.
	/// </summary>
	public static TimelineScale Months => new("Months", TimelineUnits.Months, 1);
	/// <summary>
	/// Gets the weekly timeline scale.
	/// </summary>
	public static TimelineScale Weeks => new("Weeks", TimelineUnits.Weeks, 1);
	/// <summary>
	/// Gets the daily timeline scale.
	/// </summary>
	public static TimelineScale Days => new("Days", TimelineUnits.Days, 1);
	/// <summary>
	/// Gets the hourly timeline scale.
	/// </summary>
	public static TimelineScale Hours => new("Hours", TimelineUnits.Hours, 1);
	/// <summary>
	/// Gets the 4-hour timeline scale.
	/// </summary>
	public static TimelineScale Hours4 => new("4 Hours", TimelineUnits.Hours, 4);
	/// <summary>
	/// Gets the 6-hour timeline scale.
	/// </summary>
	public static TimelineScale Hours6 => new("6 Hours", TimelineUnits.Hours, 6);
	/// <summary>
	/// Gets the 8-hour timeline scale.
	/// </summary>
	public static TimelineScale Hours8 => new("8 Hours", TimelineUnits.Hours, 8);
	/// <summary>
	/// Gets the 12-hour timeline scale.
	/// </summary>
	public static TimelineScale Hours12 => new("12 Hours", TimelineUnits.Hours, 12);
	/// <summary>
	/// Gets the minute-based timeline scale.
	/// </summary>
	public static TimelineScale Minutes => new("Minutes", TimelineUnits.Minutes, 1);
	/// <summary>
	/// Gets the 5-minute timeline scale.
	/// </summary>
	public static TimelineScale Minutes5 => new("5 Minutes", TimelineUnits.Minutes, 5);
	/// <summary>
	/// Gets the 10-minute timeline scale.
	/// </summary>
	public static TimelineScale Minutes10 => new("10 Minutes", TimelineUnits.Minutes, 10);
	/// <summary>
	/// Gets the 15-minute timeline scale.
	/// </summary>
	public static TimelineScale Minutes15 => new("15 Minutes", TimelineUnits.Minutes, 15);
	/// <summary>
	/// Gets the second-based timeline scale.
	/// </summary>
	public static TimelineScale Seconds => new("Seconds", TimelineUnits.Seconds, 1);

	#endregion
}
