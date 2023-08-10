namespace PanoramicData.Blazor.Arguments;

public class SelectionArgs<TItem>
{
	public Selection<TItem> Selection { get; set; } = new();
}
