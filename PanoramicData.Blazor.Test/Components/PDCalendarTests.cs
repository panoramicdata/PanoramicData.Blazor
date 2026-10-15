using AwesomeAssertions;
using Bunit;
using PanoramicData.Blazor.Enums;
using PanoramicData.Blazor.Test.Helpers;
using System.Globalization;

namespace PanoramicData.Blazor.Test.Components;

/// <summary>
/// Tests that <see cref="PDCalendar{TItem}"/> renders its month, week and agenda views, places items on the
/// right days and hours in its time zone (including across daylight saving changes), selects items and slots,
/// navigates by toolbar and keyboard within its date bounds, and reports its visible range.
/// </summary>
public partial class PDCalendarTests : BunitContext
{
	private static readonly TimeSpan _bst = TimeSpan.FromHours(1);
	private static readonly DateOnly _oct15 = new(2026, 10, 15);

	private readonly TestClock _clock = new(new DateTimeOffset(2026, 10, 15, 10, 30, 0, _bst));
	private readonly List<(DateTimeOffset From, DateTimeOffset To)> _ranges = [];
	private readonly Show _morning = new(1, "Morning show", At(15, 9), At(15, 10));
	private readonly Show _news = new(2, "News", At(15, 9, 30), At(15, 10, 30));
	private readonly Show _lateShow = new(3, "Late show", At(14, 22), At(15, 2));
	private readonly Show _festival = new(4, "Festival", At(20, 12), At(22, 12));

	/// <summary>Sets up the rendering context.</summary>
	public PDCalendarTests()
	{
		JSInterop.Mode = JSRuntimeMode.Loose;
	}

	/// <summary>A radio show, as a calendar item.</summary>
	/// <param name="Id">The identifier.</param>
	/// <param name="Title">The title.</param>
	/// <param name="Start">When the show starts.</param>
	/// <param name="End">When the show ends.</param>
	public sealed record Show(int Id, string Title, DateTimeOffset Start, DateTimeOffset End)
	{
		/// <summary>Gets a text that differs from the title, so tests can tell which one is shown.</summary>
		/// <returns>The text.</returns>
		public override string ToString() => $"Show {Id}";
	}

	private static DateTimeOffset At(int day, int hour, int minute = 0) => new(2026, 10, day, hour, minute, 0, _bst);

	private static string Long(DateOnly date) => date.ToString("dddd d MMMM yyyy", CultureInfo.CurrentCulture);

	private IRenderedComponent<PDCalendar<Show>> RenderCalendar(
		Action<ComponentParameterCollectionBuilder<PDCalendar<Show>>>? configure = null,
		IEnumerable<Show>? items = null,
		DateOnly? date = null,
		CalendarView view = CalendarView.Month)
		=> Render<PDCalendar<Show>>(parameters =>
		{
			parameters
				.Add(p => p.Items, items ?? [_morning, _news, _lateShow, _festival])
				.Add(p => p.StartSelector, s => s.Start)
				.Add(p => p.EndSelector, s => s.End)
				.Add(p => p.TitleSelector, s => s.Title)
				.Add(p => p.TimeZone, CalendarTestZones.London)
				.Add(p => p.Clock, _clock)
				.Add(p => p.Date, date ?? _oct15)
				.Add(p => p.View, view)
				.Add(p => p.OnVisibleRangeChanged, range => _ranges.Add(range));
			configure?.Invoke(parameters);
		});

	private static AngleSharp.Dom.IElement Day(IRenderedComponent<PDCalendar<Show>> calendar, DateOnly date)
		=> calendar.Find($"#{calendar.Instance.Id}-{date:yyyyMMdd}");

