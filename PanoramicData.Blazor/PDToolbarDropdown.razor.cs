using Microsoft.AspNetCore.Components;
using PanoramicData.Blazor.Interfaces;
using PanoramicData.Blazor.Models;
using PanoramicData.Blazor.Services;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace PanoramicData.Blazor
{
	public partial class PDToolbarDropdown : IDisposable
	{
		#region Inject
		[Inject] IGlobalEventService GlobalEventService { get; set; } = null!;
		#endregion

		/// <summary>
		/// Gets or sets the unique identifier.
		/// </summary>
		[Parameter] public string Key { get; set; } = string.Empty;

		/// <summary>
		/// Gets or sets the button sizes.
		/// </summary>
		[Parameter] public ButtonSizes? Size { get; set; }

		/// <summary>
		/// Gets or sets the text displayed on the button.
		/// </summary>
		[Parameter] public string Text { get; set; } = string.Empty;

		/// <summary>
		/// Gets or sets CSS classes for the button.
		/// </summary>
		[Parameter] public string CssClass { get; set; } = "btn-secondary";

		/// <summary>
		/// Gets or sets CSS classes for the toolbar item.
		/// </summary>
		[Parameter] public string ItemCssClass { get; set; } = "";

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
		/// Gets or sets the menu items to be displayed in the context menu.
		/// </summary>
		[Parameter] public List<MenuItem> Items { get; set; } = new List<MenuItem>();

		/// <summary>
		/// Event raised whenever user clicks on the button.
		/// </summary>
		[Parameter] public EventCallback<string> Click { get; set; }

		private string ButtonSizeCssClass
		{
			get
			{
				return Size switch
				{
					ButtonSizes.Small => "btn-sm",
					ButtonSizes.Large => "btn-lg",
					_ => string.Empty,
				};
			}
		}

		protected override void OnInitialized()
		{
			GlobalEventService.KeyUpEvent += GlobalEventService_KeyUpEvent;
			foreach (var item in Items)
			{
				if (item.ShortcutKey.HasValue)
				{
					GlobalEventService.RegisterShortcutKey(item.ShortcutKey);
				}
			}
		}

		private async void GlobalEventService_KeyUpEvent(object sender, KeyboardInfo e)
		{
			foreach (var item in Items)
			{
				if (item.ShortcutKey.IsMatch(e.Key, e.Code, e.AltKey, e.CtrlKey, e.ShiftKey))
				{
					await InvokeAsync(async () => await OnClick(item.GetKeyOrText()).ConfigureAwait(true)).ConfigureAwait(true);
					break;
				}
			}
		}

		public void Dispose()
		{
			foreach (var item in Items)
			{
				if (item.ShortcutKey.HasValue)
				{
					GlobalEventService.UnregisterShortcutKey(item.ShortcutKey);
				}
			}
			GlobalEventService.KeyUpEvent -= GlobalEventService_KeyUpEvent;
		}

		private async Task OnClick(string itemKey)
		{
			await Click.InvokeAsync(itemKey).ConfigureAwait(true);
		}
	}
}
