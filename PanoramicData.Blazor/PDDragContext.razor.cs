using Microsoft.AspNetCore.Components;

namespace PanoramicData.Blazor
{
	public partial class PDDragContext
	{
		[Parameter] public RenderFragment ChildContent { get; set; } = null!;

		/// <summary>
		/// Gets or sets the current data being dragged.
		/// </summary>
		public object? Payload { get; set; }
	}
}
