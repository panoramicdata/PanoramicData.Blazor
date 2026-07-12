namespace PanoramicData.Blazor;

/// <summary>
/// A Blazor component that displays a country flag image based on an ISO 3166-1 alpha-2 country code.
/// </summary>
public partial class PDFlag
{
	/// <summary>
	/// Gets or sets the two-letter ISO country code for the flag to be displayed.
	/// </summary>
	[EditorRequired]
	[Parameter]
	public required string CountryCode { get; set; } = string.Empty;

	/// <summary>
	/// Gets or sets the width of the flag.
	/// </summary>
	[Parameter]
	public string Width { get; set; } = "2em";
}