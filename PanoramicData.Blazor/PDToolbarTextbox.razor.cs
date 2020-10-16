using System.Threading.Tasks;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;

namespace PanoramicData.Blazor
{
    public partial class PDToolbarTextbox
    {
		/// <summary>
		/// Sets the width of the containing div element.
		/// </summary>
		[Parameter] public string Width { get; set; } = "Auto";

		/// <summary>
		/// Sets additional CSS classes for the textbox.
		/// </summary>
		[Parameter] public string CssClass { get; set; } = "btn-light";

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
		/// Gets the style for the containing div element.
		/// </summary>
		public string ItemStyle => $"width: {Width}";

		private async Task OnInput(ChangeEventArgs args)
		{
			await ValueChanged.InvokeAsync(args.Value.ToString()).ConfigureAwait(true);
		}

		private async Task OnKeypress(KeyboardEventArgs args)
		{
			await Keypress.InvokeAsync(args).ConfigureAwait(true);
		}
	}
}
