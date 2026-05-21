namespace PanoramicData.Blazor.Models;

/// <summary>
/// Describes a single series used by timeline chart rendering.
/// </summary>
public class TimelineSeries
{
	/// <summary>
	/// Gets or sets the series colour.
	/// </summary>
	public string Colour { get; set; } = "Green";
	/// <summary>
	/// Gets or sets the numeric format string used for the series values.
	/// </summary>
	public string Format { get; set; } = "0,0";
	/// <summary>
	/// Gets or sets the display label for the series.
	/// </summary>
	public string Label { get; set; } = "Series";
}
