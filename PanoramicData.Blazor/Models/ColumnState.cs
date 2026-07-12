namespace PanoramicData.Blazor.Models;

/// <summary>
/// Records the persisted display state of a single column in a <see cref="PanoramicData.Blazor.PDTable{TItem}"/>.
/// </summary>
public class ColumnState
{
	/// <summary>Gets or sets the display order of this column relative to other columns. A higher value places the column further to the right. Defaults to 1000.</summary>
	public int Ordinal { get; set; } = 1000;

	/// <summary>Gets or sets a value indicating whether this column is visible. Defaults to <c>true</c>.</summary>
	public bool Visible { get; set; } = true;
}
