namespace PanoramicData.Blazor.Models;

/// <summary>
/// Specifies whether and how a preview panel is displayed in the file explorer.
/// </summary>
public enum FilePreviewModes
{
	/// <summary>
	/// No preview.
	/// </summary>
	Off,
	/// <summary>
	/// Preview always visible.
	/// </summary>
	On,
	/// <summary>
	/// Optional preview, defaulting to visible.
	/// </summary>
	OptionalOn,
	/// <summary>
	/// Optional preview, defaulting to hidden.
	/// </summary>
	OptionalOff
}
