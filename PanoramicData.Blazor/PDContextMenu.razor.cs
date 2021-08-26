using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.JSInterop;
using PanoramicData.Blazor.Exceptions;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace PanoramicData.Blazor
{
	public partial class PDContextMenu : IDisposable
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
		[Parameter] public EventCallback<MenuItemsEventArgs> UpdateState { get; set; }

		/// <summary>
		/// Gets or sets an event callback delegate fired when the user selects clicks one of the items.
		/// </summary>
		[Parameter] public EventCallback<MenuItem> ItemClick { get; set; }

		/// <summary>
		/// Sets whether the context menu is enabled or disabled.
		/// </summary>
		[Parameter] public bool Enabled { get; set; } = true;

		/// <summary>
		/// Gets the unique identifier of this panel.
		/// </summary>
		public string Id { get; private set; } = string.Empty;

		protected async override Task OnInitializedAsync()
		{
			Id = $"pdcm{++_idSequence}";
			var available = await JSRuntime.InvokeAsync<bool>("panoramicData.hasPopperJs").ConfigureAwait(true);
			if (!available)
			{
				throw new PDContextMenuException($"To use the {nameof(PDContextMenu)} component you must include the popper.js library");
			}
		}

		public async Task ClickHandler(MenuItem item)
		{
			if (!item.IsDisabled)
			{
				await JSRuntime.InvokeVoidAsync("panoramicData.hideMenu", Id).ConfigureAwait(true);
				await ItemClick.InvokeAsync(item).ConfigureAwait(true);
			}
		}

		private async Task OnMouseDown(MouseEventArgs args)
		{
			if (Enabled && args.Button == 2)
			{
				var cancelArgs = new MenuItemsEventArgs(this, Items)
				{
					// get details of element that was clicked on
					SourceElement = await JSRuntime.InvokeAsync<ElementInfo>("panoramicData.getElementAtPoint", args.ClientX, args.ClientY).ConfigureAwait(true)
				};

				await UpdateState.InvokeAsync(cancelArgs).ConfigureAwait(true);
				if (!cancelArgs.Cancel)
				{
					await JSRuntime.InvokeVoidAsync("panoramicData.showMenu", Id, args.ClientX, args.ClientY).ConfigureAwait(true);
				}
			}
		}

		public void Dispose()
		{
			JSRuntime.InvokeVoidAsync("panoramicData.hideMenu", Id);
		}
	}
}
