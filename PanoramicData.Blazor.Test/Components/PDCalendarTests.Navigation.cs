using AwesomeAssertions;
using Bunit;
using PanoramicData.Blazor.Enums;

namespace PanoramicData.Blazor.Test.Components;

/// <summary>
/// Toolbar, view switching, date bounds and visible range tests for <see cref="PDCalendar{TItem}"/>.
/// </summary>
public partial class PDCalendarTests
{
	/// <summary>
	/// Verifies that the visible range is reported once when first shown, with the start of the first and the day after the last grid day.
	/// </summary>
	[Fact]
	public void VisibleRange_IsReportedWhenFirstShown()
	{
		var calendar = RenderCalendar();

		_ranges.Should().Equal((new DateTimeOffset(2026, 9, 28, 0, 0, 0, _bst), new DateTimeOffset(2026, 11, 2, 0, 0, 0, TimeSpan.Zero)));
		calendar.Instance.VisibleRange.Should().Be(_ranges[0]);

		// re-rendering with the same parameters does not report it again
		calendar.Render(p => p.Add(x => x.IsLoading, true));
		_ranges.Should().ContainSingle();
	}

	/// <summary>
	/// Verifies that previous, next and today move the period, report the new date and range, and update the title.
	/// </summary>
	[Fact]
	public async Task Toolbar_PreviousNextAndToday_MoveThePeriod()
	{
		var dates = new List<DateOnly?>();
		var calendar = RenderCalendar(p => p.Add(x => x.DateChanged, d => dates.Add(d)));
		calendar.Find(".pdcal-title").TextContent.Should().Be(calendar.Instance.Title);

		await calendar.Find(".pdcal-next").ClickAsync(new());
		calendar.Instance.CurrentDate.Should().Be(new DateOnly(2026, 11, 15));
		calendar.Find(".pdcal-title").TextContent.Should().Be(calendar.Instance.Title);

		await calendar.Find(".pdcal-previous").ClickAsync(new());
		await calendar.Find(".pdcal-previous").ClickAsync(new());
		calendar.Instance.CurrentDate.Should().Be(new DateOnly(2026, 9, 15));

		await calendar.Find(".pdcal-today-button").ClickAsync(new());
		calendar.Instance.CurrentDate.Should().Be(_oct15);

		dates.Should().Equal(new DateOnly(2026, 11, 15), _oct15, new DateOnly(2026, 9, 15), _oct15);
		_ranges.Should().HaveCount(5);
		calendar.Find(".pdcal-previous").GetAttribute("aria-label").Should().Be("Previous month");
	}

	/// <summary>
	/// Verifies that today does nothing when today is already shown.
	/// </summary>
	[Fact]
	public async Task Toolbar_Today_WhenAlreadyThere_DoesNothing()
	{
		var calendar = RenderCalendar();

		await calendar.Find(".pdcal-today-button").ClickAsync(new());

		_ranges.Should().ContainSingle();
	}

	/// <summary>
	/// Verifies that the view switcher offers the available views, marks the current one and switches.
	/// </summary>
	[Fact]
	public async Task Toolbar_ViewSwitcher_ChangesTheView()
	{
		CalendarView? changed = null;
		var calendar = RenderCalendar(p => p.Add(x => x.ViewChanged, v => changed = v));

		var buttons = calendar.FindAll(".pdcal-view-button");
		buttons.Select(b => b.TextContent.Trim()).Should().Equal("Month", "Week", "Agenda");
		buttons[0].GetAttribute("aria-pressed").Should().Be("true");

		await buttons[1].ClickAsync(new());

		changed.Should().Be(CalendarView.Week);
		calendar.FindAll(".pdcal-week").Should().ContainSingle();
		calendar.Find(".pdcal").ClassList.Should().Contain("pdcal-view-week");
		calendar.FindAll(".pdcal-view-button")[1].GetAttribute("aria-pressed").Should().Be("true");
		calendar.Find(".pdcal-next").GetAttribute("aria-label").Should().Be("Next week");
		_ranges.Should().HaveCount(2);

		// choosing the current view again does nothing
		await calendar.FindAll(".pdcal-view-button")[1].ClickAsync(new());
		_ranges.Should().HaveCount(2);
	}

	/// <summary>
	/// Verifies that each part of the toolbar can be hidden, and that a single available view hides the switcher.
	/// </summary>
	[Fact]
	public void Toolbar_PartsCanBeHidden()
	{
		var calendar = RenderCalendar(p => p
			.Add(x => x.ShowNavigation, false)
			.Add(x => x.ShowToday, false)
			.Add(x => x.ShowTitle, false)
			.Add(x => x.AvailableViews, [CalendarView.Month, CalendarView.Agenda]));

		calendar.FindAll(".pdcal-previous, .pdcal-next, .pdcal-today-button, .pdcal-title").Should().BeEmpty();
		calendar.FindAll(".pdcal-view-button").Should().HaveCount(2);

		calendar.Render(p => p.Add(x => x.AvailableViews, [CalendarView.Month]));
		calendar.FindAll(".pdcal-views").Should().BeEmpty();

		calendar.Render(p => p.Add(x => x.AvailableViews, [CalendarView.Month, CalendarView.Week]).Add(x => x.ShowViewSwitcher, false));
		calendar.FindAll(".pdcal-views").Should().BeEmpty();

		calendar.Render(p => p.Add(x => x.ShowToolbar, false));
		calendar.FindAll(".pdcal-toolbar").Should().BeEmpty();
	}

