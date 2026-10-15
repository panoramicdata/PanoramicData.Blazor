using AwesomeAssertions;
using Bunit;
using PanoramicData.Blazor.Enums;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.JSInterop;
using PanoramicData.Blazor.Test.Helpers;

namespace PanoramicData.Blazor.Test.Components;

/// <summary>
/// Week view tests for <see cref="PDCalendar{TItem}"/>.
/// </summary>
public partial class PDCalendarTests
{
	private IRenderedComponent<PDCalendar<Show>> RenderWeek(
		Action<ComponentParameterCollectionBuilder<PDCalendar<Show>>>? configure = null,
		IEnumerable<Show>? items = null,
		DateOnly? date = null)
		=> RenderCalendar(configure, items, date, CalendarView.Week);

	private static AngleSharp.Dom.IElement Column(IRenderedComponent<PDCalendar<Show>> calendar, string isoDate)
		=> calendar.Find($".pdcal-week-column[data-date='{isoDate}']");

	/// <summary>
	/// Verifies that the week view has a header for each day and a row for each hour, labelled by local time.
	/// </summary>
	[Fact]
	public void Week_HasADayHeaderAndAnHourRowGrid()
	{
		var calendar = RenderWeek();

		calendar.FindAll(".pdcal-week-header [role=columnheader]").Should().HaveCount(8);
		var rows = calendar.FindAll(".pdcal-week-body [role=row]");
		rows.Should().HaveCount(24);
		calendar.FindAll(".pdcal-hour-label").Select(e => e.TextContent).Take(3).Should().Equal("00:00", "01:00", "02:00");
		rows[0].QuerySelectorAll("[role=gridcell]").Should().HaveCount(7);
		calendar.Find(".pdcal-week-body").GetAttribute("style").Should().Be("--pdcal-slot-height:48px;height:1152px");
		calendar.Find(".pdcal-week-scroll").GetAttribute("style").Should().Be("max-height:36rem");
		calendar.Instance.Title.Should().Be("12 to 18 Oct 2026");

		var header = calendar.FindAll(".pdcal-week-day-header")[3];
		header.GetAttribute("aria-label").Should().Be(Long(_oct15));
		header.GetAttribute("aria-current").Should().Be("date");
		calendar.FindAll(".pdcal-day-length").Should().BeEmpty();
	}

	/// <summary>
	/// Verifies that items are positioned by elapsed time, overlapping items are placed side by side, and an item
	/// spanning midnight is drawn in both day columns.
	/// </summary>
	[Fact]
	public void Week_PositionsItemsByTimeAndPacksOverlaps()
	{
		var calendar = RenderWeek();

		var items = Column(calendar, "2026-10-15").QuerySelectorAll(".pdcal-item");
		items.Select(e => e.QuerySelector(".pdcal-item-title")!.TextContent).Should().Equal("Late show", "Morning show", "News");
		items[0].GetAttribute("style").Should().Be("top:0px;height:96px;left:0%;width:100%");
		items[1].GetAttribute("style").Should().Be("top:432px;height:48px;left:0%;width:50%");
		items[2].GetAttribute("style").Should().Be("top:456px;height:48px;left:50%;width:50%");

		var lateStart = Column(calendar, "2026-10-14").QuerySelector(".pdcal-item")!;
		lateStart.GetAttribute("style").Should().Be("top:1056px;height:96px;left:0%;width:100%");
		lateStart.ClassList.Should().Contain("pdcal-item-continues-after");
	}

	/// <summary>
	/// Verifies that a very short item is drawn at the minimum height.
	/// </summary>
	[Fact]
	public void Week_ShortItems_UseTheMinimumHeight()
	{
		var pip = new Show(5, "Pip", At(15, 12), At(15, 12, 5));

		var calendar = RenderWeek(p => p.Add(x => x.MinItemHeight, 30).Add(x => x.SlotHeight, 60), [pip]);

		Column(calendar, "2026-10-15").QuerySelector(".pdcal-item")!.GetAttribute("style")
			.Should().Be("top:720px;height:30px;left:0%;width:100%");
	}

