using PanoramicData.Blazor.Demo.Data;
using PanoramicData.Blazor.Services;
using System.Threading.Tasks;

namespace PanoramicData.Blazor.Demo.Pages
{
	public partial class PDFileModalsPage
	{
		private PDFileModal _fileModal = null!;
		private string _openResult = string.Empty;
		private string _saveAsResult = string.Empty;
		private readonly IDataProviderService<FileExplorerItem> _dataProvider = new TestFileSystemDataProvider();
		private bool _showOpen;

		private void OnModalHidden(string result)
		{
			// only called when modal NOT shown with a AndWaitResult method
			if (_showOpen)
			{
				_openResult = result;
			}
			else
			{
				_saveAsResult = result;
			}
		}

		private async Task ShowFileOpenModalAndWaitResult()
		{
			_openResult = await _fileModal.ShowOpenAndWaitResultAsync().ConfigureAwait(true);
		}

		private async Task ShowFileOpenFilteredModalAndWaitResult()
		{
			// show DOCX and XLSX files only
			_openResult = await _fileModal.ShowOpenAndWaitResultAsync(false, "*.docx;*.xlsx").ConfigureAwait(true);
		}

		private async Task ShowFolderOpenModalAndWaitResult()
		{
			_openResult = await _fileModal.ShowOpenAndWaitResultAsync(true).ConfigureAwait(true);
		}

		private async Task ShowFileSaveAsModalAndWaitResult()
		{
			_saveAsResult = await _fileModal.ShowSaveAsAndWaitResultAsync("NewFile.html").ConfigureAwait(true);
		}

		private async Task ShowFileOpenModal()
		{
			_showOpen = true;
			await _fileModal.ShowOpenAsync().ConfigureAwait(true);
		}

		private async Task ShowFileSaveAsModal()
		{
			_showOpen = false;
			await _fileModal.ShowSaveAsAsync().ConfigureAwait(true);
		}
	}
}
