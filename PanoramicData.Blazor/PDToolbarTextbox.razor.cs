using System.Threading.Tasks;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;

namespace PanoramicData.Blazor
{
    public partial class PDToolbarTextbox
    {
		/// <summary>
		/// Gets or sets the unique identifier.
		/// </summary>
		[Parameter] public string Key { get; set; } = string.Empty;

		/// <summary>
		/// Gets or sets CSS classes for the button.
		/// </summary>
		[Parameter] public string CssClass { get; set; } = "";

		/// <summary>
		/// Gets or sets CSS classes for the toolbar item.
		/// </summary>
		[Parameter] public string ItemCssClass { get; set; } = "";

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
		/// Sets the width of the containing div element.
		/// </summary>
		[Parameter] public string Width { get; set; } = "Auto";

		/// <summary>
		/// Sets the initial text value.
		/// </summary>
		[Parameter] public string Value { get; set; } = string.Empty;

		/// <summary>
		/// Event raised whenever the text value changes.
		/// </summary>
		[Parameter] public EventCallback<string> ValueChanged { get; set; }

		/// <summary>
		/// Event raised whenever a key is pressed.
		/// </summary>
		[Parameter] public EventCallback<KeyboardEventArgs> Keypress { get; set; }

		/// <summary>
		/// Gets or sets whether the clear button is displayed.
		/// </summary>
		[Parameter] public bool ShowClearButton { get; set; } = true;

		/// <summary>
		/// Event raised when the user clicks on the clear button.
		/// </summary>
		[Parameter] public EventCallback Cleared { get; set; }

		public string ItemStyle => $"width: {Width}";

		public string InputCssClass => ShowClearButton ? "pr-4" : "";

		private string _value = string.Empty;

		private async Task OnInput(ChangeEventArgs args)
		{
			_value = args.Value.ToString();
			await ValueChanged.InvokeAsync(_value).ConfigureAwait(true);
		}

		private async Task OnKeypress(KeyboardEventArgs args)
		{
			await Keypress.InvokeAsync(args).ConfigureAwait(true);
		}

		private async Task OnClear(MouseEventArgs _)
		{
			_value = string.Empty;
			await ValueChanged.InvokeAsync(string.Empty).ConfigureAwait(true);
			await Cleared.InvokeAsync(null).ConfigureAwait(true);
		}
	}
}
