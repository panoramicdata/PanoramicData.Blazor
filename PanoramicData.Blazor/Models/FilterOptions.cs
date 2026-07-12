namespace PanoramicData.Blazor.Models;

/// <summary>
/// Controls which filter types are available to the user in filter UI components.
/// </summary>
public class FilterOptions
{
	/// <summary>Gets or sets whether the Contains filter type is available.</summary>
	public bool AllowContains { get; set; } = true;
	/// <summary>Gets or sets whether the Does Not Contain filter type is available.</summary>
	public bool AllowDoesNotContain { get; set; } = true;
	/// <summary>Gets or sets whether the Does Not Equal filter type is available.</summary>
	public bool AllowDoesNotEqual { get; set; } = true;
	/// <summary>Gets or sets whether the Ends With filter type is available.</summary>
	public bool AllowEndsWith { get; set; } = true;
	/// <summary>Gets or sets whether the Equals filter type is available.</summary>
	public bool AllowEquals { get; set; } = true;
	/// <summary>Gets or sets whether the Starts With filter type is available.</summary>
	public bool AllowStartsWith { get; set; } = true;
	/// <summary>Gets or sets whether the In filter type is available.</summary>
	public bool AllowIn { get; set; } = true;
	/// <summary>Gets or sets whether the Not In filter type is available.</summary>
	public bool AllowNotIn { get; set; } = true;
	/// <summary>Gets or sets whether the Greater Than filter type is available.</summary>
	public bool AllowGreaterThan { get; set; } = true;
	/// <summary>Gets or sets whether the Greater Than Or Equal filter type is available.</summary>
	public bool AllowGreaterThanOrEqual { get; set; } = true;
	/// <summary>Gets or sets whether the Less Than Or Equal filter type is available.</summary>
	public bool AllowLessThanOrEqual { get; set; } = true;
	/// <summary>Gets or sets whether the Less Than filter type is available.</summary>
	public bool AllowLessThan { get; set; } = true;
	/// <summary>Gets or sets whether the Range filter type is available.</summary>
	public bool AllowRange { get; set; } = true;
	/// <summary>Gets or sets whether the Is Null filter type is available.</summary>
	public bool AllowIsNull { get; set; } = true;
	/// <summary>Gets or sets whether the Is Not Null filter type is available.</summary>
	public bool AllowIsNotNull { get; set; } = true;
	/// <summary>Gets or sets whether the Is Empty filter type is available.</summary>
	public bool AllowIsEmpty { get; set; } = true;
	/// <summary>Gets or sets whether the Is Not Empty filter type is available.</summary>
	public bool AllowIsNotEmpty { get; set; } = true;

	/// <summary>
	/// Returns a <see cref="FilterOptions"/> instance that allows only the Equals filter type.
	/// </summary>
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