	/// <summary>
	/// Verifies that the month view shows whole weeks with outside days dimmed, today marked, weekends shaded and the focused date selected.
	/// </summary>
	[Fact]
	public void Month_ShowsWholeWeeksWithHighlights()
	{
		var calendar = RenderCalendar();

		var grid = calendar.Find(".pdcal-month");
		grid.GetAttribute("role").Should().Be("grid");
		calendar.FindAll(".pdcal-month [role=row]").Should().HaveCount(6);
		calendar.FindAll(".pdcal-month [role=columnheader]").Should().HaveCount(7);
		calendar.FindAll(".pdcal-month [role=gridcell]").Should().HaveCount(35);

		Day(calendar, new DateOnly(2026, 9, 28)).ClassList.Should().Contain("pdcal-day-outside");
		var today = Day(calendar, _oct15);
		today.ClassList.Should().Contain("pdcal-today").And.Contain("pdcal-focused").And.NotContain("pdcal-day-outside");
		today.GetAttribute("aria-current").Should().Be("date");
		today.GetAttribute("aria-selected").Should().Be("true");
		today.GetAttribute("aria-label").Should().Be($"{Long(_oct15)}, 3 items, today");
		Day(calendar, new DateOnly(2026, 10, 17)).ClassList.Should().Contain("pdcal-weekend");
		Day(calendar, new DateOnly(2026, 10, 16)).GetAttribute("aria-label").Should().Be($"{Long(new DateOnly(2026, 10, 16))}, 0 items");
		Day(calendar, new DateOnly(2026, 10, 14)).GetAttribute("aria-label").Should().Be($"{Long(new DateOnly(2026, 10, 14))}, 1 item");
		grid.GetAttribute("aria-activedescendant").Should().Be(today.Id);
	}

	/// <summary>
	/// Verifies that weekend shading and the weekend days can be changed, and that a day class selector adds its classes.
	/// </summary>
	[Fact]
	public void Month_DayHighlightsAreConfigurable()
	{
		var calendar = RenderCalendar(p => p
			.Add(x => x.WeekendDays, [DayOfWeek.Friday])
			.Add(x => x.DayCssClassSelector, d => d.Day == 1 ? "first-day" : null));

		Day(calendar, new DateOnly(2026, 10, 16)).ClassList.Should().Contain("pdcal-weekend");
		Day(calendar, new DateOnly(2026, 10, 17)).ClassList.Should().NotContain("pdcal-weekend");
		Day(calendar, new DateOnly(2026, 10, 1)).ClassList.Should().Contain("first-day");

		calendar.Render(p => p.Add(x => x.ShadeWeekends, false));
		calendar.FindAll(".pdcal-weekend").Should().BeEmpty();
	}

	/// <summary>
	/// Verifies that the month view lists items by start, with an item spanning midnight on both days and a multi-day item on each of its days.
	/// </summary>
	[Fact]
	public void Month_ShowsItemsOnEachDayTheyCover()
	{
		var calendar = RenderCalendar();

		Day(calendar, new DateOnly(2026, 10, 14)).QuerySelectorAll(".pdcal-item-title").Select(e => e.TextContent).Should().Equal("Late show");
		Day(calendar, _oct15).QuerySelectorAll(".pdcal-item-title").Select(e => e.TextContent).Should().Equal("Late show", "Morning show", "News");

		foreach (var day in new[] { 20, 21, 22 })
		{
			Day(calendar, new DateOnly(2026, 10, day)).QuerySelectorAll(".pdcal-item-title").Select(e => e.TextContent).Should().Equal("Festival");
		}

		var times = Day(calendar, _oct15).QuerySelectorAll(".pdcal-item-time").Select(e => e.TextContent).ToList();
		times.Should().Equal("Until 02:00", "09:00 to 10:00", "09:30 to 10:30");
		Day(calendar, new DateOnly(2026, 10, 21)).QuerySelector(".pdcal-item-time")!.TextContent.Should().Be("All day");
		Day(calendar, new DateOnly(2026, 10, 20)).QuerySelector(".pdcal-item-time")!.TextContent.Should().Be("From 12:00");

		var festival = Day(calendar, new DateOnly(2026, 10, 21)).QuerySelector(".pdcal-item")!;
		festival.ClassList.Should().Contain("pdcal-item-continues-before").And.Contain("pdcal-item-continues-after");
	}

	/// <summary>
	/// Verifies that a day with more items than the limit shows a "+n more" button that opens that day's agenda.
	/// </summary>
	[Fact]
	public async Task Month_MoreButton_OpensTheDayAgenda()
	{
		DateOnly? dateChanged = null;
		CalendarView? viewChanged = null;
		var calendar = RenderCalendar(p => p
			.Add(x => x.MaxItemsPerDay, 1)
			.Add(x => x.DateChanged, d => dateChanged = d)
			.Add(x => x.ViewChanged, v => viewChanged = v));

		var day = Day(calendar, new DateOnly(2026, 10, 14));
		day.QuerySelector(".pdcal-more").Should().BeNull();

		var more = Day(calendar, _oct15).QuerySelector(".pdcal-more")!;
		more.TextContent.Trim().Should().Be("+2 more");
		more.GetAttribute("aria-label").Should().Be($"Show all 3 items on {Long(_oct15)}");
		Day(calendar, _oct15).QuerySelectorAll(".pdcal-item").Should().ContainSingle();

		_ranges.Clear();
		await calendar.Find($"#{calendar.Instance.Id}-20261015 .pdcal-more").ClickAsync(new());

		calendar.Instance.CurrentView.Should().Be(CalendarView.Agenda);
		viewChanged.Should().Be(CalendarView.Agenda);
		dateChanged.Should().BeNull("the day was already focused");
		_ranges.Should().ContainSingle();
		calendar.FindAll(".pdcal-agenda-day").Should().NotBeEmpty();
	}

