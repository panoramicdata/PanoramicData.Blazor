namespace PanoramicData.Blazor.Demo.Pages;

public partial class PDTreePage
{
	private readonly IDataProviderService<FileExplorerItem> _dataProvider = new TestFileSystemDataProvider();
	private FileExplorerItem? _selectedEntry;
	private bool _cancelSelection;

	private bool ShowLines { get; set; }

	private bool ShowRoot { get; set; } = true;

	private PDTree<FileExplorerItem>? Tree { get; set; }

	[CascadingParameter] protected EventManager? EventManager { get; set; }

	private void OnBeforeSelectionChanged(TreeBeforeSelectionChangeEventArgs<FileExplorerItem> args)
	{
		if (_cancelSelection)
		{
			args.Cancel = true;
			return;
		}

		EventManager?.Add(new Event("BeforeSelectionChange", new EventArgument("NewPath", args.NewNode?.Data?.Path), new EventArgument("OldPath", args.OldNode?.Data?.Path)));
	}

	private void OnException(Exception ex) => EventManager?.Add(new Event("Exception", new EventArgument("Message", ex.Message)));

	private void OnSelectionChanged(TreeNode<FileExplorerItem> node)
	{
		_selectedEntry = node?.Data;
		EventManager?.Add(new Event("SelectionChange", new EventArgument("Path", node?.Data?.Path)));
	}

	private void OnNodeExpanded(TreeNode<FileExplorerItem> node) => EventManager?.Add(new Event("NodeExpanded", new EventArgument("Path", node?.Data?.Path)));

	private void OnNodeCollapsed(TreeNode<FileExplorerItem> node) => EventManager?.Add(new Event("NodeCollapsed", new EventArgument("Path", node?.Data?.Path)));

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

	private async Task ScrollToAlicesFiles()
	{
		if (Tree is null)
		{
			return;
		}

		// Find the "Alice" node whose parent is "Users"
		var node = Tree.Search(n =>
			n.Text == "Alice" &&
			n.ParentNode is not null &&
			n.ParentNode.Text == "Users"
		);

		if (node is not null)
		{
			await Tree.SelectNode(node, false);
			if (!node.IsExpanded)
			{
				await Tree.ToggleNodeIsExpandedAsync(node); // Expand the Alice node
			}
			await Tree.ScrollNodeIntoViewAsync(node);
		}
	}
}
