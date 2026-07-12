namespace PanoramicData.Blazor;

public partial class PDDateTimeOffset : IDisposable
{
	private static readonly IReadOnlyList<TimeZoneInfo> _timeZones = TimeZoneInfo.GetSystemTimeZones();

	private string _dateCssClass = string.Empty;
	private string _timeCssClass = string.Empty;
	private System.Threading.Timer? _nowTimer;
	private bool _disposed;

	/// <summary>
	/// An event callback that is invoked when the component loses focus.
	/// </summary>
	[Parameter]
	public EventCallback Blur { get; set; }

	/// <summary>
	/// Gets or sets whether the value tracks the live current time. When <c>true</c> (and <see cref="ShowNow"/>
	/// is enabled) the value is updated automatically and the date, time and time zone inputs are disabled.
	/// Supports two-way binding via <c>@bind-IsNow</c>.
	/// </summary>
	[Parameter]
	public bool IsNow { get; set; }

	/// <summary>
	/// An event callback that is invoked when <see cref="IsNow"/> changes.
	/// </summary>
	[Parameter]
	public EventCallback<bool> IsNowChanged { get; set; }

	/// <summary>
	/// Gets or sets the interval, in milliseconds, at which the value is refreshed while <see cref="IsNow"/> is true.
	/// </summary>
	[Parameter]
	public int LiveUpdateIntervalMs { get; set; } = 1000;

	/// <summary>
	/// Gets or sets whether to show a "Now" checkbox that lets the user follow the live clock or pin a fixed instant.
	/// </summary>
	[Parameter]
	public bool ShowNow { get; set; }

	/// <summary>
	/// Gets or sets whether to show the offset from UTC.
	/// </summary>
	[Parameter]
	public bool ShowOffset { get; set; }

	/// <summary>
	/// Gets or sets whether to show the time part of the value.
	/// </summary>
	[Parameter]
	public bool ShowTime { get; set; }

	/// <summary>
	/// Gets or sets whether to show a named time zone selector (the full set of system time zones) instead of the
	/// numeric UTC offset selector. When enabled, the value's offset is derived from the selected zone for the
	/// chosen instant, so daylight saving is applied correctly. Takes precedence over <see cref="ShowOffset"/>.
	/// </summary>
	[Parameter]
	public bool ShowTimeZones { get; set; }

	/// <summary>
	/// Gets or sets the step in seconds for the time input.
	/// </summary>
	[Parameter]
	public int TimeStepSecs { get; set; } = 1;

	/// <summary>
	/// Gets or sets the identifier of the selected time zone (see <see cref="TimeZoneInfo.Id"/>) when
	/// <see cref="ShowTimeZones"/> is enabled. Defaults to the local time zone when null or unrecognised.
	/// Supports two-way binding via <c>@bind-TimeZoneId</c>.
	/// </summary>
	[Parameter]
	public string? TimeZoneId { get; set; }

	/// <summary>
	/// An event callback that is invoked when <see cref="TimeZoneId"/> changes.
	/// </summary>
	[Parameter]
	public EventCallback<string?> TimeZoneIdChanged { get; set; }

	/// <summary>
	/// Gets or sets the current value.
	/// </summary>
	[Parameter]
	public DateTimeOffset Value { get; set; }

	/// <summary>
	/// An event callback that is invoked when the value changes.
	/// </summary>
	[Parameter]
	public EventCallback<DateTimeOffset> ValueChanged { get; set; }

	/// <summary>
	/// The full set of system time zones offered by the named time zone selector.
	/// </summary>
	private static IReadOnlyList<TimeZoneInfo> TimeZones => _timeZones;

	/// <summary>
	/// The currently selected time zone, resolved from <see cref="TimeZoneId"/> and falling back to the local zone.
	/// </summary>
	private TimeZoneInfo SelectedTimeZone
	{
		get
		{
			if (!string.IsNullOrEmpty(TimeZoneId))
			{
				try
				{
					return TimeZoneInfo.FindSystemTimeZoneById(TimeZoneId);
				}
				catch (TimeZoneNotFoundException)
				{
				}
				catch (InvalidTimeZoneException)
				{
				}
			}

			return TimeZoneInfo.Local;
		}
	}

	private static string OffsetDisplay(double offset)
	{
		var plusMinus = offset < 0 ? "-" : (offset > 0 ? "+" : " ");
		var hours = Math.Floor(Math.Abs(offset));
		var minutes = offset % 1 == 0 ? "00" : "30";
		return $"{plusMinus}{hours:00}:{minutes}";
	}

	/// <summary>
	/// Starts or stops timer-driven live updates whenever parameters change.
	/// </summary>
	protected override void OnParametersSet()
	{
		base.OnParametersSet();
		UpdateNowTimer();
	}

	private async Task OnBlur(FocusEventArgs args) => await Blur.InvokeAsync().ConfigureAwait(true);

