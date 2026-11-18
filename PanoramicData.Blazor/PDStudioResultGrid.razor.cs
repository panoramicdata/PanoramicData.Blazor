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

	/// <summary>
	/// Event callback for when the user clicks a row, or presses Enter on one. While it is bound, rows show a
	/// pointer and a hover highlight and can be focused.
	/// </summary>
	[Parameter] public EventCallback<PDStudioResultRowClickEventArgs> OnRowClick { get; set; }

	// Each row with its index in ResultSet.Rows, which is what a click reports whatever the sort order.
	private IEnumerable<(int Index, object?[] Row)> SortedRows
	{
		get
		{
			var rows = ResultSet.Rows.Select((row, index) => (index, row));
			if (_sortColumn is not int column)
			{
				return rows;
			}

			return _sortDescending
				? rows.OrderByDescending(r => r.row[column], ValueComparer.Instance)
				: rows.OrderBy(r => r.row[column], ValueComparer.Instance);
		}
	}

	// Null when nothing is bound, so an unbound row renders exactly as a plain <tr>.
	private Dictionary<string, object>? RowAttributes(int index, object?[] row)
	{
		if (!OnRowClick.HasDelegate)
		{
			return null;
		}

		return new()
		{
			["class"] = "pd-studio-grid-clickable",
			["tabindex"] = "0",
			["onclick"] = EventCallback.Factory.Create<MouseEventArgs>(this, () => RaiseRowClickAsync(index, row)),
			["onkeydown"] = EventCallback.Factory.Create<KeyboardEventArgs>(this, e => OnRowKeyDownAsync(e, index, row))
		};
	}

	// Plain Enter only: Ctrl+Enter is PDStudio's Execute shortcut.
	private Task OnRowKeyDownAsync(KeyboardEventArgs e, int index, object?[] row)
		=> e is { Key: "Enter", CtrlKey: false, AltKey: false, ShiftKey: false, MetaKey: false, Repeat: false }
			? RaiseRowClickAsync(index, row)
			: Task.CompletedTask;

	private Task RaiseRowClickAsync(int index, object?[] row)
		=> OnRowClick.InvokeAsync(new PDStudioResultRowClickEventArgs(index, row, ResultSet.Columns));

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
