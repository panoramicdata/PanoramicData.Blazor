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

		private void SelectionChangeHandler(TreeNode<FileExplorerItem> node)
		{
			_selectedEntry = node?.Data;
			_events += $"selection changed: path = {node?.Data?.Path}{Environment.NewLine}";
		}

		private void NodeExpandedHandler(TreeNode<FileExplorerItem> node)
		{
			_events += $"node expanded: path = {node?.Data?.Path}{Environment.NewLine}";
		}

		private void NodeCollapsedHandler(TreeNode<FileExplorerItem> node)
		{
			_events += $"node collapsed: path = {node?.Data?.Path}{Environment.NewLine}";
		}
	}
}
