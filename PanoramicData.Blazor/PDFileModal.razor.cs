using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;
using PanoramicData.Blazor.Services;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace PanoramicData.Blazor
{
	public partial class PDFileModal
	{
		private PDModal Modal { get; set; } = null!;
		private PDModal ModalConfirm { get; set; } = null!;
		private string _modalTitle = string.Empty;
		private PDFileExplorer FileExplorer { get; set; } = null!;
		private readonly List<ToolbarItem> _toolbarItems = new List<ToolbarItem>();
		private readonly ToolbarTextBox _filenameTextbox = new ToolbarTextBox { Key = "Filename", Label = "File name", Width = "100%" };
		private readonly ToolbarButton _cancelButton = new ToolbarButton { Key = "Cancel", Text = "Cancel", CssClass = "btn-secondary", IconCssClass = "fas fa-fw fa-times" };
		private readonly ToolbarButton _okButton = new ToolbarButton { Key = "OK", Text = "OK", CssClass = "btn-primary", ShiftRight = true, IsEnabled = false };
		private readonly List<ToolbarItem> _confirmToolbarItems = new List<ToolbarItem>();
		private readonly ToolbarButton _overwriteButton = new ToolbarButton { Key = "Yes", Text = "Yes - Overwrite", CssClass = "btn-danger", IconCssClass = "fas fa-fw fa-save", ShiftRight = true };
		private readonly ToolbarButton _noButton = new ToolbarButton { Key = "No", Text = "No", CssClass = "btn-secondary", IconCssClass = "fas fa-fw fa-times" };

		[Parameter] public IDataProviderService<FileExplorerItem> DataProvider { get; set; } = null!;

		[Parameter] public string OpenButtonText { get; set; } = "Open";

		[Parameter] public string SaveButtonText { get; set; } = "Save";

		[Parameter] public string OpenTitle { get; set; } = "File Open";

		[Parameter] public string SaveTitle { get; set; } = "File Save";

		[Parameter] public ModalSizes Size { get; set; } = ModalSizes.Large;

		[Parameter] public bool HideOnBackgroundClick { get; set; }

		protected override void OnInitialized()
		{
			// create toolbar contents
			_toolbarItems.AddRange(new ToolbarItem[] {
				_filenameTextbox,
				_okButton,
				_cancelButton
			});

			_confirmToolbarItems.AddRange(new ToolbarItem[] {
				_overwriteButton,
				_noButton
			});

			// wire up filename events
			_filenameTextbox.ValueChanged = OnFilenameChanged;
			_filenameTextbox.Keypress = OnFilenameKeypress;
		}

		public async Task<string> ShowOpenAsync()
		{
			_modalTitle = OpenTitle;
			if (_filenameTextbox.IsVisible)
			{
				_filenameTextbox.IsVisible = false;
				StateHasChanged();
			}
			if (_okButton.Text != OpenButtonText)
			{
				_okButton.Text = OpenButtonText;
				StateHasChanged();
			}
			if (_okButton.IconCssClass != "fas fa-fw fa-folder-open")
			{
				_okButton.IconCssClass = "fas fa-fw fa-folder-open";
				StateHasChanged();
			}

			var userAction = await Modal.ShowAndWaitResultAsync().ConfigureAwait(true);
			return userAction == "Cancel" ? string.Empty : $"{FileExplorer.FolderPath.TrimEnd('/')}/{_filenameTextbox.Value}";
		}

		public async Task<string> ShowSaveAsAsync()
		{
			_modalTitle = SaveTitle;
			if (!_filenameTextbox.IsVisible)
			{
				_filenameTextbox.IsVisible = true;
				StateHasChanged();
			}
			if (_okButton.Text != SaveButtonText)
			{
				_okButton.Text = SaveButtonText;
				StateHasChanged();
			}
			if (_okButton.IconCssClass != "fas fa-fw fa-save")
			{
				_okButton.IconCssClass = "fas fa-fw fa-save";
				StateHasChanged();
			}

			FileExplorerItem? existing = null;
			do
			{
				var userAction = await Modal.ShowAndWaitResultAsync().ConfigureAwait(true);
				if (userAction == "Cancel")
				{
					return string.Empty;
				}

				// check for over write?
				existing = System.Array.Find(FileExplorer.FileItems, x => x.EntryType == FileExplorerItemType.File && x.Name == _filenameTextbox.Value);
				if (existing != null)
				{
					var confirmation = await ModalConfirm.ShowAndWaitResultAsync().ConfigureAwait(true);
					if (confirmation == "Yes")
					{
						existing = null;
					}
				}

			} while (existing != null);

			return $"{FileExplorer.FolderPath.TrimEnd('/')}/{_filenameTextbox.Value}";
		}

		private void OnSelectionChanged(FileExplorerItem[] selection)
		{
			_filenameTextbox.Value = selection.Length == 1 && selection[0].EntryType == FileExplorerItemType.File ? selection[0].Name : string.Empty;
			_okButton.IsEnabled = !string.IsNullOrWhiteSpace(_filenameTextbox.Value);
		}

		private async Task OnItemDoubleClick(FileExplorerItem item)
		{
			if (item.EntryType == FileExplorerItemType.File)
			{
				_filenameTextbox.Value = item.Name;
				await Modal.OnButtonClick(new KeyedEventArgs<MouseEventArgs>(_okButton.Key)).ConfigureAwait(true);
			}
		}

		private void OnFilenameChanged(string value)
		{
			_filenameTextbox.Value = value;
			_okButton.IsEnabled = !string.IsNullOrWhiteSpace(_filenameTextbox.Value);
		}

		private void OnFilenameKeypress(KeyboardEventArgs args)
		{
			if (args.Code == "Enter" && !string.IsNullOrWhiteSpace(_filenameTextbox.Value))
			{
				Task.Run(async () => await Modal.OnButtonClick(new KeyedEventArgs<MouseEventArgs>(_okButton.Key)).ConfigureAwait(true)).ConfigureAwait(true);
			}
		}
	}
}
