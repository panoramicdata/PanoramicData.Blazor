namespace PanoramicData.Blazor;

/// <summary>
/// PDTreeNode: with drop in between allowed, the pointer's position within the row decides the drop position.
/// </summary>
public partial class PDTreeNode<TItem>
{
	private ElementReference _headerRef;
	private ElementReference _contentRef;
	private int _headerDragCount;
	private double[]? _rowBounds;
	private bool _skipRender;

	/// <summary>
	/// Gets the row's drag handlers, attached only when drop in between is allowed so other trees are unchanged.
	/// </summary>
	private Dictionary<string, object> HeaderDragAttributes => AllowDropInBetween
		? new Dictionary<string, object>
		{
			["ondragenter"] = EventCallback.Factory.Create<DragEventArgs>(this, OnHeaderDragEnterAsync),
			["ondragleave"] = EventCallback.Factory.Create<DragEventArgs>(this, OnHeaderDragLeave),
			["ondragover"] = EventCallback.Factory.Create<DragEventArgs>(this, OnHeaderDragOver),
			["ondrop"] = EventCallback.Factory.Create<DragEventArgs>(this, OnHeaderDropAsync)
		}
		: [];

	private bool IsDropIndicatorHere => AllowDropInBetween && Node is not null && Tree?.DropIndicatorNode == Node;

	/// <summary>Gets the insertion line to draw for this node, if any.</summary>
	private DropPosition? DropLine => IsDropIndicatorHere && Tree.DropIndicatorPosition != DropPosition.Into
		? Tree.DropIndicatorPosition
		: null;

	private string DropLineCssClass => Tree.DropIndicatorValid ? "pdtree-drop-line" : "pdtree-drop-line invalid";

	private string NodeCssClass => AllowDropInBetween ? "pdtreenode pdtreenode-reorder" : "pdtreenode";

	/// <summary>
	/// Gets the drop highlight for the row: with drop in between, only for a drop into this node.
	/// </summary>
	private string? ContentDragCssClass
	{
		get
		{
			if (AllowDropInBetween)
			{
				return IsDropIndicatorHere && Tree.DropIndicatorPosition == DropPosition.Into
					? (Tree.DropIndicatorValid ? "drag-over" : "drag-over-invalid")
					: null;
			}

			return AllowDrop && _dragOverCount > 0 ? (IsDragOverValid ? "drag-over" : "drag-over-invalid") : null;
		}
	}

	private (DropPosition? Line, bool Into, bool Valid) DropIndicatorSnapshot
		=> (DropLine, ContentDragCssClass is not null, Tree?.DropIndicatorValid ?? true);

	/// <summary>
	/// Measures the row: its top and height, and where its content starts, in client pixels.
	/// </summary>
	private async Task<double[]?> GetRowBoundsAsync()
	{
		if (_commonModule is null)
		{
			return null;
		}

		var bounds = await _commonModule.InvokeAsync<double[]?>("getTreeRowBounds", _headerRef, _contentRef).ConfigureAwait(true);
		return bounds is { Length: 3 } ? bounds : null;
	}

	/// <summary>
	/// Decides the node and position a drop at the pointer refers to.
	/// </summary>
	private (TreeNode<TItem> Target, DropPosition Position) ResolveDrop(TreeNode<TItem> node, DragEventArgs args, double[] bounds)
	{
		var allowInto = AllowDrop && Tree.IsDropPositionAllowed(node, DragContext?.Payload, DropPosition.Into);
		var position = TreeDropZone.GetPosition(args.ClientY - bounds[0], bounds[1], allowInto);
		if (position == DropPosition.After && IsOutdented(node, args, bounds))
		{
			return (node.ParentNode!, DropPosition.After);
		}

		return (node, position);
	}

	/// <summary>
	/// Gets whether the pointer is left of the last child's text, so the drop means after its parent.
	/// </summary>
	private bool IsOutdented(TreeNode<TItem> node, DragEventArgs args, double[] bounds)
		=> Tree.AllowDropAfterParent
			&& args.ClientX < bounds[2]
			&& node.IsLastSibling()
			&& Tree.IsRenderedNode(node.ParentNode);

	private async Task OnHeaderDragEnterAsync(DragEventArgs args)
	{
		_skipRender = true;
		if (_headerDragCount++ == 0)
		{
			_rowBounds = await GetRowBoundsAsync().ConfigureAwait(true);
		}
	}

	private void OnHeaderDragLeave(DragEventArgs args)
	{
		_skipRender = true;
		_headerDragCount = Math.Max(0, _headerDragCount - 1);
		if (_headerDragCount == 0)
		{
			Tree.ClearDropIndicator(this);
		}
	}

	private void OnHeaderDragOver(DragEventArgs args)
	{
		_skipRender = true;
		if (Node is null || _rowBounds is null)
		{
			return;
		}

		var (target, position) = ResolveDrop(Node, args, _rowBounds);
		Tree.SetDropIndicator(this, target, position, Tree.IsDropPositionAllowed(target, DragContext?.Payload, position));
	}

	private async Task OnHeaderDropAsync(DragEventArgs args)
	{
		_headerDragCount = 0;
		_dragOverCount = 0;
		var bounds = await GetRowBoundsAsync().ConfigureAwait(true) ?? _rowBounds;
		Tree.ClearDropIndicator();
		if (DragContext is null || Node is null)
		{
			return;
		}

		var (target, position) = bounds is null
			? (Node, AllowDrop ? DropPosition.Into : DropPosition.After)
			: ResolveDrop(Node, args, bounds);
		if (Tree.IsDropPositionAllowed(target, DragContext.Payload, position))
		{
			await Drop.InvokeAsync(new DropEventArgs(target, DragContext.Payload, args.CtrlKey, position)).ConfigureAwait(true);
		}
	}
}
