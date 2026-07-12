namespace PanoramicData.Blazor.Models;

/// <summary>
/// Represents a set of selected items, supporting both individual selection and "select all" semantics.
/// </summary>
/// <typeparam name="TItem">The type of item that can be selected.</typeparam>
public class Selection<TItem>
{
	/// <summary>Gets or sets a value indicating that all available items are selected, regardless of the contents of <see cref="Items"/>.</summary>
	public bool AllSelected { get; set; }

	/// <summary>Gets or sets the explicitly selected items. Ignored when <see cref="AllSelected"/> is <c>true</c>.</summary>
	public List<TItem> Items { get; set; } = [];

	/// <summary>
	/// Returns a string representation of the current selection: <c>"(All)"</c> when all items are selected, a comma-separated list of selected item strings, or <c>"(None)"</c> when nothing is selected.
	/// </summary>
	/// <returns>A human-readable description of the selection.</returns>
	public override string ToString()
	{
		if (AllSelected)
		{
			return "(All)";
		}

		if (Items.Count != 0)
		{
			return string.Join(", ", Items.Select(x => x?.ToString() ?? "").ToArray());
		}

		return "(None)";
	}
}
