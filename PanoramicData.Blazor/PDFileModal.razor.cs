using System.Threading.Tasks;
using System.Collections.Generic;
using Microsoft.AspNetCore.Components;
using PanoramicData.Blazor.Services;

namespace PanoramicData.Blazor
{
	public partial class PDFileModal
    {
		private PDModal _modal = null!;
		private string _modalTitle = string.Empty;
		private List<ToolbarItem> _modalButtons = new List<ToolbarItem>
		{
			new ToolbarButton { Text = "OK", CssClass = "btn-primary", IconCssClass = "fas fa-fw fa-folder-open" },
			new ToolbarButton { Text = "Cancel", CssClass = "btn-secondary", IconCssClass = "fas fa-fw fa-times" }
		};
		private FileExplorerItem? _selecteditem = null;

		/// <summary>
		/// Sets the IDataProviderService instance to use to fetch data.
		/// </summary>
		[Parameter] public IDataProviderService<FileExplorerItem> DataProvider { get; set; } = null!;

		[Parameter] public string OpenButtonText { get; set; } = "Open";

		[Parameter] public string OpenTitle { get; set; } = "File Open";

		public async Task<string> ShowOpenAsync()
		{
			// update button state
			_modalTitle = OpenTitle;
			if (_modal.Buttons[0] is ToolbarButton firstButton && firstButton.Text != OpenButtonText)
			{
				firstButton.Text = OpenButtonText;
				StateHasChanged();
			}
			var userAction = await _modal.ShowAndWaitResultAsync().ConfigureAwait(true);
			return userAction == "Cancel" || _selecteditem == null ? string.Empty : _selecteditem.Path;
		}

		public void OnSelectionChanged(FileExplorerItem[] selection)
		{
			if (_modal.Buttons[0] is ToolbarButton firstButton)
			{
				firstButton.IsEnabled = selection.Length > 0 && selection[0].EntryType == FileExplorerItemType.File;
				if(selection.Length == 1 && selection[0].EntryType == FileExplorerItemType.File)
				{
					_selecteditem = selection[0];
				}
			}
		}

		public async Task OnItemDoubleClick(FileExplorerItem item)
		{
			if (_modal.Buttons[0] is ToolbarButton firstButton)
			{
				_selecteditem = item;
				await _modal.OnButtonClick(firstButton.Key).ConfigureAwait(true);
			}
		}
	}
}
