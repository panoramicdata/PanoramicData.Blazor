using System;
using Microsoft.AspNetCore.Components.Web;

namespace PanoramicData.Blazor
{
	public class ToolbarTextBox : ToolbarItem
	{
		/// <summary>
		/// Gets or sets CSS classes for the button.
		/// </summary>
		public string CssClass { get; set; } = "";

		/// <summary>
		/// Gets or sets CSS classes for the toolbar item.
		/// </summary>
		public string ItemCssClass { get; set; } = "";

		/// <summary>
		/// Sets the width of the containing div element.
		/// </summary>
		public string Width { get; set; } = "Auto";

		/// <summary>
		/// Sets the initial text value.
		/// </summary>
		public string Value { get; set; } = string.Empty;

		/// <summary>
		/// Event raised whenever the text value changes.
		/// </summary>
		public Action<string>? ValueChanged { get; set; }

		/// <summary>
		/// Event raised whenever a key is pressed.
		/// </summary>
		public Action<KeyboardEventArgs>? Keypress { get; set; }

		/// <summary>
		/// Gets or sets whether the clear button is displayed.
		/// </summary>
		public bool ShowClearButton { get; set; } = true;

		/// <summary>
		/// Event raised when the user clicks on the clear button.
		/// </summary>
		public Action? Cleared { get; set; }

		/// <summary>
		/// Gets or sets an optional label to be displayed before the textbox.
		/// </summary>
		public string Label { get; set; } = string.Empty;

		/// <summary>
		/// Gets or sets the debounce wait period in milliseconds.
		/// </summary>
		public int DebounceWait { get; set; } = 0;
	}
}
