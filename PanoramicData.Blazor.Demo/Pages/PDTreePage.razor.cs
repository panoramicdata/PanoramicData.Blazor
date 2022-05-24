namespace PanoramicData.Blazor.Demo.Pages;

public partial class PDTreePage
{
	private readonly IDataProviderService<FileExplorerItem> _dataProvider = new TestFileSystemDataProvider();
	private FileExplorerItem? _selectedEntry;
	private bool ShowLines { get; set; }
	private bool ShowRoot { get; set; } = true;
	private PDTree<FileExplorerItem>? Tree { get; set; }

	[CascadingParameter] protected EventManager? EventManager { get; set; }

	private void OnException(Exception ex)
	{
		EventManager?.Add(new Event("Exception", new EventArgument("Message", ex.Message)));
	}

	private void OnSelectionChanged(TreeNode<FileExplorerItem> node)
	{
		_selectedEntry = node?.Data;
		EventManager?.Add(new Event("SelectionChange", new EventArgument("Path", node?.Data?.Path)));
	}

	private void OnNodeExpanded(TreeNode<FileExplorerItem> node)
	{
		EventManager?.Add(new Event("NodeExpanded", new EventArgument("Path", node?.Data?.Path)));
	}

	private void OnNodeCollapsed(TreeNode<FileExplorerItem> node)
	{
		EventManager?.Add(new Event("NodeCollapsed", new EventArgument("Path", node?.Data?.Path)));
	}

	private void OnBeforeEdit(TreeNodeBeforeEditEventArgs<FileExplorerItem> args)
	{
		EventManager?.Add(new Event("BeforeEdit", new EventArgument("Path", args.Node?.Data?.Path)));

		// disallow edit of root node
		args.Cancel = args.Node?.ParentNode == null;
	}

	private void OnAfterEdit(TreeNodeAfterEditEventArgs<FileExplorerItem> args)
	{
		var item = args.Node.Data;
		EventManager?.Add(new Event("AfterEdit", new EventArgument("Path", item?.Path), new EventArgument("NewValue", args.NewValue)));

		if (string.IsNullOrWhiteSpace(args.NewValue))
		{
			args.Cancel = true;
		}
		else if (args.Node?.Data != null)
		{
			// update the underlying data items path value (as this is what the template displays)
			args.Node.Data.Path = $"{item?.ParentPath}/{args.NewValue}";
		}
	}
}
