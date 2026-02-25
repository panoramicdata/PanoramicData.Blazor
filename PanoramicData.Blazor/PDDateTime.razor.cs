namespace PanoramicData.Blazor;

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

	private async Task OnBlur(FocusEventArgs args) => await Blur.InvokeAsync().ConfigureAwait(true);

	private Task OnDateInputAsync(ChangeEventArgs args)
	{
		try
		{
			var value = args.Value?.ToString();
			var parseFormat = UseNativeDatePicker ? "yyyy-MM-dd" : DateFormat;
			if (value != null && DateTime.TryParseExact(value, parseFormat, CultureInfo.InvariantCulture, DateTimeStyles.None, out DateTime dt))
			{
				Value = dt.Date.Add(Value.TimeOfDay);
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
			if (value != null && DateTime.TryParseExact(value, "HH:mm:ss", CultureInfo.InvariantCulture, DateTimeStyles.None, out DateTime dt))
			{
				Value = Value.Date.Add(dt.TimeOfDay);
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
}
