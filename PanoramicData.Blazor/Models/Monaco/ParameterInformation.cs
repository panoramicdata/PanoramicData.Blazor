namespace PanoramicData.Blazor.Models.Monaco;

/// <summary>
/// Holds display information for a single parameter within a Monaco editor signature-help item.
/// </summary>
public class ParameterInformation
{
	/// <summary>Gets or sets the documentation text for this parameter.</summary>
	public string Documentation { get; set; } = string.Empty;

	/// <summary>Gets or sets the label text used to identify the parameter in the signature-help UI.</summary>
	public string Label { get; set; } = string.Empty;
}
