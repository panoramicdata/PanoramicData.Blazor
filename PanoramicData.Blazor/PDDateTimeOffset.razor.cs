namespace PanoramicData.Blazor;

public partial class PDDateTimeOffset
{
	private string _dateCssClass = string.Empty;
	private string _timeCssClass = string.Empty;

	[Parameter]
	public EventCallback Blur { get; set; }

	[Parameter]
	public bool ShowOffset { get; set; }

	[Parameter]
	public bool ShowTime { get; set; }

	[Parameter]
	public int TimeStepSecs { get; set; } = 1;

	[Parameter]
	public DateTimeOffset Value { get; set; }

	[Parameter]
	public EventCallback<DateTimeOffset> ValueChanged { get; set; }

	private static string OffsetDisplay(double offset)
	{
		var plusMinus = offset < 0 ? "-" : (offset > 0 ? "+" : " ");
		var hours = Math.Floor(Math.Abs(offset));
		var minutes = offset % 1 == 0 ? "00" : "30";
		return $"{plusMinus}{hours:00}:{minutes}";
	}

	private async Task OnBlur(FocusEventArgs args) => await Blur.InvokeAsync().ConfigureAwait(true);

	private Task OnDateInputAsync(ChangeEventArgs args)
	{
		try
		{
			var value = args.Value?.ToString();
			if (value != null && DateTimeOffset.TryParseExact(value, "yyyy-MM-dd", CultureInfo.InvariantCulture, DateTimeStyles.None, out DateTimeOffset dt))
			{
				Value = new DateTimeOffset(dt.Date.Add(Value.TimeOfDay), Value.Offset);
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
				Value = new DateTimeOffset(Value.Date.Add(dt.TimeOfDay), Value.Offset);
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
}
