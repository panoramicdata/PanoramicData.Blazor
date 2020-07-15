using System;
using PanoramicData.Blazor.Services;
using PanoramicData.Blazor.Web.Data;

namespace PanoramicData.Blazor.Web.Pages
{
	public partial class PDTreePage
    {
		private IDataProviderService<FileExplorerItem> _dataProvider = new TestFileSystemDataProvider();
		private IDataProviderService<FileExplorerItem> _dataProviderOnDemand = new TestFileSystemDataProvider();
		private PDTree<FileExplorerItem>? _tree;
		private bool _showLines = false;
		private bool _showRoot = true;
		private string _events = string.Empty;
		private FileExplorerItem? _selectedEntry;

		private void SelectionChangeHandler(FileExplorerItem entry)
		{
			_selectedEntry = entry;
			_events += $"selection changed: path = {entry.Path}{Environment.NewLine}";
		}

		private void NodeExpandedHandler(FileExplorerItem entry)
		{
			_events += $"node expanded: path = {entry.Path}{Environment.NewLine}";
		}

		private void NodeCollapsedHandler(FileExplorerItem entry)
		{
			_events += $"node collapsed: path = {entry.Path}{Environment.NewLine}";
		}
	}
}
