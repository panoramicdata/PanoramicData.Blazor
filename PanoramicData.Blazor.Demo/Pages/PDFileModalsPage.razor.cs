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
		private IDataProviderService<FileExplorerItem> _dataProvider = new TestFileSystemDataProvider();

		private async Task ShowFileOpenModal(string _)
		{
			_openResult = await _fileModal.ShowOpenAsync();
		}

		private async Task ShowFileSaveAsModal(string _)
		{
			_saveAsResult = await _fileModal.ShowSaveAsAsync();
		}
	}
}