	/// <summary>
	/// Verifies that limiting the visible hours removes rows, hides items outside them and clips items crossing them.
	/// </summary>
	[Fact]
	public void Week_VisibleHours_LimitRowsAndClipItems()
	{
		var early = new Show(6, "Early", At(15, 5), At(15, 6));
		var crossing = new Show(7, "Crossing", At(15, 7), At(15, 9));
		var calendar = RenderWeek(p => p.Add(x => x.FirstVisibleHour, 8).Add(x => x.LastVisibleHour, 18), [early, crossing, _morning]);

		calendar.FindAll(".pdcal-week-body [role=row]").Should().HaveCount(10);
		calendar.FindAll(".pdcal-hour-label")[0].TextContent.Should().Be("08:00");

		var items = Column(calendar, "2026-10-15").QuerySelectorAll(".pdcal-item");
		items.Select(e => e.QuerySelector(".pdcal-item-title")!.TextContent).Should().Equal("Crossing", "Morning show");
		items[0].GetAttribute("style").Should().StartWith("top:0px;height:48px;");
		items[1].GetAttribute("style").Should().StartWith("top:48px;height:48px;");
	}

	/// <summary>
	/// Verifies that in the week the clocks go back the repeated hour gets its own row, labelled with its offset,
	/// that other days show it as not occurring, and that the day is noted as 25 hours long.
	/// </summary>
	[Fact]
	public void Week_AutumnClockChange_ShowsTwentyFiveHours()
	{
		var repeated = new Show(8, "Second one o'clock", new DateTimeOffset(2026, 10, 25, 1, 0, 0, TimeSpan.Zero), new DateTimeOffset(2026, 10, 25, 2, 0, 0, TimeSpan.Zero));
		var calendar = RenderWeek(items: [repeated], date: new DateOnly(2026, 10, 25));

		calendar.FindAll(".pdcal-week-body [role=row]").Should().HaveCount(25);
		calendar.FindAll(".pdcal-hour-label").Select(e => e.TextContent).Take(4)
			.Should().Equal("00:00", "01:00 UTC+01:00", "01:00 UTC+00:00", "02:00");

		var monday = calendar.Find($"#{calendar.Instance.Id}-20261019-2");
		monday.ClassList.Should().Contain("pdcal-slot-none");
		monday.GetAttribute("aria-disabled").Should().Be("true");
		monday.GetAttribute("aria-label").Should().Be($"{Long(new DateOnly(2026, 10, 19))}, this hour does not occur on this day");
		calendar.Find($"#{calendar.Instance.Id}-20261025-2").ClassList.Should().NotContain("pdcal-slot-none");

		calendar.FindAll(".pdcal-day-length").Select(e => e.TextContent).Should().Equal("25 hours");
		calendar.FindAll(".pdcal-week-day-header")[6].GetAttribute("aria-label").Should().Be($"{Long(new DateOnly(2026, 10, 25))}, 25 hours");

		Column(calendar, "2026-10-25").QuerySelector(".pdcal-item")!.GetAttribute("style").Should().StartWith("top:96px;height:48px;");
	}

	/// <summary>
	/// Verifies that in the week the clocks go forward the skipped hour does not occur on that day, which is noted as 23 hours long.
	/// </summary>
	[Fact]
	public void Week_SpringClockChange_ShowsTwentyThreeHours()
	{
		var calendar = RenderWeek(items: [], date: new DateOnly(2026, 3, 29));

		calendar.FindAll(".pdcal-week-body [role=row]").Should().HaveCount(24);
		calendar.Find($"#{calendar.Instance.Id}-20260329-1").ClassList.Should().Contain("pdcal-slot-none");
		calendar.FindAll(".pdcal-day-length").Select(e => e.TextContent).Should().Equal("23 hours");
	}

