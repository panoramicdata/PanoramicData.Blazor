using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;

namespace PanoramicData.Blazor
{
	public partial class PDLabel
	{
		[Parameter]
		public EventCallback<MouseEventArgs> Click { get; set; }

		[Parameter]
		public string CssClass { get; set; } = string.Empty;

		[Parameter]
		public string IconCssClass { get; set; } = string.Empty;

		[Parameter]
		public bool PreventDefault { get; set; }

		[Parameter]
		public bool StopPropagation { get; set; }

		[Parameter]
		public string Text { get; set; } = string.Empty;

		[Parameter]
		public string TextCssClass { get; set; } = string.Empty;

		[Parameter]
		public string ToolTip { get; set; } = string.Empty;
	}
}
