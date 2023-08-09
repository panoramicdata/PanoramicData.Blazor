namespace PanoramicData.Blazor.Arguments;

public class SelectionArgs<TItem>
{
	public bool AllSelected { get; set; }

	public IEnumerable<TItem> SelectedItems { get; set; } = Array.Empty<TItem>();

	public override string ToString()
	{
		if (AllSelected)
		{
			return "(All)";
		}
		if (SelectedItems.Any())
		{
			return string.Join(", ", SelectedItems.Select(x => x?.ToString() ?? "").ToArray());
		}
		return "(None)";
	}
}
