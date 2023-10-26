namespace PanoramicData.Blazor.Models;

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
