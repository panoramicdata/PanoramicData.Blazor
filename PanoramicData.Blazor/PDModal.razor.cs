using System.Threading.Tasks;
using System.Collections.Generic;
using Microsoft.JSInterop;
using Microsoft.AspNetCore.Components;
using System.Text;

namespace PanoramicData.Blazor
{
	public partial class PDModal
    {
		private static int _sequence;
		private TaskCompletionSource<string>? _userChoice;

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
		/// Sets the size of the modal dialog.
		/// </summary>
		[Parameter] public ModalSizes Size { get; set; } = ModalSizes.Medium;

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
		public async Task ShowAsync()
		{
			await JSRuntime.InvokeVoidAsync("showBsDialog", $"#{Id}").ConfigureAwait(true);
		}

		/// <summary>
		/// Hides the Modal Dialog.
		/// </summary>
		public async Task HideAsync()
		{
			await JSRuntime.InvokeVoidAsync("hideBsDialog", $"#{Id}").ConfigureAwait(true);
		}

		/// <summary>
		/// Displays the Modal Dialog and awaits the users choice.
		/// </summary>
		public async Task<string> ShowAndWaitResultAsync()
		{
			// show dialog and await user choice.
			await ShowAsync().ConfigureAwait(true);
			_userChoice = new TaskCompletionSource<string>();
			var result = await _userChoice.Task.ConfigureAwait(true);
			await HideAsync().ConfigureAwait(true);
			_userChoice = null;
			return result;
		}

		public async Task OnButtonClick(string key)
		{
			// are we waiting for using response?
			if (_userChoice != null)
			{
				_userChoice.SetResult(key);
}
			else
			{
				// forward to calling app
				await ButtonClick.InvokeAsync(key).ConfigureAwait(true);
			}
		}

		private string ModalCssClass
		{
			get
			{

				var sb = new StringBuilder();
				if (Size == ModalSizes.ExtraLarge)
				{
					sb.Append("modal-xl ");
				}
				else if (Size == ModalSizes.Large)
				{
					sb.Append("modal-lg ");
				}
				else if (Size == ModalSizes.Small)
				{
					sb.Append("modal-sm ");
				}
				if (CenterVertically)
				{
					sb.Append("modal-dialog-centered ");
				}
				return sb.ToString().TrimEnd();
			}
		}
	}
}
