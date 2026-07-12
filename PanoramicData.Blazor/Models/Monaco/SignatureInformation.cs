namespace PanoramicData.Blazor.Models.Monaco;

/// <summary>
/// Holds display information for a single method overload shown in the Monaco editor signature-help popup.
/// </summary>
public class SignatureInformation
{
	/// <summary>Gets or sets the zero-based index of the currently active parameter within this signature, or <c>null</c> when no parameter is active.</summary>
	public int? ActiveParameter { get; set; }

	/// <summary>Gets or sets the documentation text for this overload.</summary>
	public string Documentation { get; set; } = string.Empty;

	/// <summary>Gets or sets the formatted signature label displayed in the popup header.</summary>
	public string Label { get; set; } = string.Empty;

	/// <summary>Gets or sets the parameter information for each argument in this overload.</summary>
	public ParameterInformation[] Parameters { get; set; } = [];
}


