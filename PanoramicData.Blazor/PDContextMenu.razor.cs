using System.ComponentModel;
using System.Threading.Tasks;
using System.Collections.Generic;
using Microsoft.JSInterop;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;
using PanoramicData.Blazor.Exceptions;

namespace PanoramicData.Blazor
{
	public partial class PDContextMenu
    {
		private static int _idSequence;

		[Inject] public IJSRuntime? JSRuntime { get; set; }

		/// <summary>
		/// Gets or sets the menu items to be displayed in the context menu.
		/// </summary>
		[Parameter] public List<MenuItem> Items { get; set; } = new List<MenuItem>();

		/// <summary>
		/// Gets or sets the child content that the COntextMenu wraps.
		/// </summary>
		[Parameter] public RenderFragment? ChildContent { get; set; }

		/// <summary>
		/// Gets or sets an event that is raised just prior to the context menu being shown and allowing
		/// the application to refresh the state of the items.
		/// </summary>
		[Parameter] public EventCallback<CancelEventArgs> UpdateState { get; set; }

		/// <summary>
		/// Gets or sets an event callback delegate fired when the user selects clicks one of the items.
		/// </summary>
		[Parameter] public EventCallback<MenuItem> ItemClick { get; set; }

		/// <summary>
		/// Gets the unique identifier of this panel.
		/// </summary>
		public string Id { get; private set; } = string.Empty;

		protected async override Task OnInitializedAsync()
		{
			Id = $"pdcm{++_idSequence}";
			var available = await JSRuntime.InvokeAsync<bool>("hasPopperJs").ConfigureAwait(true);
			if (!available)
			{
				throw new PDContextMenuException($"To use the {nameof(PDContextMenu)} component you must include the popper.js library");
			}
		}

		public async Task ClickHandler(MenuItem item)
		{
			if(!item.IsDisabled)
			{
				await JSRuntime.InvokeVoidAsync("hideMenu", Id).ConfigureAwait(true);
				await ItemClick.InvokeAsync(item).ConfigureAwait(true);
			}
		}

		private async Task ShowMenuHandler(MouseEventArgs args)
		{
			if (args.Button == 2)
			{
				var cancelArgs = new CancelEventArgs();
				await UpdateState.InvokeAsync(cancelArgs).ConfigureAwait(true);
				if (!cancelArgs.Cancel)
				{
					await JSRuntime.InvokeVoidAsync("showMenu", Id, args.ClientX, args.ClientY).ConfigureAwait(true);
				}
			}
		}
	}
}
