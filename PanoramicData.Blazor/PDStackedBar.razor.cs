namespace PanoramicData.Blazor;

public partial class PDStackedBar
{
	[Parameter]
	public string DateFormat { get; set; } = "dd/MM/yy HH:mm";

	[Parameter]
	public DataPoint DataPoint { get; set; } = new DataPoint();

	[Parameter]
	public bool IsEnabled { get; set; } = true;

	[Parameter]
	public double MaxValue { get; set; } = 100;

	[Parameter]
	public TimelineOptions Options { get; set; } = new TimelineOptions();

	[Parameter]
	public double X { get; set; }

	[Parameter]
	public Func<double, double> YValueTransform { get; set; } = (v) => v;

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

		sb.Append($"{DataPoint.CountLabel}: ").AppendLine(DataPoint!.Count.ToString(CultureInfo.InvariantCulture));
		return sb.ToString();
	}
}
