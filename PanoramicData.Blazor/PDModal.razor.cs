using System.Collections.Generic;
using Microsoft.JSInterop;
using Microsoft.AspNetCore.Components;

namespace PanoramicData.Blazor
{
	public partial class PDModal
    {
		private static int _sequence;

		[Inject] public IJSRuntime? JSRuntime { get; set; }

		/// <summary>
		/// Sets the title shown in the modal dialog header.
		/// </summary>
		[Parameter] public string Title { get; set; } = string.Empty;

		/// <summary>
		/// Sets the content displayed in the modal dialog body.
		/// </summary>
		[Parameter] public RenderFragment? ChildContent { get; set; }

		/// <summary>
		/// Sets the buttons displayed in the modal dialog footer.
		/// </summary>
		[Parameter] public List<ToolbarItem> Buttons { get; set; } = new List<ToolbarItem>
		{
			new ToolbarButton { Text = "Yes", CssClass = "btn-primary", ShiftRight = true },
			new ToolbarButton { Text = "No" },
		};

		/// <summary>
		/// Event triggered whenever the user clicks on a button.
		/// </summary>
		[Parameter] public EventCallback<string> ButtonClick { get; set; }

		/// <summary>
		/// Display the close button in the top right of the modal?
		/// </summary>
		[Parameter] public bool ShowClose { get; set; }

		/// <summary>
		/// Sets the title shown in the modal dialog header.
		/// </summary>
		[Parameter] public bool CenterVertically { get; set; }

		/// <summary>
		/// Gets the unique identifier of the modal.
		/// </summary>
		public string Id { get; } = $"pd-modal-{++_sequence}";

		/// <summary>
		/// Displays the Modal Dialog.
		/// </summary>
		public void Show()
		{
			JSRuntime.InvokeVoidAsync("showBsDialog", $"#{Id}");
		}

		/// <summary>
		/// Hides the Modal Dialog.
		/// </summary>
		public void Hide()
		{
			JSRuntime.InvokeVoidAsync("hideBsDialog", $"#{Id}");
		}
	}
}
