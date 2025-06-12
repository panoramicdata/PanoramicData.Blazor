namespace PanoramicData.Blazor.Models;

public class NumericRange
{
	public NumericRange()
	{
	}

	public NumericRange(double start, double end)
	{
		Start = start;
		End = end;
	}

	public double Start { get; set; }

	public double End { get; set; }
}
