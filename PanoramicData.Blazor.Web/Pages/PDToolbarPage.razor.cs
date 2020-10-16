using System;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Collections.Generic;
using Microsoft.AspNetCore.Components.Web;

namespace PanoramicData.Blazor.Web.Pages
{
    public partial class PDToolbarPage
    {
		private string _events = string.Empty;
		private bool _showButtons = true;
		private bool _enableButtons = true;
		private string _searchText = string.Empty;
		private List<ToolbarItem> ToolbarItems = new List<ToolbarItem>
		{
			new ToolbarButton { Key = "tb-open", Text = "Open", CssClass="btn-primary", IconCssClass = "fas fa-fw fa-folder-open", TextCssClass="d-none d-sm-none d-md-inline", ToolTip="Open something" },
			new ToolbarButton { Key = "tb-rename", Text="Rename", CssClass="btn-secondary", IconCssClass = "fas fa-fw fa-edit", TextCssClass="d-none d-sm-none d-md-inline", ToolTip="Rename something" },
			new ToolbarSeparator(),
			new ToolbarButton { Key = "tb-download", Text="Download", CssClass="btn-secondary", IconCssClass="fas fa-fw fa-file-download", TextCssClass="d-none d-sm-none d-md-inline", ToolTip="Download something" },
			new ToolbarButton { Key = "tb-enabledisable", Text="Disable",  CssClass="btn-secondary", ShiftRight = true },
			new ToolbarButton { Key = "tb-showhide", Text="Hide",  CssClass="btn-secondary" }
		};

		public void OnOpen()
		{
			_events += $"Open button click{Environment.NewLine}";
		}

		public void OnRename()
		{
			_events += $"Rename button click{Environment.NewLine}";
		}

		public void OnDownload()
		{
			_events += $"Download button click{Environment.NewLine}";
		}

		public void OnButtonClick(string key)
		{
			switch(key)
			{
				case "tb-enabledisable":
					{
						ToolbarItems[0].IsEnabled = !ToolbarItems[0].IsEnabled;
						ToolbarItems[1].IsEnabled = !ToolbarItems[1].IsEnabled;
						ToolbarItems[3].IsEnabled = !ToolbarItems[3].IsEnabled;
						var button = ToolbarItems[4] as ToolbarButton;
						if (button.Text.StartsWith("Disable"))
						{
							button.Text = "Enable";
						}
						else
						{
							button.Text = "Disable";
						}
					}
					break;

				case "tb-showhide":
					{
						ToolbarItems[0].IsVisible = !ToolbarItems[0].IsVisible;
						ToolbarItems[1].IsVisible = !ToolbarItems[1].IsVisible;
						ToolbarItems[2].IsVisible = !ToolbarItems[2].IsVisible;
						ToolbarItems[3].IsVisible = !ToolbarItems[3].IsVisible;
						var button = ToolbarItems[5] as ToolbarButton;
						if (button.Text.StartsWith("Show"))
						{
							button.Text = "Hide";
						}
						else
						{
							button.Text = "Show";
						}
					}
					break;

				default:
					_events += $"Button {key} Clicked{Environment.NewLine}";
					break;
			}
		}

		private void OnKeypress(KeyboardEventArgs args)
		{
			_events += $"Search textbox key-press: {args.Key} {Environment.NewLine}";
		}
	}
}