	/// <summary>
	/// Verifies that west of Greenwich the repeated hour is labelled with negative offsets.
	/// </summary>
	[Fact]
	public void Week_RepeatedHourWestOfGreenwich_HasNegativeOffsets()
	{
		var calendar = RenderWeek(items: [], date: new DateOnly(2026, 11, 1));
		calendar.FindAll(".pdcal-week-body [role=row]").Should().HaveCount(24, "London's clocks went back the week before");

		// switching the time zone lays the week out again
		calendar.Render(p => p.Add(x => x.TimeZone, TimeZoneInfo.FindSystemTimeZoneById("America/New_York")));

		calendar.FindAll(".pdcal-week-body [role=row]").Should().HaveCount(25);
		calendar.FindAll(".pdcal-hour-label").Select(e => e.TextContent).Skip(1).Take(2)
			.Should().Equal("01:00 UTC-04:00", "01:00 UTC-05:00");
	}

	/// <summary>
	/// Verifies that weekend shading also applies to the week view's slots, and can be turned off.
	/// </summary>
	[Fact]
	public void Week_ShadesWeekendSlots()
	{
		var calendar = RenderWeek();
		calendar.Find($"#{calendar.Instance.Id}-20261017-9").ClassList.Should().Contain("pdcal-weekend");

		calendar.Render(p => p.Add(x => x.ShadeWeekends, false));
		calendar.FindAll(".pdcal-weekend").Should().BeEmpty();
	}

	/// <summary>
	/// Verifies that a slot height of zero gives no minimum duration, so items that only nearly touch are not packed together.
	/// </summary>
	[Fact]
	public void Week_ZeroSlotHeight_PacksWithoutAMinimumDuration()
	{
		var first = new Show(10, "First", At(15, 12), At(15, 12));
		var second = new Show(11, "Second", At(15, 12, 1), At(15, 12, 2));

		var calendar = RenderWeek(p => p.Add(x => x.SlotHeight, 0), [first, second]);

		Column(calendar, "2026-10-15").QuerySelectorAll(".pdcal-item").Select(e => e.GetAttribute("style"))
			.Should().OnlyContain(style => style!.EndsWith("left:0%;width:100%", StringComparison.Ordinal));
	}

	/// <summary>
	/// Verifies that a day column showing no time at all draws none of its items.
	/// </summary>
	[Fact]
	public void Week_DayWithNoVisibleHour_DrawsNoItems()
	{
		var item = new Show(9, "Spring", new DateTimeOffset(2026, 3, 29, 0, 0, 0, TimeSpan.Zero), new DateTimeOffset(2026, 3, 29, 5, 0, 0, TimeSpan.Zero));

		var calendar = RenderWeek(p => p.Add(x => x.FirstVisibleHour, 1).Add(x => x.LastVisibleHour, 2), [item], new DateOnly(2026, 3, 29));

		calendar.FindAll(".pdcal-item").Should().BeEmpty();
	}

	/// <summary>
	/// Verifies that the current time line is drawn in today's column only, and moves when the minute timer fires.
	/// </summary>
	[Fact]
	public void Week_NowIndicator_FollowsTheClock()
	{
		var calendar = RenderWeek();

		var now = calendar.Find(".pdcal-now");
		now.GetAttribute("style").Should().Be("top:504px");
		Column(calendar, "2026-10-15").QuerySelector(".pdcal-now").Should().NotBeNull();
		calendar.FindAll(".pdcal-now").Should().ContainSingle();

		var timer = _clock.Timers.Should().ContainSingle().Subject;
		timer.DueTime.Should().Be(TimeSpan.FromMinutes(1));
		timer.Period.Should().Be(TimeSpan.FromMinutes(1));

		_clock.UtcNow = _clock.UtcNow.AddMinutes(5);
		timer.Fire();

		calendar.WaitForAssertion(() => calendar.Find(".pdcal-now").GetAttribute("style").Should().Be("top:508px"));
	}

	/// <summary>
	/// Verifies that the timer moves the today highlight when the day changes.
	/// </summary>
	[Fact]
	public void Timer_AtMidnight_MovesToday()
	{
		var calendar = RenderCalendar();
		Day(calendar, _oct15).ClassList.Should().Contain("pdcal-today");

		_clock.UtcNow = _clock.UtcNow.AddDays(1);
		_clock.Timers.Single().Fire();

		calendar.WaitForAssertion(() => Day(calendar, new DateOnly(2026, 10, 16)).ClassList.Should().Contain("pdcal-today"));
		Day(calendar, _oct15).ClassList.Should().NotContain("pdcal-today");
	}

