using Microsoft.AspNetCore.Components.Web;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace PanoramicData.Blazor.Web.Pages
{
	public partial class PDContextMenuPage
    {
		private List<MenuItem> _items = new List<MenuItem>();
		private PDContextMenu _contextMenu;
		protected override void OnInitialized()
		{
			_items.Add(new MenuItem { Text = "One", IconCssClass = "fas fa-dice-one" });
			_items.Add(new MenuItem { Text = "Two", IconCssClass = "fas fa-dice-two" });
			_items.Add(new MenuItem { Text = "Three", IsDisabled = true, IconCssClass = "fas fa-dice-three" });
			_items.Add(new MenuItem { Text = "Four", IsVisible = false, IconCssClass = "fas fa-dice-four" });
			_items.Add(new MenuItem { Text = "Five", IconCssClass = "fas fa-dice-five" });
			_items.Add(new MenuItem { Text = "Six", IconCssClass = "fas fa-dice-six" });
		}

		private async Task ShowMenuHandler(MouseEventArgs args)
		{
			if (args.Button == 2)
			{
				await _contextMenu.ShowAsync(args.ClientX, args.ClientY);
			}
		}
	}
}
