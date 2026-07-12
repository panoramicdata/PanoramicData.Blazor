namespace PanoramicData.Blazor.Models;

/// <summary>
/// Holds the CSS class and tooltip for an icon used in data display components.
/// </summary>
public class IconInfo
{
	/// <summary>Gets or sets the CSS class string that renders the icon (e.g. a Font Awesome class).</summary>
	public string CssCls { get; set; } = string.Empty;

	/// <summary>Gets or sets the tooltip text shown when the user hovers over the icon.</summary>
	public string ToolTip { get; set; } = string.Empty;
}
