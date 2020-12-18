using Microsoft.AspNetCore.Components;
using PanoramicData.Blazor.Demo.Data;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace PanoramicData.Blazor.Demo.Pages
{
	public partial class PDTreePage3
	{
		private bool _firstNodeUpdate = true;
		private PDTree<TreeItem> _tree = null!;
		private readonly TreeDataProvider _treeDataProvider = new TreeDataProvider();

		[CascadingParameter] protected EventManager? EventManager { get; set; }

		private void OnTreeNodeUpdated(TreeNode<TreeItem> _)
		{
			// expand the tree on first data fetch
			if (_firstNodeUpdate)
			{
				_firstNodeUpdate = false;
				_tree.ExpandAll();
				StateHasChanged();
			}
		}

		private string GetIconCssClass(TreeItem item, int _)
		{
			return item.IsGroup ? "fas fa-fw fa-building" : "fas fa-fw fa-user";
		}

		private async Task OnDrop(DropEventArgs args)
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
			var sourceNode = _tree.RootNode.Find(source.Id.ToString());
			var targetNode = _tree.RootNode.Find(target.Id.ToString());
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

		private void ReOrderNodes(IEnumerable<TreeNode<TreeItem>>? nodes)
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
}
