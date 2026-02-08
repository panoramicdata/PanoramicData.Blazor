namespace PanoramicData.Blazor.Models.Tiles;

/// <summary>
/// Defines the connection mode for tile connectors.
/// </summary>
public enum ConnectionMode
{
	/// <summary>
	/// Straight line connections for adjacent tiles and diagonals only.
	/// </summary>
	StraightLine,

	/// <summary>
	/// Bezier curve connections between adjacent rows (row difference of 1).
	/// Can span any column distance within the adjacent row.
	/// </summary>
	RowCurves,

	/// <summary>
	/// Bezier curve connections between adjacent columns (column difference of 1).
	/// Can span any row distance within the adjacent column.
	/// </summary>
	ColumnCurves
}