	/// <summary>
	/// Verifies that "+n more" on another day moves the focused date, and that asking for the agenda of a day out of bounds does nothing.
	/// </summary>
	[Fact]
	public async Task ShowDayAgenda_MovesTheDateWithinTheBounds()
	{
		DateOnly? dateChanged = null;
		var calendar = RenderCalendar(p => p
			.Add(x => x.MaxItemsPerDay, 0)
			.Add(x => x.MaxDate, new DateOnly(2026, 10, 31))
			.Add(x => x.DateChanged, d => dateChanged = d));

		await calendar.InvokeAsync(() => calendar.Instance.ShowDayAgendaAsync(new DateOnly(2026, 11, 1)));
		calendar.Instance.CurrentView.Should().Be(CalendarView.Month);

		await calendar.Find($"#{calendar.Instance.Id}-20261020 .pdcal-more").ClickAsync(new());
		dateChanged.Should().Be(new DateOnly(2026, 10, 20));
		calendar.Instance.CurrentView.Should().Be(CalendarView.Agenda);

		// already showing that day's agenda: nothing changes
		_ranges.Clear();
		await calendar.InvokeAsync(() => calendar.Instance.ShowDayAgendaAsync(new DateOnly(2026, 10, 20)));
		_ranges.Should().BeEmpty();
	}

	/// <summary>
	/// Verifies that the item template replaces the default content, and that item classes, tooltips and labels come from the selectors.
	/// </summary>
	[Fact]
	public void Items_UseTheTemplateAndSelectors()
	{
		var calendar = RenderCalendar(p => p
			.Add(x => x.ItemTemplate, show => $"<b class=\"custom\">{show.Title}!</b>")
			.Add(x => x.CssClassSelector, show => show.Id == 1 ? "show-music" : null)
			.Add(x => x.TooltipSelector, show => show.Id == 1 ? "Custom tip" : null));

		var items = Day(calendar, _oct15).QuerySelectorAll(".pdcal-item");
		items[1].QuerySelector("b.custom")!.TextContent.Should().Be("Morning show!");
		items[1].QuerySelector(".pdcal-item-title").Should().BeNull();
		items[1].ClassList.Should().Contain("show-music");
		items[1].GetAttribute("title").Should().Be("Custom tip");
		items[1].GetAttribute("aria-label").Should().Be($"Morning show, {Long(_oct15)}, 09:00 to 10:00");
		items[2].GetAttribute("title").Should().Be("News, 09:30 to 10:30");
		items[0].GetAttribute("title").Should().Be("Late show, 14 Oct 22:00 to 15 Oct 02:00");
	}

	/// <summary>
	/// Verifies that without a title selector the item's own text is used, and that a zero length item shows a single time.
	/// </summary>
	[Fact]
	public void Items_WithoutATitleSelector_UseToString()
	{
		var instant = new Show(9, "Pip", At(15, 12), At(15, 12));
		var calendar = Render<PDCalendar<Show>>(p => p
			.Add(x => x.Items, [instant])
			.Add(x => x.StartSelector, s => s.Start)
			.Add(x => x.EndSelector, s => s.End)
			.Add(x => x.TimeZone, CalendarTestZones.London)
			.Add(x => x.Clock, _clock)
			.Add(x => x.Date, _oct15));

		calendar.Find(".pdcal-item-title").TextContent.Should().Be("Show 9");
		calendar.Find(".pdcal-item-time").TextContent.Should().Be("12:00");
	}

	/// <summary>
	/// Verifies that an item with no text of its own gets an empty title.
	/// </summary>
	[Fact]
	public void Items_WithNoText_HaveAnEmptyTitle()
	{
		var calendar = Render<PDCalendar<Untitled>>(p => p
			.Add(x => x.Items, [new Untitled(At(15, 12))])
			.Add(x => x.StartSelector, s => s.At)
			.Add(x => x.EndSelector, s => s.At)
			.Add(x => x.TimeZone, CalendarTestZones.London)
			.Add(x => x.Clock, _clock)
			.Add(x => x.Date, _oct15));

		calendar.Find(".pdcal-item-title").TextContent.Should().BeEmpty();
	}

