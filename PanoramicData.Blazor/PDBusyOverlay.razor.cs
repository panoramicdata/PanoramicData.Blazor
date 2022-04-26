using Microsoft.AspNetCore.Components;

namespace PanoramicData.Blazor
{
	public partial class PDBusyOverlay
	{
		[Parameter]
		public bool IsBusy { get; set; }

		[Parameter]
		public RenderFragment? ChildContent { get; set; }

		[Parameter]
		public string OverlayCssClass { get; set; } = "fas fa-4x fa-spin fa-circle-notch";

		[Parameter]
		public RenderFragment? OverlayContent { get; set; }
	}
}