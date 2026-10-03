namespace PanoramicData.Blazor.Demo.Pages;

/// <summary>
/// Moving tree nodes in response to a drop, for the PDTree drag and drop demo.
/// </summary>
public partial class PDTreePage3
{
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

		// find source and target nodes, and the sibling lists they belong to
		if (Tree.RootNode.Find(source.Id.ToString()) is not { ParentNode.Nodes: { } sourceSiblings } sourceNode
			|| Tree.RootNode.Find(target.Id.ToString()) is not { ParentNode.Nodes: { } targetSiblings } targetNode)
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

	private static void InsertBesideNode(TreeNode<TreeItem> sourceNode, TreeNode<TreeItem> targetNode, List<TreeNode<TreeItem>> siblings, bool before)
	{
		var tIdx = siblings.IndexOf(targetNode);
		siblings.Insert(before ? tIdx : tIdx + 1, sourceNode);
		sourceNode.ParentNode = targetNode.ParentNode;
		ReOrderNodes(siblings);
	}

	private static void MoveIntoNode(TreeNode<TreeItem> sourceNode, TreeNode<TreeItem> targetNode)
	{
		sourceNode.ParentNode = targetNode;
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
