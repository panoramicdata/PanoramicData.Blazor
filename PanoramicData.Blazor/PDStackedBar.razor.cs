namespace PanoramicData.Blazor;

public partial class PDStackedBar : IEnablable
{
	/// <summary>
	/// Gets or sets the format for displaying dates.
	/// </summary>
	[Parameter]
	public string DateFormat { get; set; } = "dd/MM/yy HH:mm";

	/// <summary>
	/// Gets or sets the data point to be rendered.
	/// </summary>
	[Parameter]
	public DataPoint DataPoint { get; set; } = new DataPoint();

	/// <summary>
	/// Gets or sets the height of the bar.
	/// </summary>
	[Parameter]
	public double Height { get; set; }

	/// <summary>
	/// Gets or sets whether the component is enabled.
	/// </summary>
	[Parameter]
	public bool IsEnabled { get; set; } = true;

	/// <summary>
	/// Gets or sets the maximum value for the bar.
	/// </summary>
	[Parameter]
	public double MaxValue { get; set; } = 100;

	/// <summary>
	/// Gets or sets the timeline options.
	/// </summary>
	[Parameter]
	public TimelineOptions Options { get; set; } = new TimelineOptions();

	/// <summary>
	/// Gets or sets the X coordinate of the bar.
	/// </summary>
	[Parameter]
	public double X { get; set; }

	/// <summary>
	/// A function to transform the Y value of data points.
	/// </summary>
	[Parameter]
	public Func<double, double> YValueTransform { get; set; } = (v) => v;

	public void Disable()
	{
		IsEnabled = false;
		StateHasChanged();
	}

	public void Enable()
	{
		IsEnabled = true;
		StateHasChanged();
	}

	public void SetEnabled(bool isEnabled)
	{
		IsEnabled = isEnabled;
		StateHasChanged();
	}

	private string GetTitle()
	{
		if (DataPoint is null)
		{
			return string.Empty;
		}

		var sb = new StringBuilder();
		sb.AppendLine(DataPoint.StartTime.ToString(DateFormat, CultureInfo.InvariantCulture));
		if (DataPoint != null && DataPoint.SeriesValues.Length > 0)
		{
			for (var i = 0; i < Options.Series.Length; i++)
			{
				sb.Append(Options.Series[i].Label)
				  .Append(": ")
				  .AppendLine(DataPoint.SeriesValues[i].ToString(Options.Series[i].Format, CultureInfo.InvariantCulture));
			}
		}

		sb.Append(CultureInfo.InvariantCulture, $"{DataPoint.CountLabel}: ").AppendLine(DataPoint!.Count.ToString(CultureInfo.InvariantCulture));
		return sb.ToString();
	}
}
