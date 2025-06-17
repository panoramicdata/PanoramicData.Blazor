namespace PanoramicData.Blazor.Models;

public class FilterOptions
{
	public bool AllowContains { get; set; } = true;
	public bool AllowDoesNotContain { get; set; } = true;
	public bool AllowDoesNotEqual { get; set; } = true;
	public bool AllowEndsWith { get; set; } = true;
	public bool AllowEquals { get; set; } = true;
	public bool AllowStartsWith { get; set; } = true;
	public bool AllowIn { get; set; } = true;
	public bool AllowNotIn { get; set; } = true;
	public bool AllowGreaterThan { get; set; } = true;
	public bool AllowGreaterThanOrEqual { get; set; } = true;
	public bool AllowLessThanOrEqual { get; set; } = true;
	public bool AllowLessThan { get; set; } = true;
	public bool AllowRange { get; set; } = true;
	public bool AllowIsNull { get; set; } = true;
	public bool AllowIsNotNull { get; set; } = true;
	public bool AllowIsEmpty { get; set; } = true;
	public bool AllowIsNotEmpty { get; set; } = true;

	public static FilterOptions SingleValue()
	{
		return new FilterOptions
		{
			AllowContains = false,
			AllowDoesNotContain = false,
			AllowDoesNotEqual = false,
			AllowEndsWith = false,
			AllowEquals = true,
			AllowStartsWith = false,
			AllowIn = false,
			AllowNotIn = false,
			AllowGreaterThan = false,
			AllowGreaterThanOrEqual = false,
			AllowLessThanOrEqual = false,
			AllowLessThan = false,
			AllowRange = false,
			AllowIsNull = false,
			AllowIsNotNull = false,
			AllowIsEmpty = false,
			AllowIsNotEmpty = false
		};
	}
}
