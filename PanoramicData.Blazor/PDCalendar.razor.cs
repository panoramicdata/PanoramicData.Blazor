using PanoramicData.Blazor.Helpers;

namespace PanoramicData.Blazor;

/// <summary>
/// Displays items that have a start and an end in time on a calendar, as a month grid, a week time grid
/// or an agenda list, with navigation, selection and keyboard support.
/// </summary>
/// <typeparam name="TItem">The type of the items shown on the calendar.</typeparam>
/// <remarks>
/// The component is generic: it reads everything it needs from an item through selector delegates, so any
/// type can be shown without converting it to a calendar model. Which day and hour an item falls in is
/// decided by <see cref="TimeZone"/>, including on the days the clocks change, when the week view shows 23
/// or 25 hours. The date arithmetic lives in <see cref="CalendarMath"/> and the layout in
/// <see cref="CalendarLayoutEngine"/> and <see cref="CalendarTimeAxis"/>, all of which can be used and
/// tested on their own. No JavaScript is used unless <see cref="ScrollToHour"/> is set.
/// </remarks>
public partial class PDCalendar<TItem> : IAsyncDisposable
{
	private static readonly DayOfWeek[] _defaultWeekendDays = [DayOfWeek.Saturday, DayOfWeek.Sunday];
	private static readonly CalendarView[] _allViews = [CalendarView.Month, CalendarView.Week, CalendarView.Agenda];

	private readonly string _id = $"pdcal-{ComponentIdSequence.Next()}";

	private bool _initialised;
	private bool _disposed;
	private DateOnly _date;
	private DateOnly? _lastDateParameter;
	private CalendarView _view;
	private CalendarView _lastViewParameter;
	private TItem? _selected;
	private TItem? _lastSelectedParameter;
	private (DateTimeOffset From, DateTimeOffset To)? _lastRange;
	private int _focusedRow;
	private bool _preventDefaultKey;
	private bool _scrollPending;
	private DateOnly _today;

	private IReadOnlyList<DateOnly> _dates = [];
	private IReadOnlyList<IReadOnlyList<DateOnly>> _weeks = [];
	private Dictionary<DateOnly, IReadOnlyList<CalendarSegment<TItem>>> _segmentsByDate = [];
	private CalendarTimeAxis? _axis;

	private ITimer? _timer;
	private TimeProvider? _timerClock;
	private IJSObjectReference? _commonModule;

	/// <summary>Gets or sets the JavaScript runtime, used only to scroll the week view when <see cref="ScrollToHour"/> is set.</summary>
	[Inject] public IJSRuntime JSRuntime { get; set; } = null!;

	/// <summary>Gets or sets the items to show. Only the items within the visible range are laid out, so a host may supply just those (see <see cref="OnVisibleRangeChanged"/>).</summary>
	[Parameter] public IEnumerable<TItem>? Items { get; set; }

	/// <summary>Gets or sets the function returning the instant an item starts. Required for items to be shown.</summary>
	[Parameter] public Func<TItem, DateTimeOffset>? StartSelector { get; set; }

	/// <summary>Gets or sets the function returning the instant an item ends. Required for items to be shown; an end before the start is treated as the start.</summary>
	[Parameter] public Func<TItem, DateTimeOffset>? EndSelector { get; set; }

	/// <summary>Gets or sets the function returning the title of an item. When not supplied, the item's ToString is used.</summary>
	[Parameter] public Func<TItem, string>? TitleSelector { get; set; }

	/// <summary>Gets or sets the function returning a key that identifies an item, used to recognise the selected item when items are reloaded as new instances. When not supplied, items are compared with their own equality.</summary>
	[Parameter] public Func<TItem, object>? KeySelector { get; set; }

	/// <summary>Gets or sets the function returning extra CSS classes for an item.</summary>
	[Parameter] public Func<TItem, string?>? CssClassSelector { get; set; }

	/// <summary>Gets or sets the function returning the hover text for an item. When not supplied, the title and times are used.</summary>
	[Parameter] public Func<TItem, string?>? TooltipSelector { get; set; }

	/// <summary>Gets or sets the function deciding whether an item is disabled. A disabled item is shown but cannot be selected.</summary>
	[Parameter] public Func<TItem, bool>? IsDisabledSelector { get; set; }

	/// <summary>Gets or sets an optional template for the content of an item, replacing the default title and time.</summary>
	[Parameter] public RenderFragment<TItem>? ItemTemplate { get; set; }

