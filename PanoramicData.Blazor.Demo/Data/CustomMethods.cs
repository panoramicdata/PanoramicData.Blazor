namespace PanoramicData.Blazor.Demo.Data;

public static class CustomMethods
{
	/// <summary>
	/// Example of a method that uses attributes to provide descriptions
	/// </summary>
	[Display(Description = "Sums up the given values.")]
	public static int Sum(
		[Display(Description = "First value")] int value1,
		[Display(Description = "Second value")] int value2
	)
	{
		return value1 + value2;
	}
}
