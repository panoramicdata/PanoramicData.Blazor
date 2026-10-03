namespace PanoramicData.Blazor.Demo.Pages;

public partial class PDTreePage3
{
	protected PDTree<TreeItem> Tree { get; set; } = null!;
	private readonly TreeDataProvider _treeDataProvider = new();

	[CascadingParameter] protected EventManager? EventManager { get; set; }

	private void OnReady() => Tree.ExpandAll();

	private static string GetIconCssClass(TreeItem item) => item.IsGroup ? "fas fa-fw fa-building" : "fas fa-fw fa-user";

	private static TreeItem? GetFirstPayloadItem(object? payload)
		=> payload is List<TreeItem> items && items.Count > 0 ? items[0] : null;

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

	/// <summary>
	/// Validates a move: a group can only be dragged onto another group and a person
	/// can only be dropped onto a group itself (not before or after it).
	/// </summary>
	private static bool IsValidMove(TreeItem source, TreeItem target, bool? before)
		=> source.IsGroup ? target.IsGroup : !(target.IsGroup && before != null);

	public void ReOrder(TreeItem source, TreeItem target, bool? before)
	{
		// validate the move
		if (!IsValidMove(source, target, before))
		{
			return;
		}

		// find source and target nodes
		var sourceNode = Tree.RootNode.Find(source.Id.ToString());
		var targetNode = Tree.RootNode.Find(target.Id.ToString());
		var sourceSiblings = GetSiblings(sourceNode);
		var targetSiblings = GetSiblings(targetNode);
		if (sourceNode is null || targetNode is null || sourceSiblings is null || targetSiblings is null)
		{
			return;
		}

		// remove source node from parent node
		sourceSiblings.Remove(sourceNode);

		if (source.IsGroup || !target.IsGroup)
		{
			InsertBesideNode(sourceNode, targetNode, targetSiblings, before == true);
		}
		else
		{
			MoveIntoNode(sourceNode, targetNode);
		}
	}

	private static List<TreeNode<TreeItem>>? GetSiblings(TreeNode<TreeItem>? node) => node?.ParentNode?.Nodes;

	private static void InsertBesideNode(TreeNode<TreeItem> sourceNode, TreeNode<TreeItem> targetNode, List<TreeNode<TreeItem>> siblings, bool before)
	{
		var tIdx = siblings.IndexOf(targetNode);
		siblings.Insert(before ? tIdx : tIdx + 1, sourceNode);
		sourceNode.ParentNode = targetNode.ParentNode;
		ReOrderNodes(siblings);
	}

	private static void MoveIntoNode(TreeNode<TreeItem> sourceNode, TreeNode<TreeItem> targetNode)
	{
		targetNode.Nodes?.Add(sourceNode);
		sourceNode.ParentNode = targetNode;
		ReOrderNodes(targetNode.Nodes);
	}

	private static void ReOrderNodes(IEnumerable<TreeNode<TreeItem>>? nodes)
	{
		if (nodes != null)
		{
			var index = 0;
			foreach (var node in nodes)
			{
				if (node.Data != null)
				{
					node.Data.Order = ++index;
				}
			}
		}
	}
}
