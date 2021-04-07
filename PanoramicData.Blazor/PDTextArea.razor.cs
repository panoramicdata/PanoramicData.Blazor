using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.JSInterop;
using System;
using System.Threading.Tasks;

namespace PanoramicData.Blazor
{
	public partial class PDTextArea : IDisposable
	{
		private static int _seq;
		private DotNetObjectReference<PDTextArea>? _objRef;

		/// <summary>
		/// Injected javascript interop object.
		/// </summary>
		[Inject] public IJSRuntime? JSRuntime { get; set; }

		/// <summary>
		/// Gets or sets CSS classes for the text box.
		/// </summary>
		[Parameter] public string CssClass { get; set; } = "";

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
		/// Sets the width of the containing div element.
		/// </summary>
		[Parameter] public string Width { get; set; } = "Auto";

		/// <summary>
		/// Sets the maximum length of the input.
		/// </summary>
		[Parameter] public int MaxLength { get; set; } = -1;

		/// <summary>
		/// Gets or sets placeholder text for the text box.
		/// </summary>
		[Parameter] public string Placeholder { get; set; } = string.Empty;

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
		/// Sets the number of rows displayed.
		/// </summary>
		[Parameter] public int Rows { get; set; } = 5;

		/// <summary>
		/// Gets or sets whether the clear button is displayed.
		/// </summary>
		[Parameter] public bool ShowClearButton { get; set; } = true;

		/// <summary>
		/// Sets the debounce wait period in milliseconds.
		/// </summary>
		[Parameter] public int DebounceWait { get; set; } = 0;

		/// <summary>
		/// Event raised when the user clicks on the clear button.
		/// </summary>
		[Parameter] public EventCallback Cleared { get; set; }

		public string Id { get; set; } = $"pd-textarea-{++_seq}";

		protected override async Task OnAfterRenderAsync(bool firstRender)
		{
			if (firstRender && DebounceWait > 0)
			{
				_objRef = DotNetObjectReference.Create(this);
				await JSRuntime.InvokeVoidAsync("panoramicData.debounceInput", Id, DebounceWait, _objRef).ConfigureAwait(true);
			}
		}

		private async Task OnInput(ChangeEventArgs args)
		{
			if (DebounceWait <= 0)
			{
				Value = args.Value.ToString();
				await ValueChanged.InvokeAsync(args.Value.ToString()).ConfigureAwait(true);
			}
		}

		[JSInvokable]
		public async Task OnDebouncedInput(string value)
		{
			Value = value;
			await ValueChanged.InvokeAsync(value).ConfigureAwait(true);
		}

		private async Task OnKeypress(KeyboardEventArgs args)
		{
			await Keypress.InvokeAsync(args).ConfigureAwait(true);
		}

		public void Dispose()
		{
			_objRef?.Dispose();
		}
	}
}
