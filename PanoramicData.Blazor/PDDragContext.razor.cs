using Microsoft.AspNetCore.Components;

namespace PanoramicData.Blazor
{
	public partial class PDDragContext
    {
		[Parameter] public RenderFragment ChildContent { get; set; } = null!;

		public object? Payload { get; set; }
	}
}
