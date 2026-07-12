namespace PanoramicData.Blazor.Models;

/// <summary>
/// Represents a closed numeric range defined by a start and an end value.
/// </summary>
public class NumericRange
{
	/// <summary>
	/// Initializes a new instance of <see cref="NumericRange"/> with both values set to 0.
	/// </summary>
	public NumericRange()
	{
	}

	/// <summary>
	/// Initializes a new instance of <see cref="NumericRange"/> with the given start and end values.
	/// </summary>
	/// <param name="start">The lower bound of the range.</param>
	/// <param name="end">The upper bound of the range.</param>
	public NumericRange(double start, double end)
	{
		Start = start;
		End = end;
	}

	/// <summary>Gets or sets the lower bound of the range.</summary>
	public double Start { get; set; }

	/// <summary>Gets or sets the upper bound of the range.</summary>
	public double End { get; set; }
}