	/// <summary>Gets or sets the time zone that decides which day and hour an item falls in. The default is the local time zone.</summary>
	[Parameter] public TimeZoneInfo TimeZone { get; set; } = TimeZoneInfo.Local;

	/// <summary>Gets or sets the clock used for today and the current time indicator. The default is the system clock.</summary>
	[Parameter] public TimeProvider Clock { get; set; } = TimeProvider.System;

	/// <summary>Gets or sets the focused date, which decides the period shown. When null, today is used.</summary>
	[Parameter] public DateOnly? Date { get; set; }

	/// <summary>Gets or sets the callback raised when the focused date changes, supporting two-way binding.</summary>
	[Parameter] public EventCallback<DateOnly?> DateChanged { get; set; }

	/// <summary>Gets or sets the view shown.</summary>
	[Parameter] public CalendarView View { get; set; } = CalendarView.Month;

	/// <summary>Gets or sets the callback raised when the view changes, supporting two-way binding.</summary>
	[Parameter] public EventCallback<CalendarView> ViewChanged { get; set; }

	/// <summary>Gets or sets the views offered by the view switcher, in order.</summary>
	[Parameter] public IReadOnlyList<CalendarView> AvailableViews { get; set; } = _allViews;

	/// <summary>Gets or sets the day on which weeks start. The default is Monday.</summary>
	[Parameter] public DayOfWeek FirstDayOfWeek { get; set; } = DayOfWeek.Monday;

	/// <summary>Gets or sets the selected item.</summary>
	[Parameter] public TItem? SelectedItem { get; set; }

	/// <summary>Gets or sets the callback raised when the selected item changes, supporting two-way binding.</summary>
	[Parameter] public EventCallback<TItem?> SelectedItemChanged { get; set; }

	/// <summary>Gets or sets the callback raised when the user selects an item by clicking it or pressing Enter on it.</summary>
	[Parameter] public EventCallback<TItem> OnItemSelected { get; set; }

	/// <summary>Gets or sets the callback raised when the user double clicks an item.</summary>
	[Parameter] public EventCallback<TItem> OnItemDoubleClick { get; set; }

	/// <summary>Gets or sets the callback raised when the user selects an empty hour slot or a day, giving the instant the slot or day starts.</summary>
	[Parameter] public EventCallback<DateTimeOffset> OnSlotSelected { get; set; }

	/// <summary>Gets or sets the callback raised whenever the visible range changes, including when first shown, so that a host can load items lazily. The range runs from the start of the first visible day to the start of the day after the last.</summary>
	[Parameter] public EventCallback<(DateTimeOffset From, DateTimeOffset To)> OnVisibleRangeChanged { get; set; }

	/// <summary>Gets or sets a value indicating whether items are loading, which shows the busy overlay.</summary>
	[Parameter] public bool IsLoading { get; set; }

	/// <summary>Gets or sets a value indicating whether the week view draws a line at the current time, refreshed every minute.</summary>
	[Parameter] public bool NowIndicator { get; set; } = true;

	/// <summary>Gets or sets the earliest date that can be shown or navigated to.</summary>
	[Parameter] public DateOnly? MinDate { get; set; }

	/// <summary>Gets or sets the latest date that can be shown or navigated to.</summary>
	[Parameter] public DateOnly? MaxDate { get; set; }

	/// <summary>Gets or sets the function returning extra CSS classes for a day.</summary>
	[Parameter] public Func<DateOnly, string?>? DayCssClassSelector { get; set; }

	/// <summary>Gets or sets a value indicating whether weekend days are shaded.</summary>
	[Parameter] public bool ShadeWeekends { get; set; } = true;

	/// <summary>Gets or sets the days treated as the weekend. The default is Saturday and Sunday.</summary>
	[Parameter] public IReadOnlyCollection<DayOfWeek> WeekendDays { get; set; } = _defaultWeekendDays;

	/// <summary>Gets or sets the most items listed in a day of the month view before a "+n more" button is shown, which opens that day's agenda.</summary>
	[Parameter] public int MaxItemsPerDay { get; set; } = 3;

	/// <summary>Gets or sets the number of days listed by the agenda view, starting at the focused date.</summary>
	[Parameter] public int AgendaDays { get; set; } = 7;

	/// <summary>Gets or sets the first local hour shown by the week view, from 0 to 23.</summary>
	[Parameter] public int FirstVisibleHour { get; set; }

