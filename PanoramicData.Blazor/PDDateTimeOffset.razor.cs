namespace PanoramicData.Blazor;

/// <summary>
/// A Blazor component that provides a date, time, and time zone offset input for <see cref="DateTimeOffset"/> values.
/// </summary>
public partial class PDDateTimeOffset : IDisposable
{
	private static readonly IReadOnlyList<TimeZoneInfo> _timeZones = TimeZoneInfo.GetSystemTimeZones();
	private static readonly Dictionary<double, string> _offsetZoneNames = BuildOffsetZoneNames();

	private string _dateCssClass = string.Empty;
	private string _timeCssClass = string.Empty;
	private Timer? _nowTimer;
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
			// An unknown or unreadable zone is not found, and falls back to the local zone
			return !string.IsNullOrEmpty(TimeZoneId) && TimeZoneInfo.TryFindSystemTimeZoneById(TimeZoneId, out var timeZone)
				? timeZone
				: TimeZoneInfo.Local;
		}
	}

	/// <summary>
	/// Builds a lookup of UTC-offset (in hours) to a representative system time zone display name, so the numeric
	/// offset selector can also show a recognisable time zone name. The first system zone with a matching base
	/// (standard) offset is used as the representative.
	/// </summary>
	private static Dictionary<double, string> BuildOffsetZoneNames()
	{
		var map = new Dictionary<double, string>();
		foreach (var timeZone in _timeZones)
		{
			var hours = timeZone.BaseUtcOffset.TotalHours;
			if (!map.ContainsKey(hours))
			{
				map[hours] = timeZone.DisplayName;
			}
		}

		return map;
	}

	/// <summary>
	/// The text shown in the native date input, which needs an ISO date whatever the culture's calendar.
	/// </summary>
	private string DateText => Value.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture);

	/// <summary>
	/// The text shown in the native time input, which needs HH:mm:ss whatever the culture's time separator.
	/// </summary>
	private string TimeText => Value.ToString("HH:mm:ss", CultureInfo.InvariantCulture);

	/// <summary>
	/// Finds the listed option that stands for a zone. A zone can be valid yet unlisted: on Linux with the
	/// machine zone Etc/UTC, <see cref="TimeZoneInfo.Local"/> has the id "Etc/UTC" while
	/// <see cref="TimeZoneInfo.GetSystemTimeZones()"/> lists it only as "UTC". Such a zone is matched to a listed
	/// zone with the same Windows id and the same rules.
	/// </summary>
	/// <param name="zone">The zone to find.</param>
	/// <param name="listed">The zones offered as options.</param>
	/// <returns>The id of the listed zone that stands for <paramref name="zone"/>, or null when none does.</returns>
	internal static string? ResolveListedTimeZoneId(TimeZoneInfo zone, IReadOnlyList<TimeZoneInfo> listed)
	{
		if (listed.Any(listedZone => listedZone.Id == zone.Id))
		{
			return zone.Id;
		}

		var windowsId = ToWindowsId(zone.Id);
		return listed.FirstOrDefault(listedZone => ToWindowsId(listedZone.Id) == windowsId && listedZone.HasSameRules(zone))?.Id;
	}

	/// <summary>
	/// Gets the zones offered by the named time zone selector: the listed zones, preceded by the zone in use when
	/// that is not listed and has no listed equivalent, so that it can still be shown as selected.
	/// </summary>
	/// <param name="selected">The zone in use.</param>
	/// <param name="listed">The zones offered as options.</param>
	/// <returns>The zones to offer, and the id of the one to show as selected.</returns>
	internal static (IReadOnlyList<TimeZoneInfo> Options, string SelectedId) GetTimeZoneOptions(TimeZoneInfo selected, IReadOnlyList<TimeZoneInfo> listed)
	{
		var selectedId = ResolveListedTimeZoneId(selected, listed);
		return selectedId is null
			? ([selected, .. listed], selected.Id)
			: (listed, selectedId);
	}

	private static string ToWindowsId(string id)
		=> TimeZoneInfo.TryConvertIanaIdToWindowsId(id, out var windowsId) ? windowsId : id;

	private static string StripUtcPrefix(string displayName)
	{
		// System time zone display names start with the offset, as in "(UTC+01:00) Amsterdam, Berlin", so the
		// leading "(UTC...) " is stripped to avoid showing the offset twice alongside the numeric one.
		var closeIndex = displayName.IndexOf(") ", StringComparison.Ordinal);
		return closeIndex >= 0 && closeIndex + 2 < displayName.Length
			? displayName[(closeIndex + 2)..]
			: displayName;
	}

	private static string OffsetDisplay(double offset)
	{
		var plusMinus = offset < 0 ? "-" : (offset > 0 ? "+" : " ");
		var hours = Math.Floor(Math.Abs(offset));
		var minutes = (int)Math.Round(Math.Abs(offset) * 60) % 60 == 0 ? "00" : "30";
		var label = $"{plusMinus}{hours:00}:{minutes}";
		return _offsetZoneNames.TryGetValue(offset, out var displayName)
			? $"{label}  {StripUtcPrefix(displayName)}"
			: label;
	}

	/// <summary>
	/// Starts or stops timer-driven live updates whenever parameters change.
	/// </summary>
	protected override void OnParametersSet()
	{
		base.OnParametersSet();
		UpdateNowTimer();
	}

	private async Task OnBlur() => await Blur.InvokeAsync().ConfigureAwait(true);

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
		var value = args.Value?.ToString();
		if (value != null && DateTimeOffset.TryParseExact(value, "yyyy-MM-dd", CultureInfo.InvariantCulture, DateTimeStyles.None, out DateTimeOffset dt))
		{
			Value = BuildValue(dt.Date.Add(Value.TimeOfDay));
			_dateCssClass = string.Empty;
			return ValueChanged.InvokeAsync(Value);
		}

		_dateCssClass = "invalid";
		return Task.CompletedTask;
	}

	private Task OnTimeInputAsync(ChangeEventArgs args)
	{
		var value = args.Value?.ToString();
		if (value != null && DateTimeOffset.TryParseExact(value, "HH:mm:ss", CultureInfo.InvariantCulture, DateTimeStyles.None, out DateTimeOffset dt))
		{
			Value = BuildValue(Value.Date.Add(dt.TimeOfDay));
			_timeCssClass = string.Empty;
			return ValueChanged.InvokeAsync(Value);
		}

		_timeCssClass = "invalid";
		return Task.CompletedTask;
	}

	private Task OnOffsetInputAsync(ChangeEventArgs args)
	{
		try
		{
			var value = Convert.ToDouble(args.Value, CultureInfo.InvariantCulture);
			var ts = TimeSpan.FromHours(value);
			Value = new DateTimeOffset(Value.DateTime, ts);
			return ValueChanged.InvokeAsync(Value);
		}
		catch
		{
			// Not an offset that can be applied (not a number, or out of range): keep the current one
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
			_nowTimer = new Timer(_ => OnNowTick(), null, 0, interval);
		}
		else if (!shouldRun && _nowTimer is not null)
		{
			_nowTimer.Dispose();
			_nowTimer = null;
		}
	}

	// Each tick is marshalled onto the renderer, and the timer thread does not wait for it to finish
	private void OnNowTick()
		=> _ = InvokeAsync(async () =>
		{
			if (_disposed)
			{
				return;
			}

			Value = CurrentInstant();
			await ValueChanged.InvokeAsync(Value).ConfigureAwait(true);
			StateHasChanged();
		});

	/// <summary>
	/// Releases resources used by the component.
	/// </summary>
	public void Dispose()
	{
		_nowTimer?.Dispose();
		_nowTimer = null;
		Dispose(true);
		GC.SuppressFinalize(this);
	}

	/// <summary>
	/// Releases resources used by the component.
	/// </summary>
	/// <param name="disposing">True when called from <see cref="Dispose()"/>.</param>
	protected virtual void Dispose(bool disposing)
		=> _disposed = true;
}
