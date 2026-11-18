namespace PanoramicData.Blazor;

/// <summary>
/// PDTree: choosing where a drop lands when nodes are reordered by dragging, and showing it.
/// </summary>
public partial class PDTree<TItem>
{
	private object? _dropIndicatorSource;

	/// <summary>
	/// Optional predicate, consulted for each drop position when <see cref="AllowDropInBetween"/> is set, that
	/// decides whether the drag payload may be dropped before, after or into a node. A consumer can refuse
	/// <see cref="DropPosition.Into"/> while allowing before and after. When <c>null</c>, every position is allowed.
	/// <see cref="IsDropValid"/> still applies to <see cref="DropPosition.Into"/> as well.
	/// </summary>
	[Parameter] public Func<TItem, object?, DropPosition, bool>? IsDropPositionValid { get; set; }

	/// <summary>
	/// Gets or sets whether, with <see cref="AllowDropInBetween"/>, dropping in the bottom of the last child's row
	/// with the pointer left of the child's text means after its parent rather than after the child.
	/// </summary>
	[Parameter] public bool AllowDropAfterParent { get; set; }

	/// <summary>Gets the node the drop indicator is shown for, if any.</summary>
	internal TreeNode<TItem>? DropIndicatorNode { get; private set; }

	/// <summary>Gets the position the drop indicator shows.</summary>
	internal DropPosition DropIndicatorPosition { get; private set; }

	/// <summary>Gets whether the drop the indicator shows is allowed.</summary>
	internal bool DropIndicatorValid { get; private set; }

	/// <summary>
	/// Gets whether the payload may be dropped at a position relative to a node.
	/// </summary>
	internal bool IsDropPositionAllowed(TreeNode<TItem> node, object? payload, DropPosition position)
	{
		if (node.Data is null)
		{
			return true;
		}

		if (position == DropPosition.Into && IsDropValid?.Invoke(node.Data, payload) == false)
		{
			return false;
		}

		return IsDropPositionValid?.Invoke(node.Data, payload, position) ?? true;
	}

	/// <summary>
	/// Gets whether a node is drawn in the tree, rather than being the hidden root.
	/// </summary>
	internal bool IsRenderedNode(TreeNode<TItem>? node)
	{
		if (node is null || node == RootNode)
		{
			return false;
		}

		return ShowRoot || RootNode.Nodes is not { Count: 1 } || node != RootNode.Nodes[0];
	}

	/// <summary>
	/// Shows the drop indicator for a position relative to a node, re-rendering only when it changes.
	/// </summary>
	/// <param name="source">The row the pointer is over, which alone may later clear it.</param>
	/// <param name="node">The node the drop is relative to.</param>
	/// <param name="position">The drop position.</param>
	/// <param name="valid">Whether the drop is allowed.</param>
	internal void SetDropIndicator(object source, TreeNode<TItem> node, DropPosition position, bool valid)
	{
		_dropIndicatorSource = source;
		if (DropIndicatorNode == node && DropIndicatorPosition == position && DropIndicatorValid == valid)
		{
			return;
		}

		DropIndicatorNode = node;
		DropIndicatorPosition = position;
		DropIndicatorValid = valid;
		StateHasChanged();
	}

	/// <summary>
	/// Clears the drop indicator if it was set by the given row.
	/// </summary>
	internal void ClearDropIndicator(object source)
	{
		if (_dropIndicatorSource == source)
		{
			ClearDropIndicator();
		}
	}

	/// <summary>
	/// Clears the drop indicator.
	/// </summary>
	internal void ClearDropIndicator()
	{
		_dropIndicatorSource = null;
		if (DropIndicatorNode is not null)
		{
			DropIndicatorNode = null;
			StateHasChanged();
		}
	}
}