	/// <summary>An item whose text is null.</summary>
	/// <param name="at">When the item occurs.</param>
	public sealed class Untitled(DateTimeOffset at)
	{
		/// <summary>Gets when the item occurs.</summary>
		public DateTimeOffset At { get; } = at;

		/// <summary>Gets no text at all.</summary>
		/// <returns>Null.</returns>
		public override string? ToString() => null;
	}

	/// <summary>
	/// Verifies that without start and end selectors nothing is laid out.
	/// </summary>
	[Fact]
	public void Items_WithoutSelectors_AreNotShown()
	{
		var calendar = Render<PDCalendar<Show>>(p => p
			.Add(x => x.Items, [_morning])
			.Add(x => x.Clock, _clock)
			.Add(x => x.View, CalendarView.Agenda));

		calendar.FindAll(".pdcal-item").Should().BeEmpty();
		calendar.Find(".pdcal-empty").TextContent.Should().Be("No items");
	}

	/// <summary>
	/// Verifies that with no date the calendar opens on today, by the supplied clock and time zone.
	/// </summary>
	[Fact]
	public void Date_WhenNotSet_IsToday()
	{
		_clock.UtcNow = new DateTimeOffset(2026, 10, 31, 23, 30, 0, TimeSpan.Zero);

		var calendar = Render<PDCalendar<Show>>(p => p
			.Add(x => x.TimeZone, CalendarTestZones.London)
			.Add(x => x.Clock, _clock));

		// 23:30 UTC on 31 October is still the 31st in London, which is on GMT by then
		calendar.Instance.CurrentDate.Should().Be(new DateOnly(2026, 10, 31));

		var tokyo = Render<PDCalendar<Show>>(p => p
			.Add(x => x.TimeZone, TimeZoneInfo.FindSystemTimeZoneById("Asia/Tokyo"))
			.Add(x => x.Clock, _clock));
		tokyo.Instance.CurrentDate.Should().Be(new DateOnly(2026, 11, 1));
	}

	/// <summary>
	/// Verifies that the busy overlay is shown while loading.
	/// </summary>
	[Fact]
	public void IsLoading_ShowsTheBusyOverlay()
	{
		var calendar = RenderCalendar();
		calendar.Find(".pd-busy-overlay").ClassList.Should().Contain("hide");
		calendar.Find(".pdcal").GetAttribute("aria-busy").Should().Be("false");

		calendar.Render(p => p.Add(x => x.IsLoading, true));

		calendar.Find(".pd-busy-overlay").ClassList.Should().Contain("show");
		calendar.Find(".pdcal").GetAttribute("aria-busy").Should().Be("true");
	}

	/// <summary>
	/// Verifies that the root carries the view and any extra CSS class.
	/// </summary>
	[Fact]
	public void Root_CarriesTheViewAndCssClass()
	{
		var calendar = RenderCalendar(p => p.Add(x => x.CssClass, "my-calendar"));

		calendar.Find(".pdcal").ClassList.Should().Contain("pdcal-view-month").And.Contain("my-calendar");
		calendar.Find(".pdcal").Id.Should().Be(calendar.Instance.Id);
	}

	/// <summary>A clock whose current time is set by the test, and whose timers fire only when the test fires them.</summary>
	private sealed class TestClock(DateTimeOffset utcNow) : TimeProvider
	{
		public DateTimeOffset UtcNow { get; set; } = utcNow;

		public List<TestTimer> Timers { get; } = [];

		public override DateTimeOffset GetUtcNow() => UtcNow;

		public override ITimer CreateTimer(TimerCallback callback, object? state, TimeSpan dueTime, TimeSpan period)
		{
			var timer = new TestTimer(callback, state, dueTime, period);
			Timers.Add(timer);
			return timer;
		}
	}

	/// <summary>A timer that records how it was set up and fires on demand.</summary>
	private sealed class TestTimer(TimerCallback callback, object? state, TimeSpan dueTime, TimeSpan period) : ITimer
	{
		public TimeSpan DueTime { get; } = dueTime;

		public TimeSpan Period { get; } = period;

		public bool IsDisposed { get; private set; }

		public void Fire() => callback(state);

		public bool Change(TimeSpan dueTime, TimeSpan period) => true;

		public void Dispose() => IsDisposed = true;

		public ValueTask DisposeAsync()
		{
			Dispose();
			return ValueTask.CompletedTask;
		}
	}
}