	/// <summary>Gets or sets the local hour at which the week view stops, from 1 to 24.</summary>
	[Parameter] public int LastVisibleHour { get; set; } = 24;

	/// <summary>Gets or sets the height in pixels of one hour of the week view.</summary>
	[Parameter] public double SlotHeight { get; set; } = 48;

	/// <summary>Gets or sets the smallest height in pixels at which an item is drawn in the week view, so that short items stay clickable.</summary>
	[Parameter] public double MinItemHeight { get; set; } = 22;

	/// <summary>Gets or sets the CSS maximum height of the scrolling area of the week view.</summary>
	[Parameter] public string ScrollHeight { get; set; } = "36rem";

	/// <summary>Gets or sets the local hour the week view scrolls to when it is shown. When null, no scrolling is done and no JavaScript is loaded.</summary>
	[Parameter] public int? ScrollToHour { get; set; }

	/// <summary>Gets or sets the .NET format string used for times.</summary>
	[Parameter] public string TimeFormat { get; set; } = "HH:mm";

	/// <summary>Gets or sets a value indicating whether the toolbar is shown at all.</summary>
	[Parameter] public bool ShowToolbar { get; set; } = true;

	/// <summary>Gets or sets a value indicating whether the toolbar shows the previous and next buttons.</summary>
	[Parameter] public bool ShowNavigation { get; set; } = true;

	/// <summary>Gets or sets a value indicating whether the toolbar shows the today button.</summary>
	[Parameter] public bool ShowToday { get; set; } = true;

	/// <summary>Gets or sets a value indicating whether the toolbar shows the title of the visible period.</summary>
	[Parameter] public bool ShowTitle { get; set; } = true;

	/// <summary>Gets or sets a value indicating whether the toolbar shows the view switcher.</summary>
	[Parameter] public bool ShowViewSwitcher { get; set; } = true;

	/// <summary>Gets or sets the text shown by the agenda view when there are no items.</summary>
	[Parameter] public string EmptyText { get; set; } = "No items";

	/// <summary>Gets or sets the accessible label of the calendar.</summary>
	[Parameter] public string AriaLabel { get; set; } = "Calendar";

	/// <summary>Gets or sets extra CSS classes for the outermost element.</summary>
	[Parameter] public string? CssClass { get; set; }

	/// <summary>Gets the unique identifier of the outermost element.</summary>
	public string Id => _id;

	/// <summary>Gets the focused date currently shown.</summary>
	public DateOnly CurrentDate => _date;

	/// <summary>Gets the view currently shown.</summary>
	public CalendarView CurrentView => _view;

	/// <summary>Gets the item currently selected, if any.</summary>
	public TItem? CurrentSelection => _selected;

	/// <summary>Gets the range of time currently shown, from the start of the first day to the start of the day after the last.</summary>
	public (DateTimeOffset From, DateTimeOffset To) VisibleRange
		=> CalendarMath.GetVisibleRange(_view, _date, FirstDayOfWeek, AgendaDays, TimeZone);

	/// <summary>Gets the title of the visible period, such as "October 2026" or "12 to 18 Oct 2026".</summary>
	public string Title => CalendarMath.GetTitle(_view, _date, FirstDayOfWeek, AgendaDays, CultureInfo.CurrentCulture);

	/// <summary>Gets or sets the scrolling element of the week view, set by the markup.</summary>
	protected ElementReference ScrollElement { get; set; }

	private DateOnly Today => CalendarMath.GetDate(Clock.GetUtcNow(), TimeZone);

	/// <inheritdoc />
	protected override async Task OnParametersSetAsync()
	{
		_today = Today;

		if (!_initialised || Date != _lastDateParameter)
		{
			_date = Date ?? _today;
		}

		if (!_initialised || View != _lastViewParameter)
		{
			_scrollPending |= View == CalendarView.Week && (!_initialised || _view != View);
			_view = View;
		}

		if (!_initialised || !EqualityComparer<TItem?>.Default.Equals(SelectedItem, _lastSelectedParameter))
		{
			_selected = SelectedItem;
		}

		_lastDateParameter = Date;
		_lastViewParameter = View;
		_lastSelectedParameter = SelectedItem;
		_initialised = true;

		_date = CalendarMath.Clamp(_date, MinDate, MaxDate);
		EnsureTimer();
		BuildView();

		await RaiseVisibleRangeChangedAsync().ConfigureAwait(true);
	}

