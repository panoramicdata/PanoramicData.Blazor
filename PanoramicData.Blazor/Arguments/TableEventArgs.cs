namespace PanoramicData.Blazor.Arguments;

/// <summary>
/// The TableEventArgs class holds details on a node event.
/// </summary>
/// <remarks>
/// Initializes a new instance of the TableEventArgs class.
/// </remarks>
/// <param name="item">The item the event relates to.</param>
public class TableEventArgs<TItem>(TItem item) where TItem : class
{
	/// <summary>
	/// Gets the item the event relates to.
	/// </summary>
	public TItem Item { get; } = item;
}

/// <summary>
/// The TableCancelEventArgs class allows an event to be canceled.
/// </summary>
/// <remarks>
/// Initializes a new instance of the TableCancelEventArgs class.
/// </remarks>
/// <param name="node">The node the event relates to.</param>
public class TableCancelEventArgs<TItem>(TItem item) : TableEventArgs<TItem>(item) where TItem : class
{
	/// <summary>
	/// Gets or sets whether the operation attempted should be canceled.
	/// </summary>
	public bool Cancel { get; set; }
}

/// <summary>
/// The TableBeforeEditEventArgs class allows a pending edit to be canceled.
/// </summary>
/// <remarks>
/// Initializes a new instance of the TableBeforeEditEventArgs class.
/// </remarks>
/// <param item="">The item the event relates to.</param>
public class TableBeforeEditEventArgs<TItem>(TItem item) : TableCancelEventArgs<TItem>(item) where TItem : class
{
	/// <summary>
	/// Gets or sets the start of the selected text to be edited.
	/// </summary>
	public int SelectionStart { get; set; }

	/// <summary>
	/// Gets or sets the end of the selected text to be edited.
	/// </summary>
	public int SelectionEnd { get; set; }
}

/// <summary>
/// The TableAfterEditEventArgs class provides details of a completed edit and allows for cancellation.
/// </summary>
/// <remarks>
/// Initializes a new instance of the TableBeforeEditEventArgs class.
/// </remarks>
/// <param name="item">The original item values the event relates to.</param>
public class TableAfterEditEventArgs<TItem>(TItem item) : TableCancelEventArgs<TItem>(item) where TItem : class
{
	/// <summary>
	/// Gets or sets the new values.
	/// </summary>
	public Dictionary<string, object?> NewValues { get; set; } = [];

	public override string ToString()
	{
		var sb = new StringBuilder();
		foreach (var kvp in NewValues)
		{
			if (sb.Length > 0)
			{
				sb.Append(", ");
			}

			string valueStr = kvp.Value?.ToString() ?? "(null)";
			sb.Append(kvp.Key).Append(" = ").Append(valueStr);
		}

		return sb.ToString();
	}
}

/// <summary>
/// The TableAfterEditEventArgs class provides details of a completed edit and allows for cancellation.
/// </summary>
/// <remarks>
/// Initializes a new instance of the TableBeforeEditEventArgs class.
/// </remarks>
/// <param name="item">The original item values the event relates to.</param>
public class TableAfterEditCommittedEventArgs<TItem>(TItem item) : TableEventArgs<TItem>(item) where TItem : class
{
	/// <summary>
	/// Gets or sets the new values.
	/// </summary>
	public Dictionary<string, object?> NewValues { get; set; } = [];

	public override string ToString()
	{
		var sb = new StringBuilder();
		foreach (var kvp in NewValues)
		{
			if (sb.Length > 0)
			{
				sb.Append(", ");
			}

			string valueStr = kvp.Value?.ToString() ?? "(null)";
			sb.Append(kvp.Key).Append(" = ").Append(valueStr);
		}

		return sb.ToString();
	}
}

/// <summary>
/// The TableSelectionEventArgs class holds data for an action that is performed on a selection of items.
/// </summary>
/// <remarks>
/// Initializes a new instance of the TableSelectionEventArgs class.
/// </remarks>
/// <param name="items">The items the event relates to.</param>
public class TableSelectionEventArgs<TItem>(TItem[] items) where TItem : class
{
	/// <summary>
	/// Gets the items the event relates to.
	/// </summary>
	public TItem[] Items { get; } = items;
}
