namespace PanoramicData.Blazor.Models;

/// <summary>
/// Holds the persisted display state for all columns in a <see cref="PanoramicData.Blazor.PDTable{TItem}"/>.
/// </summary>
public class TableState
{
	/// <summary>Gets or sets a dictionary mapping column keys to their persisted <see cref="ColumnState"/>.</summary>
	public IDictionary<string, ColumnState> Columns { get; set; } = new Dictionary<string, ColumnState>();
}
