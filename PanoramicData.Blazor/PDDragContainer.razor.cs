namespace PanoramicData.Blazor;

public partial class PDDragContainer<TItem> where TItem : class
{
	[Parameter]
	public RenderFragment ChildContent { get; set; } = null!;

	[Parameter]
	public IEnumerable<TItem> Items { get; set; } = Array.Empty<TItem>();

	[Parameter]
	public EventCallback<IEnumerable<TItem>> SelectionChanged { get; set; }

	public TItem? Payload { get; set; }

	public async Task OnSelectionChangedAsync()
	{
		var selection = (from item in Items
						 where item is ISelectable si && si.IsSelected
						 select item).ToArray();
		await SelectionChanged.InvokeAsync(selection);
	}
}
