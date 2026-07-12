namespace PanoramicData.Blazor.Models;

/// <summary>
/// Specifies how many rows a user can select simultaneously in a <see cref="PanoramicData.Blazor.PDTable{TItem}"/>.
/// </summary>
public enum TableSelectionMode
{
	/// <summary>Row selection is disabled.</summary>
	None,
#pragma warning disable CA1720 // 'Single' is the correct domain term for this selection mode
	/// <summary>Only one row may be selected at a time.</summary>
	Single,
#pragma warning restore CA1720
	/// <summary>Multiple rows may be selected simultaneously.</summary>
	Multiple
}
