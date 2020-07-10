using PanoramicData.Blazor.Services;
using PanoramicData.Blazor.Web.Data;

namespace PanoramicData.Blazor.Web.Pages
{
	public partial class PDTreePage
    {
		private IDataProviderService<FileSystemEntry> _dataProvider = new TestFileSystemDataProvider();
		private PDTree<FileSystemEntry>? _tree;
		private bool _showLines = false;
		private bool _showRoot = true;
		private FileSystemEntry? _selectedEntry;

		private void OnSelectionChange(FileSystemEntry entry)
		{
			_selectedEntry = entry;
		}

	}
}
