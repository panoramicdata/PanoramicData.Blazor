using PanoramicData.Blazor.Helpers;
using System.Globalization;

namespace PanoramicData.Blazor.Demo.Pages;

public partial class PDCalendarPage
{
	/// <summary>
	/// The week the United Kingdom's clocks go back: Sunday 25 October 2026 is 25 hours long.
	/// </summary>
	private static readonly DateOnly _clockChangeDate = new(2026, 10, 25);

	private static readonly TimeZoneInfo _london = TimeZoneInfo.FindSystemTimeZoneById("Europe/London");

	private readonly CalendarView[] _views = [CalendarView.Month, CalendarView.Week, CalendarView.Agenda];
	private readonly DayOfWeek[] _firstDays = [DayOfWeek.Monday, DayOfWeek.Sunday, DayOfWeek.Saturday];

	private IReadOnlyList<Broadcast> _items = [];
	private int _loadCount;
	private bool _isLoading;
	private Broadcast? _selected;
	private string _lastSlot = "none";

	[CascadingParameter] protected EventManager? EventManager { get; set; }

	protected DateOnly? Date { get; set; }

	protected CalendarView View { get; set; } = CalendarView.Week;

	protected DayOfWeek FirstDayOfWeek { get; set; } = DayOfWeek.Monday;

	protected int FirstVisibleHour { get; set; }

	protected int LastVisibleHour { get; set; } = 24;

	protected int MaxItemsPerDay { get; set; } = 3;

	protected bool NowIndicator { get; set; } = true;

	protected bool ShadeWeekends { get; set; } = true;

	protected bool UseBounds { get; set; }

	protected bool SlowLoading { get; set; } = true;

	private DateOnly? MinDate => UseBounds ? new DateOnly(2026, 9, 1) : null;

	private DateOnly? MaxDate => UseBounds ? new DateOnly(2026, 12, 31) : null;

	/// <summary>
	/// A scheduled broadcast on one of two fictional stations.
	/// </summary>
	public sealed record Broadcast(string Id, string Station, string Title, DateTimeOffset Start, DateTimeOffset End)
	{
		public bool IsRepeat { get; init; }

		public string CssClass => Station switch
		{
			"City FM" => "demo-cal-city",
			"Coast Radio" => "demo-cal-coast",
			_ => "demo-cal-special"
		};
	}

	/// <summary>
	/// Loads the broadcasts for the range the calendar is showing, as a host would from a database or API.
	/// </summary>
	private async Task OnVisibleRangeChangedAsync((DateTimeOffset From, DateTimeOffset To) range)
	{
		var load = ++_loadCount;
		_isLoading = true;
		EventManager?.Add(new Event(
			"OnVisibleRangeChanged",
			new EventArgument("From", range.From.ToString("yyyy-MM-dd HH:mm zzz", CultureInfo.InvariantCulture)),
			new EventArgument("To", range.To.ToString("yyyy-MM-dd HH:mm zzz", CultureInfo.InvariantCulture))));

		if (SlowLoading)
		{
			await Task.Delay(400).ConfigureAwait(true);
		}

		// a later request has overtaken this one, so its results are stale
		if (load != _loadCount)
		{
			return;
		}

		_items = GenerateSchedule(range.From, range.To);
		_isLoading = false;
	}

	private void OnItemSelected(Broadcast broadcast)
		=> EventManager?.Add(new Event("OnItemSelected", new EventArgument("Title", broadcast.Title), new EventArgument("Station", broadcast.Station)));

	private void OnItemDoubleClick(Broadcast broadcast)
		=> EventManager?.Add(new Event("OnItemDoubleClick", new EventArgument("Title", broadcast.Title)));

	private void OnSlotSelected(DateTimeOffset slot)
	{
		_lastSlot = slot.ToString("ddd d MMM yyyy HH:mm zzz", CultureInfo.InvariantCulture);
		EventManager?.Add(new Event("OnSlotSelected", new EventArgument("Start", _lastSlot)));
	}

	private void ShowClockChange()
	{
		Date = _clockChangeDate;
		View = CalendarView.Week;
	}

	private static string? GetDayCssClass(DateOnly date)
		=> CalendarMath.GetDayLength(date, _london) == TimeSpan.FromHours(24) ? null : "demo-cal-clock-change";

