namespace PanoramicData.Blazor.Models;

public class Selection<TItem>
{
	public bool AllSelected { get; set; }

	public List<TItem> Items { get; set; } = new();

	public override string ToString()
	{
		if (AllSelected)
		{
			return "(All)";
		}
		if (Items.Any())
		{
			return string.Join(", ", Items.Select(x => x?.ToString() ?? "").ToArray());
		}
		return "(None)";
	}
}