	/// <summary>
	/// Verifies that the time line can be turned off, which stops the timer, and that the timer waits for the next minute boundary.
	/// </summary>
	[Fact]
	public void NowIndicator_Off_DrawsNoLineAndStopsTheTimer()
	{
		_clock.UtcNow = new DateTimeOffset(2026, 10, 15, 10, 30, 15, _bst);
		var calendar = RenderWeek();
		var timer = _clock.Timers.Single();
		timer.DueTime.Should().Be(TimeSpan.FromSeconds(45));

		calendar.Render(p => p.Add(x => x.NowIndicator, false));

		calendar.FindAll(".pdcal-now").Should().BeEmpty();
		timer.IsDisposed.Should().BeTrue();
	}

	/// <summary>
	/// Verifies that the line is not drawn when the current time is outside the visible hours, or not in the visible week.
	/// </summary>
	[Fact]
	public void NowIndicator_OutsideTheVisibleTime_IsNotDrawn()
	{
		var calendar = RenderWeek(p => p.Add(x => x.FirstVisibleHour, 12));
		calendar.FindAll(".pdcal-now").Should().BeEmpty();

		var nextWeek = RenderWeek(date: new DateOnly(2026, 10, 22));
		nextWeek.FindAll(".pdcal-now").Should().BeEmpty();
	}

	/// <summary>
	/// Verifies that a new clock replaces the timer.
	/// </summary>
	[Fact]
	public void Clock_WhenReplaced_RestartsTheTimer()
	{
		var calendar = RenderWeek();
		var first = _clock.Timers.Single();
		var other = new TestClock(_clock.UtcNow);

		calendar.Render(p => p.Add(x => x.Clock, other));
		calendar.Render(p => p.Add(x => x.SlotHeight, 40));

		first.IsDisposed.Should().BeTrue();
		other.Timers.Should().ContainSingle();
	}

	/// <summary>
	/// Verifies that the timer is stopped when the component is disposed, and a late tick is ignored.
	/// </summary>
	[Fact]
	public async Task Dispose_StopsTheTimer()
	{
		var calendar = RenderWeek();
		var timer = _clock.Timers.Single();

		await calendar.Instance.DisposeAsync();

		timer.IsDisposed.Should().BeTrue();
		timer.Fire();
	}

	/// <summary>
	/// Verifies that the week view scrolls to the requested hour through the common module, and only once per showing.
	/// </summary>
	[Fact]
	public async Task ScrollToHour_ScrollsTheWeekView()
	{
		var module = JSInterop.SetupModule(JSInteropVersionHelper.CommonJsUrl);
		module.SetupVoid("setProperty", _ => true).SetVoidResult();

		var calendar = RenderWeek(p => p.Add(x => x.ScrollToHour, 8).Add(x => x.SlotHeight, 50));

		var call = module.VerifyInvoke("setProperty");
		call.Arguments[1].Should().Be("scrollTop");
		call.Arguments[2].Should().Be(400d);

		await calendar.InvokeAsync(() => calendar.Instance.NavigateAsync(1));
		module.Invocations.Count(i => i.Identifier == "setProperty").Should().Be(1);

		await calendar.InvokeAsync(() => calendar.Instance.SetViewAsync(CalendarView.Month));
		await calendar.InvokeAsync(() => calendar.Instance.SetViewAsync(CalendarView.Week));
		calendar.WaitForAssertion(() => module.Invocations.Count(i => i.Identifier == "setProperty").Should().Be(2));

		await calendar.Instance.DisposeAsync();
	}

	/// <summary>
	/// Verifies that an hour after the last visible row scrolls to the last row.
	/// </summary>
	[Fact]
	public void ScrollToHour_PastTheLastRow_ScrollsToTheEnd()
	{
		var module = JSInterop.SetupModule(JSInteropVersionHelper.CommonJsUrl);
		module.SetupVoid("setProperty", _ => true).SetVoidResult();

		RenderWeek(p => p.Add(x => x.ScrollToHour, 23).Add(x => x.FirstVisibleHour, 8).Add(x => x.LastVisibleHour, 18));

		module.VerifyInvoke("setProperty").Arguments[2].Should().Be(9 * 48d);
	}

