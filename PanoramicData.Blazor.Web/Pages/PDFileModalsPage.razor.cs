using System.Threading.Tasks;
using Microsoft.AspNetCore.Components.Web;
using PanoramicData.Blazor.Services;
using PanoramicData.Blazor.Web.Data;

namespace PanoramicData.Blazor.Web.Pages
{
    public partial class PDFileModalsPage
    {
		private PDFileModal _fileModal = null!;
		private string _openResult = string.Empty;
		private IDataProviderService<FileExplorerItem> _dataProvider = new TestFileSystemDataProvider();

		private async Task ShowFileOpenModal(MouseEventArgs args)
		{
			_openResult = await _fileModal.ShowOpenAsync();
		}
	}
}
