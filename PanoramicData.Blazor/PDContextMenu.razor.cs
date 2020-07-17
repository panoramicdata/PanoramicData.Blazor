using System.Threading.Tasks;
using System.Collections.Generic;
using Microsoft.JSInterop;
using Microsoft.AspNetCore.Components;
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

		public async Task ShowAsync(double x, double y)
		{
			await JSRuntime.InvokeVoidAsync("showMenu", Id, x, y).ConfigureAwait(true);
		}

		public async Task ClickHandler(MenuItem item)
		{
			if(!item.IsDisabled)
			{
				await JSRuntime.InvokeVoidAsync("hideMenu", Id).ConfigureAwait(true);
			}
		}
	}
}
