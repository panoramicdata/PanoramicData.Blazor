namespace PanoramicData.Blazor;

/// <summary>
/// A Blazor component that provides a validation summary display for form errors.
/// </summary>
public partial class PDValidationSummary
{
		/// <summary>
	/// Gets or sets the collection of validation errors.
	/// </summary>
	[Parameter]
	public object? Errors { get; set; }
}
