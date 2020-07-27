using System;
using System.IO;
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

		private void BeforeEditHandler(TreeNodeBeforeEditEventArgs<FileExplorerItem> args)
		{
			_events += $"before edit: path = {args.Node?.Data?.Path}{Environment.NewLine}";

			// disallow edit of root node
			args.Cancel = args.Node.ParentNode == null;
		}

		private void AfterEditHandler(TreeNodeAfterEditEventArgs<FileExplorerItem> args)
		{
			var item = args.Node.Data;
			_events += $"after edit: path = {item?.Path}, new value = {args.NewValue} {Environment.NewLine}";

			if (string.IsNullOrWhiteSpace(args.NewValue))
			{
				args.Cancel = true;
			}
			else
			{
				// update the underlying data items path value (as this is what the template displays)
				args.Node.Data.Path = Path.Combine(item.ParentPath, args.NewValue);
			}
		}
	}
}