	/// <summary>
	/// Verifies that a lost circuit while scrolling is ignored.
	/// </summary>
	[Fact]
	public void ScrollToHour_WhenTheCircuitHasGone_IsIgnored()
	{
		var module = JSInterop.SetupModule(JSInteropVersionHelper.CommonJsUrl);
		module.SetupVoid("setProperty", _ => true).SetException(new JSDisconnectedException("gone"));

		var calendar = RenderWeek(p => p.Add(x => x.ScrollToHour, 8));

		calendar.FindAll(".pdcal-week").Should().ContainSingle();
	}

	/// <summary>
	/// Verifies that a component disposed while the module loads is ignored.
	/// </summary>
	[Fact]
	public void ScrollToHour_WhenDisposedDuringImport_IsIgnored()
	{
		var runtime = new DisconnectingJSRuntime { ImportException = new ObjectDisposedException("module") };
		Services.AddSingleton<IJSRuntime>(runtime);

		var calendar = RenderWeek(p => p.Add(x => x.ScrollToHour, 8));

		calendar.FindAll(".pdcal-week").Should().ContainSingle();
		runtime.Module.Calls.Should().BeEmpty();
	}

	/// <summary>
	/// Verifies that disposing tolerates a circuit that has already gone.
	/// </summary>
	[Fact]
	public async Task Dispose_WhenTheCircuitHasGone_IsIgnored()
	{
		var runtime = new DisconnectingJSRuntime();
		Services.AddSingleton<IJSRuntime>(runtime);
		var calendar = RenderWeek(p => p.Add(x => x.ScrollToHour, 8));
		runtime.Module.Calls.Should().Equal("setProperty");
		runtime.Module.ArgumentCount.Should().Be(3);
		runtime.Calls.Should().Equal(("import", JSInteropVersionHelper.CommonJsUrl));

		await calendar.Instance.DisposeAsync();
		await calendar.Instance.DisposeAsync();

		runtime.Module.DisposeCalls.Should().Be(1);
	}

	/// <summary>A runtime whose module works until it is disposed, when the circuit turns out to have gone.</summary>
	private sealed class DisconnectingJSRuntime : IJSRuntime
	{
		public DisconnectingModule Module { get; } = new();

		/// <summary>Gets or sets an exception thrown when the module is imported, if any.</summary>
		public Exception? ImportException { get; init; }

		/// <summary>Gets each call made, with the identifier and the first argument, such as the module path imported.</summary>
		public List<(string Identifier, string? FirstArgument)> Calls { get; } = [];

		public ValueTask<TValue> InvokeAsync<TValue>(string identifier, object?[]? args)
		{
			Calls.Add((identifier, args?.FirstOrDefault() as string));
			return ImportException is null
				? ValueTask.FromResult((TValue)(object)Module)
				: ValueTask.FromException<TValue>(ImportException);
		}

		public ValueTask<TValue> InvokeAsync<TValue>(string identifier, CancellationToken cancellationToken, object?[]? args)
		{
			cancellationToken.ThrowIfCancellationRequested();
			return InvokeAsync<TValue>(identifier, args);
		}
	}

	/// <summary>A module that records calls and throws when disposed.</summary>
	private sealed class DisconnectingModule : IJSObjectReference
	{
		public List<string> Calls { get; } = [];

		public int DisposeCalls { get; private set; }

		public int ArgumentCount { get; private set; }

		public ValueTask<TValue> InvokeAsync<TValue>(string identifier, object?[]? args)
		{
			Calls.Add(identifier);
			ArgumentCount += args?.Length ?? 0;
			return ValueTask.FromResult(default(TValue)!);
		}

		public ValueTask<TValue> InvokeAsync<TValue>(string identifier, CancellationToken cancellationToken, object?[]? args)
		{
			cancellationToken.ThrowIfCancellationRequested();
			return InvokeAsync<TValue>(identifier, args);
		}

		public ValueTask DisposeAsync()
		{
			DisposeCalls++;
			throw new JSDisconnectedException("The circuit has gone.");
		}
	}
}
