using Microsoft.AspNetCore.Components;

namespace PanoramicData.Blazor
{
	public partial class PDBusyOverlay
	{
		[Parameter]
		public bool IsBusy { get; set; }

		[Parameter]
		public RenderFragment? ChildContent { get; set; }

		private string GetCssClass() => $"busy-overlay {(IsBusy ? "show" : "hide")}";
	}
}