namespace PanoramicData.Blazor.Models;

/// <summary>
/// An enumeration of possible Help Text display modes.
/// </summary>
public enum HelpTextMode
{
	/// <summary>Help text is always hidden.</summary>
	Hidden,
	/// <summary>Help text is always visible.</summary>
	Shown,
	/// <summary>Help text visibility can be toggled by the user.</summary>
	Toggle
}