	/// <summary>
	/// Verifies that the date bounds disable days and navigation outside them, and clamp navigation to the bounds.
	/// </summary>
	[Fact]
	public async Task Bounds_DisableNavigationOutsideThem()
	{
		var calendar = RenderCalendar(p => p
			.Add(x => x.MinDate, new DateOnly(2026, 10, 5))
			.Add(x => x.MaxDate, new DateOnly(2026, 11, 10)));

		calendar.Find(".pdcal-previous").HasAttribute("disabled").Should().BeTrue();
		calendar.Find(".pdcal-next").HasAttribute("disabled").Should().BeFalse();
		var before = Day(calendar, new DateOnly(2026, 10, 4));
		before.ClassList.Should().Contain("pdcal-day-disabled");
		before.GetAttribute("aria-disabled").Should().Be("true");
		Day(calendar, new DateOnly(2026, 10, 5)).HasAttribute("aria-disabled").Should().BeFalse();

		await calendar.Find(".pdcal-next").ClickAsync(new());
		calendar.Instance.CurrentDate.Should().Be(new DateOnly(2026, 11, 10));
		calendar.Find(".pdcal-next").HasAttribute("disabled").Should().BeTrue();

		// navigating past the bound by code does nothing either
		await calendar.InvokeAsync(() => calendar.Instance.NavigateAsync(1));
		calendar.Instance.CurrentDate.Should().Be(new DateOnly(2026, 11, 10));
		calendar.Instance.CanNavigate(-1).Should().BeTrue();
	}

	/// <summary>
	/// Verifies that a date outside the bounds is clamped, that today cannot be chosen when it is outside them, and that going to a disabled date is ignored.
	/// </summary>
	[Fact]
	public async Task Bounds_ClampTheDateAndDisableToday()
	{
		var calendar = RenderCalendar(p => p.Add(x => x.MinDate, new DateOnly(2026, 12, 1)));

		calendar.Instance.CurrentDate.Should().Be(new DateOnly(2026, 12, 1));
		calendar.Find(".pdcal-today-button").HasAttribute("disabled").Should().BeTrue();

		await calendar.InvokeAsync(() => calendar.Instance.GoToTodayAsync());
		await calendar.InvokeAsync(() => calendar.Instance.GoToDateAsync(new DateOnly(2026, 11, 30)));
		calendar.Instance.CurrentDate.Should().Be(new DateOnly(2026, 12, 1));

		await calendar.InvokeAsync(() => calendar.Instance.GoToDateAsync(new DateOnly(2027, 2, 3)));
		calendar.Instance.CurrentDate.Should().Be(new DateOnly(2027, 2, 3));
		calendar.Instance.Title.Should().Be(new DateOnly(2027, 2, 3).ToString("MMMM yyyy", System.Globalization.CultureInfo.CurrentCulture));
	}

	/// <summary>
	/// Verifies that a new Date or View from the host is adopted, but a re-render with unchanged values keeps where the user navigated to.
	/// </summary>
	[Fact]
	public async Task Parameters_AreAdoptedOnlyWhenTheyChange()
	{
		var calendar = RenderCalendar();

		await calendar.Find(".pdcal-next").ClickAsync(new());
		calendar.Render(p => p.Add(x => x.IsLoading, false));
		calendar.Instance.CurrentDate.Should().Be(new DateOnly(2026, 11, 15));

		calendar.Render(p => p.Add(x => x.Date, new DateOnly(2027, 1, 1)));
		calendar.Instance.CurrentDate.Should().Be(new DateOnly(2027, 1, 1));

		calendar.Render(p => p.Add(x => x.View, CalendarView.Agenda));
		calendar.Instance.CurrentView.Should().Be(CalendarView.Agenda);
		calendar.FindAll(".pdcal-agenda").Should().ContainSingle();
		calendar.Find(".pdcal-next").GetAttribute("aria-label").Should().Be("Next 7 days");
	}

	/// <summary>
	/// Verifies that the agenda pages by its number of days.
	/// </summary>
	[Fact]
	public async Task Agenda_PagesByItsLength()
	{
		var calendar = RenderCalendar(p => p.Add(x => x.AgendaDays, 3), view: CalendarView.Agenda);

		await calendar.Find(".pdcal-next").ClickAsync(new());

		calendar.Instance.CurrentDate.Should().Be(new DateOnly(2026, 10, 18));
		_ranges[^1].Should().Be((new DateTimeOffset(2026, 10, 18, 0, 0, 0, _bst), new DateTimeOffset(2026, 10, 21, 0, 0, 0, _bst)));
	}
}
