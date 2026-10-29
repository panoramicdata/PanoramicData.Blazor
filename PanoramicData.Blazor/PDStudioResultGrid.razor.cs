namespace PanoramicData.Blazor;

/// <summary>
/// Shows a <see cref="PDStudioResultSet"/> as a table whose columns sort when their heading is clicked.
/// </summary>
public partial class PDStudioResultGrid : PDComponentBase
{
	private int? _sortColumn;
	private bool _sortDescending;

	/// <summary>
	/// Gets or sets the results to show.
	/// </summary>
	[Parameter, EditorRequired] public PDStudioResultSet ResultSet { get; set; } = new();

	private IEnumerable<object?[]> SortedRows
	{
		get
		{
			if (_sortColumn is not int column)
			{
				return ResultSet.Rows;
			}

			return _sortDescending
				? ResultSet.Rows.OrderByDescending(r => r[column], ValueComparer.Instance)
				: ResultSet.Rows.OrderBy(r => r[column], ValueComparer.Instance);
		}
	}

	/// <inheritdoc />
	protected override void OnParametersSet()
	{
		if (_sortColumn >= ResultSet.Columns.Count)
		{
			_sortColumn = null;
		}
	}

	// Ascending, then descending, then back to the original order.
	private void SortBy(int column)
	{
		if (_sortColumn != column)
		{
			_sortColumn = column;
			_sortDescending = false;
		}
		else if (!_sortDescending)
		{
			_sortDescending = true;
		}
		else
		{
			_sortColumn = null;
		}
	}

	private string SortMark(int column)
		=> _sortColumn != column ? string.Empty : _sortDescending ? " ▼" : " ▲";

	private string AriaSort(int column)
		=> _sortColumn != column ? "none" : _sortDescending ? "descending" : "ascending";

	// NULLs first; values of one comparable type compare natively, anything else by its shown text.
	private sealed class ValueComparer : IComparer<object?>
	{
		public static readonly ValueComparer Instance = new();

		public int Compare(object? x, object? y)
		{
			var xNull = x is null or DBNull;
			var yNull = y is null or DBNull;
			if (xNull || yNull)
			{
				return xNull.CompareTo(yNull) * -1;
			}

			if (x!.GetType() == y!.GetType() && x is IComparable comparable)
			{
				return comparable.CompareTo(y);
			}

			return string.Compare(PDStudioResultSet.FormatValue(x), PDStudioResultSet.FormatValue(y), StringComparison.OrdinalIgnoreCase);
		}
	}
}