	private async Task OnNowToggledAsync(ChangeEventArgs args)
	{
		IsNow = args.Value is bool boolValue
			? boolValue
			: bool.TryParse(args.Value?.ToString(), out var parsed) && parsed;
		await IsNowChanged.InvokeAsync(IsNow).ConfigureAwait(true);

		if (IsNow)
		{
			Value = CurrentInstant();
			await ValueChanged.InvokeAsync(Value).ConfigureAwait(true);
		}

		UpdateNowTimer();
	}

	private Task OnDateInputAsync(ChangeEventArgs args)
	{
		try
		{
			var value = args.Value?.ToString();
			if (value != null && DateTimeOffset.TryParseExact(value, "yyyy-MM-dd", CultureInfo.InvariantCulture, DateTimeStyles.None, out DateTimeOffset dt))
			{
				Value = BuildValue(dt.Date.Add(Value.TimeOfDay));
				_dateCssClass = string.Empty;
				return ValueChanged.InvokeAsync(Value);
			}

			_dateCssClass = "invalid";
		}
		catch
		{
			_dateCssClass = "invalid";
		}

		return Task.CompletedTask;
	}

	private Task OnTimeInputAsync(ChangeEventArgs args)
	{
		try
		{
			var value = args.Value?.ToString();
			if (value != null && DateTimeOffset.TryParseExact(value, "HH:mm:ss", CultureInfo.InvariantCulture, DateTimeStyles.None, out DateTimeOffset dt))
			{
				Value = BuildValue(Value.Date.Add(dt.TimeOfDay));
				_timeCssClass = string.Empty;
				return ValueChanged.InvokeAsync(Value);
			}

			_timeCssClass = "invalid";
		}
		catch
		{
			_timeCssClass = "invalid";
		}

		return Task.CompletedTask;
	}

	private Task OnOffsetInputAsync(ChangeEventArgs args)
	{
		try
		{
			var value = Convert.ToDouble(args.Value, CultureInfo.InvariantCulture);
			if (value != 0)
			{
				var ts = TimeSpan.FromHours(value);
				Value = new DateTimeOffset(Value.DateTime, ts);
				return ValueChanged.InvokeAsync(Value);
			}
		}
		catch
		{
		}

		return Task.CompletedTask;
	}

	private async Task OnTimeZoneInputAsync(ChangeEventArgs args)
	{
		var id = args.Value?.ToString();
		TimeZoneId = string.IsNullOrEmpty(id) ? null : id;
		await TimeZoneIdChanged.InvokeAsync(TimeZoneId).ConfigureAwait(true);

		// Reinterpret the currently displayed local time in the newly selected zone.
		Value = BuildValue(Value.DateTime);
		await ValueChanged.InvokeAsync(Value).ConfigureAwait(true);
	}

	/// <summary>
	/// Builds a value from an unspecified local date/time, applying the selected zone's offset (DST-aware) when
	/// <see cref="ShowTimeZones"/> is enabled, otherwise preserving the current offset.
	/// </summary>
	private DateTimeOffset BuildValue(DateTime localDateTime)
	{
		var offset = ShowTimeZones ? SelectedTimeZone.GetUtcOffset(localDateTime) : Value.Offset;
		return new DateTimeOffset(DateTime.SpecifyKind(localDateTime, DateTimeKind.Unspecified), offset);
	}

	/// <summary>
	/// The current instant expressed in the selected time zone (or local zone).
	/// </summary>
	private DateTimeOffset CurrentInstant()
		=> TimeZoneInfo.ConvertTime(DateTimeOffset.UtcNow, SelectedTimeZone);

	private void UpdateNowTimer()
	{
		var shouldRun = ShowNow && IsNow && IsEnabled;
		if (shouldRun && _nowTimer is null)
		{
			var interval = LiveUpdateIntervalMs > 0 ? LiveUpdateIntervalMs : 1000;
			_nowTimer = new System.Threading.Timer(_ => _ = OnNowTickAsync(), null, 0, interval);
		}
		else if (!shouldRun && _nowTimer is not null)
		{
			_nowTimer.Dispose();
			_nowTimer = null;
		}
	}

	private async Task OnNowTickAsync()
		=> await InvokeAsync(async () =>
		{
			if (_disposed)
			{
				return;
			}

			Value = CurrentInstant();
			await ValueChanged.InvokeAsync(Value).ConfigureAwait(true);
			StateHasChanged();
		}).ConfigureAwait(false);

	/// <summary>
	/// Releases resources used by the component.
	/// </summary>
	public void Dispose()
	{
		Dispose(true);
		GC.SuppressFinalize(this);
	}

	/// <summary>
	/// Releases resources used by the component.
	/// </summary>
	/// <param name="disposing">True when called from <see cref="Dispose()"/>.</param>
	protected virtual void Dispose(bool disposing)
	{
		if (_disposed)
		{
			return;
		}

		if (disposing)
		{
			_nowTimer?.Dispose();
			_nowTimer = null;
		}

		_disposed = true;
	}
}
