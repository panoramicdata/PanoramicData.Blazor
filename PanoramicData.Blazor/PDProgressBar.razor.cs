namespace PanoramicData.Blazor;

public partial class PDProgressBar
{
	/// <summary>
	/// Gets or sets the content to be displayed within the progress bar.
	/// </summary>
	[Parameter]
	public RenderFragment<PDProgressBar>? BarContent { get; set; }

	/// <summary>
	/// Gets or sets the number of decimal places to display in the percentage.
	/// </summary>
	[Parameter]
	public ushort DecimalPlaces { get; set; }

	/// <summary>
	/// Gets or sets the height of the progress bar.
	/// </summary>
	[Parameter]
	public string Height { get; set; } = "24px";

	/// <summary>
	/// Gets or sets the total value of the progress bar.
	/// </summary>
	[Parameter]
	public double Total { get; set; } = 100;

	/// <summary>
	/// Gets or sets the current value of the progress bar.
	/// </summary>
	[Parameter]
	public double Value { get; set; }

	public double GetPercentage()
	{
		if (Total == 0)
		{
			return 0;
		}
		else
		{
			return Math.Round((Value / Total) * 100, DecimalPlaces);
		}
	}
}