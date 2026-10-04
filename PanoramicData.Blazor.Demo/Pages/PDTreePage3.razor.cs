namespace PanoramicData.Blazor.Demo.Pages;

public partial class PDTreePage3
{
	protected PDTree<TreeItem> Tree { get; set; } = null!;
	private readonly TreeDataProvider _treeDataProvider = new();

	[CascadingParameter] protected EventManager? EventManager { get; set; }

	private void OnReady() => Tree.ExpandAll();

	private static string GetIconCssClass(TreeItem item) => item.IsGroup ? "fas fa-fw fa-building" : "fas fa-fw fa-user";

	private static TreeItem? GetFirstPayloadItem(object? payload)
		=> payload is List<TreeItem> { Count: > 0 } items ? items[0] : null;

	private void OnDrop(DropEventArgs args)
	{
		var targetItem = (args.Target as TreeNode<TreeItem>)?.Data;
		var sourceItem = GetFirstPayloadItem(args.Payload);

		EventManager?.Add(new Event("Drop",
			new EventArgument("Source", sourceItem?.Name),
			new EventArgument("Target", targetItem?.Name),
			new EventArgument("Before", args.Before),
			new EventArgument("Ctrl", args.Ctrl)));

		if (sourceItem != null && targetItem != null)
		{
			ReOrder(sourceItem, targetItem, args.Before);
		}
	}
}
