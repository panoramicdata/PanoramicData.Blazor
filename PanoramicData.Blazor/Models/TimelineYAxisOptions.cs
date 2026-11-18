namespace PanoramicData.Blazor.Models;

/// <summary>
/// Configuration options for the Y-axis (value axis) of a <see cref="PanoramicData.Blazor.PDTimeline"/> component.
/// </summary>
public class TimelineYAxisOptions
{
	/// <summary>Gets or sets an explicit maximum value for the Y axis. When <c>null</c>, the maximum is derived from the data.</summary>
	public double? MaxValue { get; set; }

	/// <summary>Gets or sets whether the top of the plot is labelled with the Y-axis maximum: <see cref="MaxValue"/> when set, otherwise the total of the tallest visible bar.</summary>
	public bool ShowMaxValue { get; set; }

	/// <summary>Gets or sets the text colour of the maximum value label.</summary>
	public string MaxValueColour { get; set; } = "Gray";

	/// <summary>Gets or sets the background colour behind the maximum value label, which keeps it readable over a bar.</summary>
	public string MaxValueBackgroundColour { get; set; } = "rgba(255, 255, 255, 0.7)";

	/// <summary>Gets or sets the .NET format string for the maximum value label.</summary>
	public string MaxValueFormat { get; set; } = "#,0.##";

	/// <summary>Gets or sets whether faint horizontal gridlines are drawn behind the bars.</summary>
	public bool ShowGridLines { get; set; }

	/// <summary>Gets or sets the most gridlines to draw; they are placed at round-number steps (1, 2 or 5 times a power of ten) below the maximum.</summary>
	public int GridLineCount { get; set; } = 4;

	/// <summary>Gets or sets the colour of the gridlines.</summary>
	public string GridLineColour { get; set; } = "rgba(128, 128, 128, 0.25)";
}
