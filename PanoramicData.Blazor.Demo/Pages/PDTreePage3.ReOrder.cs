namespace PanoramicData.Blazor.Demo.Pages;

/// <summary>
/// Moving tree nodes in response to a drop, for the PDTree drag and drop demo.
/// </summary>
public partial class PDTreePage3
{
	/// <summary>
	/// Decides, for each drop position, whether the dragged item may land there: only a group can have items
	/// dropped into it, an item can only sit beside its own kind, and a group never moves inside itself.
	/// </summary>
	private bool IsDropPositionValid(TreeItem target, object? payload, DropPosition position)
	{
		if (GetFirstPayloadItem(payload) is not { } source || IsWithin(target, source))
		{
			return false;
		}

		return position == DropPosition.Into ? target.IsGroup : source.IsGroup == target.IsGroup;
	}

	/// <summary>
	/// Gets whether an item is the given group or anywhere inside it.
	/// </summary>
	private bool IsWithin(TreeItem item, TreeItem group)
	{
		for (var node = Tree.RootNode.Find(item.Id.ToString()); node?.Data is { } data; node = node.ParentNode)
		{
			if (data.Id == group.Id)
			{
				return true;
			}
		}

		return false;
	}

	public void ReOrder(TreeItem source, TreeItem target, DropPosition position)
	{
		if (!IsDropPositionValid(target, new List<TreeItem> { source }, position))
		{
			return;
		}

		// find source and target nodes, and the sibling lists they belong to
		if (Tree.RootNode.Find(source.Id.ToString()) is not { ParentNode.Nodes: { } sourceSiblings } sourceNode
			|| Tree.RootNode.Find(target.Id.ToString()) is not { ParentNode.Nodes: { } targetSiblings } targetNode)
		{
			return;
		}

		sourceSiblings.Remove(sourceNode);
		ReOrderNodes(sourceSiblings);

		if (position == DropPosition.Into)
		{
			MoveIntoNode(sourceNode, targetNode);
		}
		else
		{
			InsertBesideNode(sourceNode, targetNode, targetSiblings, position == DropPosition.Before);
		}
	}

	private static void InsertBesideNode(TreeNode<TreeItem> sourceNode, TreeNode<TreeItem> targetNode, List<TreeNode<TreeItem>> siblings, bool before)
	{
		var tIdx = siblings.IndexOf(targetNode);
		siblings.Insert(before ? tIdx : tIdx + 1, sourceNode);
		sourceNode.ParentNode = targetNode.ParentNode;
		if (sourceNode.Data is { } item)
		{
			item.ParentId = targetNode.ParentNode?.Data?.Id;
		}

		ReOrderNodes(siblings);
	}

	private static void MoveIntoNode(TreeNode<TreeItem> sourceNode, TreeNode<TreeItem> targetNode)
	{
		sourceNode.ParentNode = targetNode;
		if (sourceNode.Data is { } item)
		{
			item.ParentId = targetNode.Data?.Id;
		}

		if (targetNode.Nodes is { } children)
		{
			children.Add(sourceNode);
			ReOrderNodes(children);
		}
	}

	private static void ReOrderNodes(IEnumerable<TreeNode<TreeItem>> nodes)
	{
		var index = 0;
		foreach (var item in nodes.Select(node => node.Data).OfType<TreeItem>())
		{
			item.Order = ++index;
		}
	}
}
