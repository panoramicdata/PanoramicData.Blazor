namespace PanoramicData.Blazor;

public partial class PDDateTime
{
	private string _dateCssClass = string.Empty;
	private string _timeCssClass = string.Empty;

	[Parameter]
	public EventCallback Blur { get; set; }

	[Parameter]
	public bool ShowTime { get; set; }

	[Parameter]
	public int TimeStepSecs { get; set; } = 1;

	[Parameter]
	public DateTime Value { get; set; }

	[Parameter]
	public EventCallback<DateTime> ValueChanged { get; set; }

	private async Task OnBlur(FocusEventArgs args) => await Blur.InvokeAsync().ConfigureAwait(true);

	private Task OnDateInputAsync(ChangeEventArgs args)
	{
		try
		{
			var value = args.Value?.ToString();
			if (value != null && DateTime.TryParseExact(value, "yyyy-MM-dd", CultureInfo.InvariantCulture, DateTimeStyles.None, out DateTime dt))
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
