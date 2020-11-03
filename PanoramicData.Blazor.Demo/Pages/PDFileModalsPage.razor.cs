using System.Threading.Tasks;
using Microsoft.AspNetCore.Components.Web;
using PanoramicData.Blazor.Services;
using PanoramicData.Blazor.Demo.Data;

namespace PanoramicData.Blazor.Demo.Pages
{
    public partial class PDFileModalsPage
    {
		private PDFileModal _fileOpenModal = null!;
		private PDFileModal _fileSaveAsModal = null!;
		private string _openResult = string.Empty;
		private IDataProviderService<FileExplorerItem> _dataProvider = new TestFileSystemDataProvider();

		private async Task ShowFileOpenModal(string _)
		{
			_openResult = await _fileOpenModal.ShowOpenAsync();
		}

		private async Task ShowFileSaveAsModal(string _)
		{
			_openResult = await _fileSaveAsModal.ShowOpenAsync();
		}
	}
}
