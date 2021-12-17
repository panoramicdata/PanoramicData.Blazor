using Microsoft.AspNetCore.Components;
using PanoramicData.Blazor.Models;

namespace PanoramicData.Blazor
{
	public partial class PDMenuItem
	{
		[CascadingParameter(Name = "ToolbarDropdown")]
		public PDToolbarDropdown ToolbarDropdown { get; set; } = null!;

		[Parameter]
		public string Key { get; set; } = string.Empty;

		[Parameter]
		public string Text { get; set; } = string.Empty;

		[Parameter]
		public bool IsVisible { get; set; } = true;

		[Parameter]
		public bool IsDisabled { get; set; }

		[Parameter]
		public string IconCssClass { get; set; } = string.Empty;

		[Parameter]
		public string Content { get; set; } = string.Empty;

		[Parameter]
		public bool IsSeparator { get; set; }

		[Parameter]
		public ShortcutKey ShortcutKey { get; set; } = new ShortcutKey();

		protected override void OnInitialized()
		{
			ToolbarDropdown?.AddMenuItem(this);
		}
	}
}
