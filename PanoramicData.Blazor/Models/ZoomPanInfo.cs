namespace PanoramicData.Blazor.Models;

public class ZoombarValue
{
	private const int DECIMAL_PLACES = 2;
	private double _pan;
	private double _zoom = 100;

	/// <summary>
	/// Gets or sets the current zoom level as a percentage.
	/// </summary>
	public double Zoom
	{
		get { return _zoom; }
		set
		{
			// restrict zoom percentage to N decimal places
			_zoom = Math.Round(value, DECIMAL_PLACES);
		}
	}

	/// <summary>
	/// Gets or sets the current Pan (offset) as a percentage.
	/// </summary>
	public double Pan
	{
		get { return _pan; }
		set
		{
			// restrict pan percentage to N decimal places
			_pan = Math.Round(value, DECIMAL_PLACES);
		}
	}
}
