namespace PanoramicData.Blazor.Models;

/// <summary>
/// Describes a result row the user clicked, or pressed Enter on, in a <see cref="PDStudio"/> result grid.
/// </summary>
/// <param name="rowIndex">The row's index in <see cref="PDStudioResultSet.Rows"/>.</param>
/// <param name="values">The row's values, one per column.</param>
/// <param name="columns">The result set's columns, in order.</param>
public class PDStudioResultRowClickEventArgs(int rowIndex, IReadOnlyList<object?> values, IReadOnlyList<PDStudioResultColumn> columns)
{
	/// <summary>Gets the row's index in <see cref="PDStudioResultSet.Rows"/>, whatever order the grid is sorted in.</summary>
	public int RowIndex { get; } = rowIndex;

	/// <summary>Gets the row's values, one per column; a null value is a database NULL.</summary>
	public IReadOnlyList<object?> Values { get; } = values;

	/// <summary>Gets the result set's columns, in order.</summary>
	public IReadOnlyList<PDStudioResultColumn> Columns { get; } = columns;

	/// <summary>
	/// Gets the row's value in the named column, matching the name without regard to case.
	/// </summary>
	/// <exception cref="ArgumentException">The result set has no column of that name.</exception>
	public object? GetValue(string columnName)
	{
		for (var i = 0; i < Columns.Count; i++)
		{
			if (string.Equals(Columns[i].Name, columnName, StringComparison.OrdinalIgnoreCase))
			{
				return Values[i];
			}
		}

		throw new ArgumentException($"The result set has no column named '{columnName}'.", nameof(columnName));
	}
}
