namespace PanoramicData.Blazor;

public partial class PDDragContainer<TItem> where TItem : class
{
	/// <summary>
	/// Gets or sets the child content of the component.
	/// </summary>
	[Parameter]
	public RenderFragment ChildContent { get; set; } = null!;

	/// <summary>
	/// Gets or sets the collection of items in the container.
	/// </summary>
	[Parameter]
	public IEnumerable<TItem> Items { get; set; } = [];

	/// <summary>
	/// An event callback that is invoked when the selection changes.
	/// </summary>
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
