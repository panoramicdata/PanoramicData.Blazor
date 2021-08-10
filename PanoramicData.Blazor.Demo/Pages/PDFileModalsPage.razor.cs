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

		private async Task ShowFileOpenModal()
		{
			_openResult = await _fileModal.ShowOpenAsync().ConfigureAwait(true);
		}

		private async Task ShowFileOpenFilteredModal()
		{
			// show DOCX and XLSX files only
			_openResult = await _fileModal.ShowOpenAsync(false, "*.docx;*.xlsx").ConfigureAwait(true);
		}

		private async Task ShowFolderOpenModal()
		{
			_openResult = await _fileModal.ShowOpenAsync(true).ConfigureAwait(true);
		}

		private async Task ShowFileSaveAsModal()
		{
			_saveAsResult = await _fileModal.ShowSaveAsAsync("NewFile.html").ConfigureAwait(true);
		}
	}
}
