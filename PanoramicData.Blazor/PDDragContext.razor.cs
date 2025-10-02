namespace PanoramicData.Blazor;

public partial class PDDragContext
{
		/// <summary>
	/// Gets or sets the child content of the component.
	/// </summary>
	[Parameter] public RenderFragment ChildContent { get; set; } = null!;

	/// <summary>
	/// Gets or sets the current data being dragged.
	/// </summary>
	public object? Payload { get; set; }
}