	/// <inheritdoc />
	protected override async Task OnAfterRenderAsync(bool firstRender)
	{
		if (!_scrollPending || _view != CalendarView.Week || !ScrollToHour.HasValue || _axis is null)
		{
			return;
		}

		_scrollPending = false;
		var rowIndex = FindRowAtOrAfter(ScrollToHour.Value);

		try
		{
			_commonModule ??= await JSRuntime.InvokeAsync<IJSObjectReference>("import", JSInteropVersionHelper.CommonJsUrl).ConfigureAwait(true);
			if (_commonModule is not null)
			{
				await _commonModule.InvokeVoidAsync("setProperty", ScrollElement, "scrollTop", rowIndex * SlotHeight).ConfigureAwait(true);
			}
		}
		catch (JSDisconnectedException)
		{
			// The circuit went away; there is nothing left to scroll.
		}
		catch (ObjectDisposedException)
		{
			// Fast page switching can dispose the component before the import completes.
		}
	}

	/// <summary>
	/// Shows the period after or before the current one.
	/// </summary>
	/// <param name="periods">The number of periods to move; negative to go back.</param>
	/// <returns>A task that completes when the calendar has moved.</returns>
	public async Task NavigateAsync(int periods)
	{
		if (!CanNavigate(periods))
		{
			return;
		}

		await SetDateAsync(GetNavigationTarget(periods)).ConfigureAwait(true);
	}

	/// <summary>
	/// Shows the period containing today.
	/// </summary>
	/// <returns>A task that completes when the calendar has moved.</returns>
	public async Task GoToTodayAsync()
	{
		_today = Today;
		if (!IsDateDisabled(_today))
		{
			await SetDateAsync(_today).ConfigureAwait(true);
		}
	}

	/// <summary>
	/// Focuses a date, showing the period that contains it. Dates outside <see cref="MinDate"/> and <see cref="MaxDate"/> are ignored.
	/// </summary>
	/// <param name="date">The date to show.</param>
	/// <returns>A task that completes when the calendar has moved.</returns>
	public async Task GoToDateAsync(DateOnly date)
	{
		if (!IsDateDisabled(date))
		{
			await SetDateAsync(date).ConfigureAwait(true);
		}
	}

	/// <summary>
	/// Switches to another view.
	/// </summary>
	/// <param name="view">The view to show.</param>
	/// <returns>A task that completes when the view has changed.</returns>
	public async Task SetViewAsync(CalendarView view)
	{
		if (view == _view)
		{
			return;
		}

		_view = view;
		_scrollPending = view == CalendarView.Week;
		BuildView();
		StateHasChanged();

		await ViewChanged.InvokeAsync(view).ConfigureAwait(true);
		await RaiseVisibleRangeChangedAsync().ConfigureAwait(true);
	}

	/// <summary>
	/// Shows the agenda for a single day, as the month view's "+n more" button does.
	/// </summary>
	/// <param name="date">The day to show.</param>
	/// <returns>A task that completes when the agenda is shown.</returns>
	public async Task ShowDayAgendaAsync(DateOnly date)
	{
		if (IsDateDisabled(date))
		{
			return;
		}

		var dateChanged = date != _date;
		_date = date;
		var viewChanged = _view != CalendarView.Agenda;
		_view = CalendarView.Agenda;
		BuildView();
		StateHasChanged();

		if (dateChanged)
		{
			await DateChanged.InvokeAsync(date).ConfigureAwait(true);
		}

		if (viewChanged)
		{
			await ViewChanged.InvokeAsync(CalendarView.Agenda).ConfigureAwait(true);
		}

		await RaiseVisibleRangeChangedAsync().ConfigureAwait(true);
	}

	/// <summary>
	/// Gets whether navigating by a number of periods would show a different period within the date bounds.
	/// </summary>
	/// <param name="periods">The number of periods; negative to go back.</param>
	/// <returns>True when the navigation is possible.</returns>
	public bool CanNavigate(int periods)
	{
		var target = GetNavigationTarget(periods);
		return CalendarMath.GetVisibleRange(_view, target, FirstDayOfWeek, AgendaDays, TimeZone) != VisibleRange
			&& !IsDateDisabled(target);
	}

	private DateOnly GetNavigationTarget(int periods)
		=> CalendarMath.Clamp(CalendarMath.Navigate(_view, _date, periods, AgendaDays), MinDate, MaxDate);

	private bool IsDateDisabled(DateOnly date)
		=> (MinDate.HasValue && date < MinDate.Value) || (MaxDate.HasValue && date > MaxDate.Value);

