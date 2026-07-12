namespace PanoramicData.Blazor;

/// <summary>
/// A Blazor component that acts as a container for drag-and-drop items, tracking the current drag payload and selection.
/// </summary>
/// <typeparam name="TItem">The type of item being dragged.</typeparam>
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

	/// <summary>
	/// Gets or sets the item currently being dragged.
	/// </summary>
	public TItem? Payload { get; set; }

	/// <summary>
	/// Raises the <see cref="SelectionChanged"/> callback with the currently selected items.
	/// </summary>
	public async Task OnSelectionChangedAsync()
	{
		var selection = (from item in Items
						 where item is ISelectable si && si.IsSelected
						 select item).ToArray();
		await SelectionChanged.InvokeAsync(selection);
	}
}
