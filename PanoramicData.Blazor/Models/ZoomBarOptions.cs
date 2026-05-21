namespace PanoramicData.Blazor.Models;

/// <summary>
/// Options for configuring zoom bar appearance and step values.
/// </summary>
public class ZoomBarOptions
{
	private double[] _zoomSteps = [10, 20, 30, 40, 50, 60, 70, 80, 90, 100];

	/// <summary>
	/// Gets or sets the zoom bar colour palette.
	/// </summary>
	public ZoomBarColours Colours { get; set; } = new ZoomBarColours();

	/// <summary>
	/// Gets or sets available zoom step percentages in ascending order.
	/// </summary>
	public double[] ZoomSteps
	{
		get { return _zoomSteps; }
		set
		{
			// ensure zoom steps are in ascending order
			_zoomSteps = [.. value.OrderBy(x => x)];
		}
	}
}

/// <summary>
/// Colour palette used by <see cref="ZoomBarOptions"/>.
/// </summary>
public class ZoomBarColours
{
	/// <summary>
	/// Gets or sets the background colour.
	/// </summary>
	public string Background { get; set; } = "White";
	/// <summary>
	/// Gets or sets the border colour.
	/// </summary>
	public string Border { get; set; } = "Silver";
	/// <summary>
	/// Gets or sets the handle background colour.
	/// </summary>
	public string HandleBackground { get; set; } = "Green";
	/// <summary>
	/// Gets or sets the handle foreground colour.
	/// </summary>
	public string HandleForeground { get; set; } = "White";
}
