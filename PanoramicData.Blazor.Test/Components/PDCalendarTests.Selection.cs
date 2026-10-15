using AwesomeAssertions;
using Bunit;
using Microsoft.AspNetCore.Components.Web;
using PanoramicData.Blazor.Enums;

namespace PanoramicData.Blazor.Test.Components;

/// <summary>
/// Item and slot selection, keyboard and agenda tests for <see cref="PDCalendar{TItem}"/>.
/// </summary>
public partial class PDCalendarTests
{
	private static Task PressAsync(IRenderedComponent<PDCalendar<Show>> calendar, string selector, string key)
		=> calendar.Find(selector).KeyDownAsync(new KeyboardEventArgs { Key = key });

	/// <summary>
	/// Verifies that clicking an item selects it, raising the selection callbacks, and marks it selected.
	/// </summary>
	[Fact]
	public async Task ItemClick_SelectsTheItem()
	{
		var changes = new List<Show?>();
		var selections = new List<Show>();
		var calendar = RenderCalendar(p => p
			.Add(x => x.SelectedItemChanged, s => changes.Add(s))
			.Add(x => x.OnItemSelected, s => selections.Add(s)));

		await Day(calendar, _oct15).QuerySelectorAll(".pdcal-item")[1].ClickAsync(new());

		changes.Should().Equal(_morning);
		selections.Should().Equal(_morning);
		calendar.Instance.CurrentSelection.Should().Be(_morning);
		var item = Day(calendar, _oct15).QuerySelectorAll(".pdcal-item")[1];
		item.ClassList.Should().Contain("pdcal-item-selected");
		item.GetAttribute("aria-pressed").Should().Be("true");

		// clicking the selected item again reports the selection but not a change
		await item.ClickAsync(new());
		changes.Should().ContainSingle();
		selections.Should().HaveCount(2);

		// selecting does not move the focused date or select the day
		_ranges.Should().ContainSingle();
	}

	/// <summary>
	/// Verifies that double clicking an item raises the double click callback.
	/// </summary>
	[Fact]
	public async Task ItemDoubleClick_RaisesTheCallback()
	{
		Show? doubleClicked = null;
		var calendar = RenderCalendar(p => p.Add(x => x.OnItemDoubleClick, s => doubleClicked = s));

		await Day(calendar, _oct15).QuerySelectorAll(".pdcal-item")[2].DoubleClickAsync(new());

		doubleClicked.Should().Be(_news);
	}

	/// <summary>
	/// Verifies that a disabled item is shown but cannot be selected or double clicked.
	/// </summary>
	[Fact]
	public async Task DisabledItems_CannotBeSelected()
	{
		Show? selected = null;
		Show? doubleClicked = null;
		var calendar = RenderCalendar(p => p
			.Add(x => x.IsDisabledSelector, s => s.Id == 1)
			.Add(x => x.OnItemSelected, s => selected = s)
			.Add(x => x.OnItemDoubleClick, s => doubleClicked = s));

		var item = Day(calendar, _oct15).QuerySelectorAll(".pdcal-item")[1];
		item.HasAttribute("disabled").Should().BeTrue();
		item.ClassList.Should().Contain("pdcal-item-disabled");
		item.GetAttribute("aria-label").Should().EndWith(", unavailable");

		// bUnit dispatches the events even to a disabled button, so this checks the component's own guard
		await item.ClickAsync(new());
		await Day(calendar, _oct15).QuerySelectorAll(".pdcal-item")[1].DoubleClickAsync(new());

		selected.Should().BeNull();
		doubleClicked.Should().BeNull();
	}

	/// <summary>
	/// Verifies that the selected item can be supplied by the host and is recognised by key when items are reloaded as new instances.
	/// </summary>
	[Fact]
	public void SelectedItem_IsMatchedByKey()
	{
		var reloaded = _morning with { Title = "Morning show (reloaded)" };
		var calendar = RenderCalendar(p => p.Add(x => x.SelectedItem, _morning), items: [reloaded]);
		calendar.FindAll(".pdcal-item-selected").Should().BeEmpty();

		calendar.Render(p => p.Add(x => x.KeySelector, s => s.Id));

		calendar.Find(".pdcal-item-selected .pdcal-item-title").TextContent.Should().Be("Morning show (reloaded)");

		calendar.Render(p => p.Add(x => x.SelectedItem, (Show?)null));
		calendar.FindAll(".pdcal-item-selected").Should().BeEmpty();
	}

