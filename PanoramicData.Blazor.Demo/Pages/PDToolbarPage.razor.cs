using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;
using PanoramicData.Blazor.Demo.Data;
using System.Collections.Generic;

namespace PanoramicData.Blazor.Demo.Pages
{
	public partial class PDToolbarPage
	{
		private string _searchText = string.Empty;

		private readonly List<MenuItem> _fileMenuItems = new()
		{
			new MenuItem { Text="New", IconCssClass="fas fa-fw fa-file-word" },
			new MenuItem { Key="Open", Text="Open...", IconCssClass="fas fa-fw fa-folder-open" },
			new MenuItem { IsSeparator=true },
			new MenuItem { Text="Save", IconCssClass="fas fa-fw fa-save", IsDisabled=true },
			new MenuItem { Key="SaveAs", Text="Save As..." },
			new MenuItem { Text="Exit", IsVisible=false }
		};

		private bool ShowButtons { get; set; } = true;
		private bool EnableButtons { get; set; } = true;

		[CascadingParameter] protected EventManager? EventManager { get; set; }

		public void OnButtonClick(string key)
		{
			EventManager?.Add(new Event("Click", new EventArgument("Key", key)));
		}

		private void OnKeypress(KeyboardEventArgs args)
		{
			EventManager?.Add(new Event("Keypress", new EventArgument("Key", args.Code)));
		}

		private void OnFileMenuClick(string itemKey)
		{
			EventManager?.Add(new Event("MenuClick", new EventArgument("Key", itemKey)));
		}

		private void OnCleared()
		{
			EventManager?.Add(new Event("Cleared"));
		}

		private void OnValueChanged(string value)
		{
			_searchText = value;
			EventManager?.Add(new Event("ValueChanged", new EventArgument("Value", value)));
		}
	}
}
