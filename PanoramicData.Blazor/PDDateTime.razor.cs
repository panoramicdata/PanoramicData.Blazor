namespace PanoramicData.Blazor;

/// <summary>
/// A Blazor component that provides a date and optional time input.
/// </summary>
public partial class PDDateTime
{
	private string _dateCssClass = string.Empty;
	private string _timeCssClass = string.Empty;

	/// <summary>
	/// An event callback that is invoked when the component loses focus.
	/// </summary>
	[Parameter]
	public EventCallback Blur { get; set; }

	/// <summary>
	/// Gets or sets the date format string used for display and parsing.
	/// Defaults to "yyyy-MM-dd". When set to the default, the native browser date picker is used.
	/// When set to a custom format, a text input is used instead.
	/// </summary>
	[Parameter]
	public string DateFormat { get; set; } = "yyyy-MM-dd";

	private bool UseNativeDatePicker => DateFormat == "yyyy-MM-dd";

	/// <summary>
	/// The text shown in the date input. The native picker needs an ISO date whatever the culture; a custom
	/// format is shown in the current culture, which is also the culture it is read back in.
	/// </summary>
	private string DateText => UseNativeDatePicker
		? Value.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture)
		: Value.ToString(DateFormat, CultureInfo.CurrentCulture);

	/// <summary>
	/// The text shown in the native time input, which needs HH:mm:ss whatever the culture's time separator.
	/// </summary>
	private string TimeText => Value.ToString("HH:mm:ss", CultureInfo.InvariantCulture);

	/// <summary>
	/// Reads a date typed into the date input: as an ISO date for the native picker, otherwise in the custom
	/// format in the current culture (as displayed) and then, for compatibility, the invariant culture.
	/// </summary>
	private bool TryParseDate(string? text, out DateTime date)
	{
		date = default;
		if (text is null)
		{
			return false;
		}

		if (UseNativeDatePicker)
		{
			return DateTime.TryParseExact(text, "yyyy-MM-dd", CultureInfo.InvariantCulture, DateTimeStyles.None, out date);
		}

		return DateTime.TryParseExact(text, DateFormat, CultureInfo.CurrentCulture, DateTimeStyles.None, out date)
			|| DateTime.TryParseExact(text, DateFormat, CultureInfo.InvariantCulture, DateTimeStyles.None, out date);
	}

	/// <summary>
	/// Gets or sets whether to show the time part of the value.
	/// </summary>
	[Parameter]
	public bool ShowTime { get; set; }

	/// <summary>
	/// Gets or sets the step in seconds for the time input.
	/// </summary>
	[Parameter]
	public int TimeStepSecs { get; set; } = 1;

	/// <summary>
	/// Gets or sets the current value.
	/// </summary>
	[Parameter]
	public DateTime Value { get; set; }

	/// <summary>
	/// An event callback that is invoked when the value changes.
	/// </summary>
	[Parameter]
	public EventCallback<DateTime> ValueChanged { get; set; }

	private async Task OnBlur() => await Blur.InvokeAsync().ConfigureAwait(true);

	private Task OnDateInputAsync(ChangeEventArgs args)
	{
		if (TryParseDate(args.Value?.ToString(), out var dt))
		{
			Value = dt.Date.Add(Value.TimeOfDay);
			_dateCssClass = string.Empty;
			return ValueChanged.InvokeAsync(Value);
		}

		_dateCssClass = "invalid";
		return Task.CompletedTask;
	}

	private Task OnTimeInputAsync(ChangeEventArgs args)
	{
		var value = args.Value?.ToString();
		if (value != null && DateTime.TryParseExact(value, "HH:mm:ss", CultureInfo.InvariantCulture, DateTimeStyles.None, out DateTime dt))
		{
			Value = Value.Date.Add(dt.TimeOfDay);
			_timeCssClass = string.Empty;
			return ValueChanged.InvokeAsync(Value);
		}

		_timeCssClass = "invalid";
		return Task.CompletedTask;
	}
}