	/// <summary>
	/// Verifies that clicking a day of the month view focuses it and reports the start of the day, unless it is outside the bounds.
	/// </summary>
	[Fact]
	public async Task DayClick_SelectsTheStartOfTheDay()
	{
		var slots = new List<DateTimeOffset>();
		var calendar = RenderCalendar(p => p
			.Add(x => x.OnSlotSelected, s => slots.Add(s))
			.Add(x => x.MaxDate, new DateOnly(2026, 10, 31)));

		await Day(calendar, new DateOnly(2026, 10, 25)).ClickAsync(new());
		await Day(calendar, new DateOnly(2026, 11, 1)).ClickAsync(new());

		slots.Should().Equal(new DateTimeOffset(2026, 10, 25, 0, 0, 0, _bst));
		calendar.Instance.CurrentDate.Should().Be(new DateOnly(2026, 10, 25));
	}

	/// <summary>
	/// Verifies that clicking an hour slot of the week view reports the instant it starts, and that an hour that does not occur cannot be chosen.
	/// </summary>
	[Fact]
	public async Task SlotClick_SelectsTheHour()
	{
		var slots = new List<DateTimeOffset>();
		var calendar = RenderWeek(p => p.Add(x => x.OnSlotSelected, s => slots.Add(s)), date: new DateOnly(2026, 10, 25));
		var id = calendar.Instance.Id;

		await calendar.Find($"#{id}-20261025-2").ClickAsync(new());
		await calendar.Find($"#{id}-20261019-2").ClickAsync(new());
		await calendar.Find($"#{id}-20261020-10").ClickAsync(new());

		slots.Should().Equal(
			new DateTimeOffset(2026, 10, 25, 1, 0, 0, TimeSpan.Zero),
			new DateTimeOffset(2026, 10, 20, 9, 0, 0, _bst));
		calendar.Instance.CurrentDate.Should().Be(new DateOnly(2026, 10, 20));
		calendar.Find(".pdcal-week").GetAttribute("aria-activedescendant").Should().Be($"{id}-20261020-10");
		calendar.Find($"#{id}-20261020-10").GetAttribute("aria-selected").Should().Be("true");
	}

	/// <summary>
	/// Verifies that a slot of a day outside the bounds cannot be chosen.
	/// </summary>
	[Fact]
	public async Task SlotClick_OutsideTheBounds_IsIgnored()
	{
		var slots = new List<DateTimeOffset>();
		var calendar = RenderWeek(p => p.Add(x => x.OnSlotSelected, s => slots.Add(s)).Add(x => x.MinDate, _oct15));

		var slot = calendar.Find($"#{calendar.Instance.Id}-20261014-9");
		slot.ClassList.Should().Contain("pdcal-day-disabled");
		await slot.ClickAsync(new());

		slots.Should().BeEmpty();
	}

	/// <summary>
	/// Verifies the month view keys: arrows move by a day or a week, page keys change month, Home goes to today, and Enter or Space selects the day.
	/// </summary>
	[Fact]
	public async Task Keyboard_InTheMonthView_MovesAndSelects()
	{
		var slots = new List<DateTimeOffset>();
		var calendar = RenderCalendar(p => p.Add(x => x.OnSlotSelected, s => slots.Add(s)));

		await PressAsync(calendar, ".pdcal-month", "ArrowRight");
		calendar.Instance.CurrentDate.Should().Be(new DateOnly(2026, 10, 16));
		await PressAsync(calendar, ".pdcal-month", "ArrowDown");
		calendar.Instance.CurrentDate.Should().Be(new DateOnly(2026, 10, 23));
		await PressAsync(calendar, ".pdcal-month", "ArrowLeft");
		await PressAsync(calendar, ".pdcal-month", "ArrowUp");
		calendar.Instance.CurrentDate.Should().Be(new DateOnly(2026, 10, 15));
		calendar.Find(".pdcal-month").GetAttribute("aria-activedescendant").Should().Be($"{calendar.Instance.Id}-20261015");

		await PressAsync(calendar, ".pdcal-month", "PageDown");
		calendar.Instance.CurrentDate.Should().Be(new DateOnly(2026, 11, 15));
		await PressAsync(calendar, ".pdcal-month", "PageUp");
		await PressAsync(calendar, ".pdcal-month", "PageUp");
		calendar.Instance.CurrentDate.Should().Be(new DateOnly(2026, 9, 15));
		await PressAsync(calendar, ".pdcal-month", "Home");
		calendar.Instance.CurrentDate.Should().Be(_oct15);

		await PressAsync(calendar, ".pdcal-month", "Enter");
		await PressAsync(calendar, ".pdcal-month", " ");
		await PressAsync(calendar, ".pdcal-month", "x");
		slots.Should().Equal(At(15, 0), At(15, 0));
		calendar.Instance.CurrentDate.Should().Be(_oct15);
	}