	private bool IsWeekend(DateOnly date) => WeekendDays.Contains(date.DayOfWeek);

	private async Task SetDateAsync(DateOnly date)
	{
		if (date == _date)
		{
			return;
		}

		_date = date;
		BuildView();
		StateHasChanged();

		await DateChanged.InvokeAsync(date).ConfigureAwait(true);
		await RaiseVisibleRangeChangedAsync().ConfigureAwait(true);
	}

	private async Task RaiseVisibleRangeChangedAsync()
	{
		var range = VisibleRange;
		if (_lastRange == range)
		{
			return;
		}

		_lastRange = range;
		await OnVisibleRangeChanged.InvokeAsync(range).ConfigureAwait(true);
	}

	/// <summary>
	/// Recomputes the days and the per-day item layout for the current view.
	/// </summary>
	private void BuildView()
	{
		_dates = CalendarMath.GetVisibleDates(_view, _date, FirstDayOfWeek, AgendaDays);
		_weeks = _view == CalendarView.Month ? CalendarMath.GetMonthWeeks(_date, FirstDayOfWeek) : [];

		var segments = StartSelector is null || EndSelector is null
			? []
			: CalendarLayoutEngine.SplitByDay(Items, StartSelector, EndSelector, _dates, TimeZone);

		if (_view == CalendarView.Week)
		{
			_axis = CalendarTimeAxis.Create(_dates, TimeZone, FirstVisibleHour, LastVisibleHour);
			_focusedRow = Math.Clamp(_focusedRow, 0, Math.Max(0, _axis.Rows.Count - 1));
			var minimumDuration = TimeSpan.FromHours(SlotHeight > 0 ? MinItemHeight / SlotHeight : 0);
			_segmentsByDate = segments
				.GroupBy(segment => segment.Date)
				.ToDictionary(group => group.Key, group => CalendarLayoutEngine.PackColumns(group, minimumDuration));
		}
		else
		{
			_axis = null;
			_segmentsByDate = segments
				.GroupBy(segment => segment.Date)
				.ToDictionary(group => group.Key, group => (IReadOnlyList<CalendarSegment<TItem>>)
					[.. group.OrderByDescending(segment => segment.IsMultiDay).ThenBy(segment => segment.Start)]);
		}
	}

	private IReadOnlyList<CalendarSegment<TItem>> GetSegments(DateOnly date)
		=> _segmentsByDate.TryGetValue(date, out var segments) ? segments : [];

	private int FindRowAtOrAfter(int hour)
	{
		var rows = _axis!.Rows;
		for (var rowIndex = 0; rowIndex < rows.Count; rowIndex++)
		{
			if (rows[rowIndex].Hour >= hour)
			{
				return rowIndex;
			}
		}

		return Math.Max(0, rows.Count - 1);
	}

	private void EnsureTimer()
	{
		if (!NowIndicator)
		{
			StopTimer();
			return;
		}

		if (_timer is not null && ReferenceEquals(_timerClock, Clock))
		{
			return;
		}

		StopTimer();
		_timerClock = Clock;

		// tick on each minute boundary, so the indicator moves when the clock's minute does
		var now = Clock.GetUtcNow();
		var untilNextMinute = TimeSpan.FromMinutes(1) - TimeSpan.FromTicks(now.Ticks % TimeSpan.TicksPerMinute);
		_timer = Clock.CreateTimer(_ => _ = OnTimerTickAsync(), null, untilNextMinute, TimeSpan.FromMinutes(1));
	}

	private void StopTimer()
	{
		_timer?.Dispose();
		_timer = null;
		_timerClock = null;
	}

	private async Task OnTimerTickAsync()
	{
		if (_disposed)
		{
			return;
		}

		try
		{
			await InvokeAsync(() =>
			{
				var today = Today;
				if (today != _today)
				{
					_today = today;
					BuildView();
				}

				StateHasChanged();
			}).ConfigureAwait(true);
		}
		catch (ObjectDisposedException)
		{
			// The renderer has gone; the next tick will not come.
		}
	}

	/// <inheritdoc />
	public async ValueTask DisposeAsync()
	{
		_disposed = true;
		StopTimer();

		if (_commonModule is not null)
		{
			try
			{
				await _commonModule.DisposeAsync().ConfigureAwait(true);
			}
			catch (JSDisconnectedException)
			{
				// The circuit has already gone, and the module with it.
			}

			_commonModule = null;
		}

		GC.SuppressFinalize(this);
	}
}
