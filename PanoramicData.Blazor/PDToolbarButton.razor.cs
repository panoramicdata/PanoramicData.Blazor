using System.Threading.Tasks;
using Microsoft.AspNetCore.Components;

namespace PanoramicData.Blazor
{
	public partial class PDToolbarButton : IToolbarItem
	{
		/// <summary>
		/// Gets or sets the unique identifier.
		/// </summary>
		[Parameter] public string Key { get; set; } = string.Empty;

		/// <summary>
		/// Gets or sets the text displayed on the button.
		/// </summary>
		[Parameter] public string Text { get; set; } = string.Empty;

		/// <summary>
		/// Gets or sets CSS classes for the button.
		/// </summary>
		[Parameter] public string CssClass { get; set; } = "btn-light";

		/// <summary>
		/// Gets or sets CSS classes for an optional icon.
		/// </summary>
		[Parameter] public string IconCssClass { get; set; } = string.Empty;

		/// <summary>
		/// Gets or sets CSS classes for the text.
		/// </summary>
		[Parameter] public string TextCssClass { get; set; } = string.Empty;

		/// <summary>
		/// Gets or sets the tooltip for the toolbar item.
		/// </summary>
		[Parameter] public string ToolTip { get; set; } = string.Empty;

		/// <summary>
		/// Gets or sets whether the toolbar item is visible.
		/// </summary>
		[Parameter] public bool IsVisible { get; set; } = true;

		/// <summary>
		/// Gets or sets whether the toolbar item is enabled.
		/// </summary>
		[Parameter] public bool IsEnabled { get; set; } = true;

		/// <summary>
		/// Gets or sets whether the toolbar item is positioned further to the right of the previous toolbar item.
		/// </summary>
		[Parameter] public bool ShiftRight { get; set; } = false;

		/// <summary>
		/// Event raised whenever user clicks on the button.
		/// </summary>
		[Parameter] public EventCallback<string> Click { get; set; }

		private async Task OnClick()
		{
			await Click.InvokeAsync(Key).ConfigureAwait(true);
		}
	}
}