	/// <summary>
	/// Verifies that the arrow keys do not move the focus onto a day outside the bounds.
	/// </summary>
	[Fact]
	public async Task Keyboard_StopsAtTheBounds()
	{
		var calendar = RenderCalendar(p => p.Add(x => x.MaxDate, new DateOnly(2026, 10, 20)));

		await PressAsync(calendar, ".pdcal-month", "ArrowDown");
		calendar.Instance.CurrentDate.Should().Be(_oct15);
		await PressAsync(calendar, ".pdcal-month", "ArrowRight");
		calendar.Instance.CurrentDate.Should().Be(new DateOnly(2026, 10, 16));
	}

	/// <summary>
	/// Verifies the week view keys: up and down move by an hour within the day, left and right by a day, and Enter selects the hour.
	/// </summary>
	[Fact]
	public async Task Keyboard_InTheWeekView_MovesByHour()
	{
		var slots = new List<DateTimeOffset>();
		var calendar = RenderWeek(p => p.Add(x => x.OnSlotSelected, s => slots.Add(s)).Add(x => x.FirstVisibleHour, 8));
		var id = calendar.Instance.Id;

		await PressAsync(calendar, ".pdcal-week", "ArrowUp");
		calendar.Find(".pdcal-week").GetAttribute("aria-activedescendant").Should().Be($"{id}-20261015-0");

		await PressAsync(calendar, ".pdcal-week", "ArrowDown");
		await PressAsync(calendar, ".pdcal-week", "ArrowDown");
		await PressAsync(calendar, ".pdcal-week", "ArrowRight");
		calendar.Find(".pdcal-week").GetAttribute("aria-activedescendant").Should().Be($"{id}-20261016-2");
		calendar.Find($"#{id}-20261016-2").ClassList.Should().Contain("pdcal-focused");

		await PressAsync(calendar, ".pdcal-week", "Enter");
		slots.Should().Equal(At(16, 10));

		for (var i = 0; i < 20; i++)
		{
			await PressAsync(calendar, ".pdcal-week", "ArrowDown");
		}

		calendar.Find(".pdcal-week").GetAttribute("aria-activedescendant").Should().Be($"{id}-20261016-15");
	}

	/// <summary>
	/// Verifies that in the agenda the up and down keys move a day at a time.
	/// </summary>
	[Fact]
	public async Task Keyboard_InTheAgenda_MovesByDay()
	{
		var calendar = RenderCalendar(view: CalendarView.Agenda);

		await PressAsync(calendar, ".pdcal-agenda", "ArrowDown");
		calendar.Instance.CurrentDate.Should().Be(new DateOnly(2026, 10, 16));
		await PressAsync(calendar, ".pdcal-agenda", "ArrowUp");
		calendar.Instance.CurrentDate.Should().Be(_oct15);
	}

	/// <summary>
	/// Verifies that the agenda lists only the days with items, marks today and describes items that carry over between days.
	/// </summary>
	[Fact]
	public void Agenda_ListsItemsByDay()
	{
		var calendar = RenderCalendar(date: new DateOnly(2026, 10, 14), view: CalendarView.Agenda);

		var days = calendar.FindAll(".pdcal-agenda-day");
		days.Select(d => d.GetAttribute("aria-label")).Should().Equal(Long(new DateOnly(2026, 10, 14)), Long(_oct15), Long(new DateOnly(2026, 10, 20)));
		days[1].QuerySelector(".badge")!.TextContent.Should().Be("Today");
		days[0].QuerySelector(".badge").Should().BeNull();
		days[0].QuerySelector(".pdcal-item-time")!.TextContent.Should().Be("From 22:00");
		days[1].QuerySelectorAll(".pdcal-item-title").Select(e => e.TextContent).Should().Equal("Late show", "Morning show", "News");
		calendar.Instance.Title.Should().Be("14 to 20 Oct 2026");
	}

	/// <summary>
	/// Verifies that an agenda with no items shows the empty text.
	/// </summary>
	[Fact]
	public void Agenda_WithNoItems_ShowsTheEmptyText()
	{
		var calendar = RenderCalendar(p => p.Add(x => x.EmptyText, "Nothing scheduled"), items: [], view: CalendarView.Agenda);

		calendar.Find(".pdcal-empty").TextContent.Should().Be("Nothing scheduled");
	}
}
