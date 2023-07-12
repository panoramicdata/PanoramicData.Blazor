namespace PanoramicData.Blazor;

public partial class PDDragContainer<TItem> where TItem : class
{
	[Parameter]
	public RenderFragment ChildContent { get; set; } = null!;

	[Parameter]
	public IEnumerable<TItem> Items { get; set; } = Array.Empty<TItem>();

	public TItem? Payload { get; set; }

}
