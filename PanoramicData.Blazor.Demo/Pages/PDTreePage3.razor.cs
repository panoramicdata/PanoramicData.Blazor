namespace PanoramicData.Blazor.Demo.Pages;

public partial class PDTreePage3
{
	protected PDTree<TreeItem> Tree { get; set; } = null!;
	private readonly TreeDataProvider _treeDataProvider = new();

	[CascadingParameter] protected EventManager? EventManager { get; set; }

	private void OnReady() => Tree.ExpandAll();

	private static string GetIconCssClass(TreeItem item, int _) => item.IsGroup ? "fas fa-fw fa-building" : "fas fa-fw fa-user";

	private void OnDrop(DropEventArgs args)
	{
		var targetItem = (args.Target as TreeNode<TreeItem>)?.Data;
		TreeItem? sourceItem = null;
		if (args.Payload is List<TreeItem> items && items.Count > 0)
		{
			sourceItem = items[0];
		}

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

	public void ReOrder(TreeItem source, TreeItem target, bool? before)
	{
		// validate the move
		if (source.IsGroup)
		{
			// can only drag group onto another group
			if (!target.IsGroup)
			{
				return;
			}
		}
		else
		{
			// can only drop person onto group itself (not before or after)
			if (target.IsGroup && before != null)
			{
				return;
			}
		}

		// find source and target nodes
		var sourceNode = Tree.RootNode.Find(source.Id.ToString());
		var targetNode = Tree.RootNode.Find(target.Id.ToString());
		if (sourceNode?.ParentNode?.Nodes is null || targetNode?.ParentNode?.Nodes is null)
		{
			return;
		}

		// remove source node from parent node
		sourceNode.ParentNode?.Nodes?.Remove(sourceNode);

		if (source.IsGroup || !target.IsGroup)
		{
			var tIdx = targetNode.ParentNode.Nodes.IndexOf(targetNode);
			targetNode.ParentNode.Nodes.Insert(before == true ? tIdx : tIdx + 1, sourceNode);
			sourceNode.ParentNode = targetNode.ParentNode;
			ReOrderNodes(targetNode.ParentNode.Nodes);
		}
		else
		{
			targetNode.Nodes?.Add(sourceNode);
			sourceNode.ParentNode = targetNode;
			ReOrderNodes(targetNode.Nodes);
		}
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
