using System;
using PanoramicData.Blazor.Services;
using PanoramicData.Blazor.Web.Data;

namespace PanoramicData.Blazor.Web.Pages
{
	public partial class PDTreePage
    {
		private IDataProviderService<FileSystemEntry> _dataProvider = new TestFileSystemDataProvider();
		private IDataProviderService<FileSystemEntry> _dataProviderOnDemand = new TestFileSystemDataProvider();
		private PDTree<FileSystemEntry>? _tree;
		private bool _showLines = false;
		private bool _showRoot = true;
		private string _events = string.Empty;
		private FileSystemEntry? _selectedEntry;

		private void SelectionChangeHandler(FileSystemEntry entry)
		{
			_selectedEntry = entry;
			_events += $"selection changed: path = {entry.Path}{Environment.NewLine}";
		}

		private void NodeExpandedHandler(FileSystemEntry entry)
		{
			_events += $"node expanded: path = {entry.Path}{Environment.NewLine}";
		}

		private void NodeCollapsedHandler(FileSystemEntry entry)
		{
			_events += $"node collapsed: path = {entry.Path}{Environment.NewLine}";
		}
	}
}
