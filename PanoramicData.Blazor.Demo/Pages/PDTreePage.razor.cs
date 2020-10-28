using System;
using System.IO;
using PanoramicData.Blazor.Services;
using PanoramicData.Blazor.Demo.Data;

namespace PanoramicData.Blazor.Demo.Pages
{
	public partial class PDTreePage
    {
		private IDataProviderService<FileExplorerItem> _dataProvider = new TestFileSystemDataProvider();
		private IDataProviderService<FileExplorerItem> _dataProviderOnDemand = new TestFileSystemDataProvider();
		private FileExplorerItem? _selectedEntry;
		private bool ShowLines { get; set; }
		private bool ShowRoot { get; set; } = true;
		private string Events { get; set; } = string.Empty;
		private PDTree<FileExplorerItem>? Tree { get; set; }

		private void SelectionChangeHandler(TreeNode<FileExplorerItem> node)
		{
			_selectedEntry = node?.Data;
			Events += $"selection changed: path = {node?.Data?.Path}{Environment.NewLine}";
		}

		private void NodeExpandedHandler(TreeNode<FileExplorerItem> node)
		{
			Events += $"node expanded: path = {node?.Data?.Path}{Environment.NewLine}";
		}

		private void NodeCollapsedHandler(TreeNode<FileExplorerItem> node)
		{
			Events += $"node collapsed: path = {node?.Data?.Path}{Environment.NewLine}";
		}

		private void BeforeEditHandler(TreeNodeBeforeEditEventArgs<FileExplorerItem> args)
		{
			Events += $"before edit: path = {args.Node?.Data?.Path}{Environment.NewLine}";

			// disallow edit of root node
			args.Cancel = args.Node?.ParentNode == null;
		}

		private void AfterEditHandler(TreeNodeAfterEditEventArgs<FileExplorerItem> args)
		{
			var item = args.Node.Data;
			Events += $"after edit: path = {item?.Path}, new value = {args.NewValue} {Environment.NewLine}";

			if (string.IsNullOrWhiteSpace(args.NewValue))
			{
				args.Cancel = true;
			}
			else if(args.Node?.Data != null)
			{
				// update the underlying data items path value (as this is what the template displays)
				args.Node.Data.Path = Path.Combine(item?.ParentPath, args.NewValue);
			}
		}
	}
}
