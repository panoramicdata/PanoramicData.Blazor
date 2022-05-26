namespace PanoramicData.Blazor.Models;

public class ZoomBarOptions
{
	private double[] _zoomSteps = new double[] { 10, 20, 30, 40, 50, 60, 70, 80, 90, 100 };

	public ZoomBarColours Colours { get; set; } = new ZoomBarColours();

	public double[] ZoomSteps
	{
		get { return _zoomSteps; }
		set
		{
			// ensure zoom steps are in ascending order
			_zoomSteps = value.OrderBy(x => x).ToArray();
		}
	}
}

public class ZoomBarColours
{
	public string Background { get; set; } = "White";
	public string Border { get; set; } = "Silver";
	public string HandleBackground { get; set; } = "Green";
	public string HandleForeground { get; set; } = "White";
}
