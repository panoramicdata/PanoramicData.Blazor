namespace PanoramicData.Blazor;

public partial class PDDragContainer<TItem> where TItem : class
{
	[Parameter]
	public RenderFragment ChildContent { get; set; } = null!;

	[Parameter]
	public IEnumerable<TItem> Items { get; set; } = Array.Empty<TItem>();

	[Parameter]
	public EventCallback<TItem> ItemReOrdered { get; set; }

	public TItem? Payload { get; set; }

	public async Task ItemReOrderedAsync(TItem item)
	{
		await ItemReOrdered.InvokeAsync(item);
	}
}
