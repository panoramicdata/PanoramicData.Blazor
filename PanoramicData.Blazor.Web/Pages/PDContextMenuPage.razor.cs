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
			_items.Add(new MenuItem { Text = "One" });
			_items.Add(new MenuItem { Text = "Two" });
			_items.Add(new MenuItem { Text = "Three", IsDisabled = true });
			_items.Add(new MenuItem { Text = "Four", IsVisible = false });
			_items.Add(new MenuItem { Text = "Five" });
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