	private static string GetTooltip(Broadcast broadcast)
	{
		var start = TimeZoneInfo.ConvertTime(broadcast.Start, _london);
		var end = TimeZoneInfo.ConvertTime(broadcast.End, _london);
		return $"{broadcast.Station}: {broadcast.Title}, {start:ddd HH:mm} to {end:ddd HH:mm} ({(broadcast.End - broadcast.Start).TotalHours:0.#} h)";
	}

	/// <summary>
	/// Builds a deterministic two-station schedule for every day touching a range.
	/// </summary>
	private static List<Broadcast> GenerateSchedule(DateTimeOffset from, DateTimeOffset to)
	{
		var first = CalendarMath.GetDate(from, _london).AddDays(-1);
		var last = CalendarMath.GetDate(to, _london);
		var broadcasts = new List<Broadcast>();

		for (var date = first; date <= last; date = date.AddDays(1))
		{
			broadcasts.AddRange(GetCityFm(date));
			broadcasts.AddRange(GetCoastRadio(date));
		}

		broadcasts.AddRange(GetSpecials());

		return [.. broadcasts.Where(b => b.End > from && b.Start < to)];
	}

	private static IEnumerable<Broadcast> GetCityFm(DateOnly date)
	{
		var isWeekend = date.DayOfWeek is DayOfWeek.Saturday or DayOfWeek.Sunday;

		yield return Show("City FM", date, "Through the Night", 2, 6, isRepeat: true);

		if (isWeekend)
		{
			yield return Show("City FM", date, "Weekend Breakfast", 6, 10);
			yield return Show("City FM", date, date.DayOfWeek == DayOfWeek.Saturday ? "Saturday Sport" : "Sunday Brunch", 10, 14);
			yield return Show("City FM", date, "The Chart Show", 14, 18);
			yield return Show("City FM", date, "Party Anthems", 18, 22);
		}
		else
		{
			yield return Show("City FM", date, "Breakfast", 6, 9);
			yield return Show("City FM", date, "Mornings", 9, 12);
			yield return Show("City FM", date, "Lunchtime", 12, 13);
			yield return Show("City FM", date, "Afternoons", 13, 16);
			yield return Show("City FM", date, "Drivetime", 16, 19);
			yield return Show("City FM", date, "Evening Session", 19, 22);
		}

		// runs past midnight into the next day
		yield return Show("City FM", date, "Late Night Request Show", 22, 26);
	}

	private static IEnumerable<Broadcast> GetCoastRadio(DateOnly date)
	{
		yield return Show("Coast Radio", date, "Coastal Breakfast", 7, 10);

		if (date.DayOfWeek is not (DayOfWeek.Saturday or DayOfWeek.Sunday))
		{
			// these two overlap each other and City FM's Lunchtime: three columns, chained
			yield return Show("Coast Radio", date, "News at Noon", 12, 12.5);
			yield return Show("Coast Radio", date, "Phone-in", 12.25, 13.25);
			yield return Show("Coast Radio", date, "Traffic and Travel", 17, 17.25);
		}

		yield return Show("Coast Radio", date, "Album of the Week", 18, 20);
	}

	private static IEnumerable<Broadcast> GetSpecials()
	{
		// a multi-day outside broadcast across the weekend the clocks go back
		yield return new Broadcast(
			"festival-2026",
			"Specials",
			"Harbour Festival, live",
			At(new DateOnly(2026, 10, 23), 18),
			At(new DateOnly(2026, 10, 25), 23));

		// the repeated hour: from 01:00 BST to 01:00 GMT is a whole real hour, and on to 02:00 GMT is another
		var extraHourStart = At(_clockChangeDate, 1);
		yield return new Broadcast("extra-hour-2026", "Specials", "The Extra Hour", extraHourStart, extraHourStart.AddHours(2));
	}

	private static Broadcast Show(string station, DateOnly date, string title, double startHour, double endHour, bool isRepeat = false)
		=> new($"{station}-{date:yyyyMMdd}-{startHour}", station, title, At(date, startHour), At(date, endHour)) { IsRepeat = isRepeat };

	/// <summary>
	/// Converts a wall clock time on a date in London to an instant; hours of 24 or more run into the following days.
	/// </summary>
	private static DateTimeOffset At(DateOnly date, double hour)
		=> CalendarMath.ToInstant(date.ToDateTime(TimeOnly.MinValue).AddHours(hour), _london);
}
