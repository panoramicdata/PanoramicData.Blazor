namespace PanoramicData.Blazor.Arguments;

/// <summary>
/// The TreeNodeEventArgs class holds details on a node event.
/// </summary>
public class TreeNodeEventArgs<TItem> : EventArgs where TItem : class
{
	/// <summary>
	/// Initializes a new instance of the TreeNodeEventArgs class.
	/// </summary>
	/// <param name="node">The node the event relates to.</param>
	public TreeNodeEventArgs(TreeNode<TItem> node)
	{
		Node = node;
	}

	/// <summary>
	/// Gets the node the event relates to.
	/// </summary>
	public TreeNode<TItem> Node { get; } = node;
}

/// <summary>
/// The TreeNodeCancelEventArgs class allows an event to be canceled.
/// </summary>
/// <remarks>
/// Initializes a new instance of the TreeNodeCancelEventArgs class.
/// </remarks>
/// <param name="node">The node the event relates to.</param>
public class TreeNodeCancelEventArgs<TItem>(TreeNode<TItem> node) : TreeNodeEventArgs<TItem>(node) where TItem : class
{

	/// <summary>
	/// Gets or sets whether the operation attempted should be canceled.
	/// </summary>
	public bool Cancel { get; set; }
}

/// <summary>
/// The TreeNodeBeforeEditEventArgs class allows a pending edit to be canceled.
/// </summary>
/// <remarks>
/// Initializes a new instance of the TreeNodeBeforeEditEventArgs class.
/// </remarks>
/// <param name="node">The node the event relates to.</param>
public class TreeNodeBeforeEditEventArgs<TItem>(TreeNode<TItem> node) : TreeNodeCancelEventArgs<TItem>(node) where TItem : class
{
}

/// <summary>
/// The TreeNodeAfterEditEventArgs class provides details of a completed edit and allows for cancellation.
/// </summary>
/// <remarks>
/// Initializes a new instance of the TreeNodeBeforeEditEventArgs class.
/// </remarks>
/// <param name="node">The node the event relates to.</param>
public class TreeNodeAfterEditEventArgs<TItem>(TreeNode<TItem> node, string oldValue, string newValue) : TreeNodeCancelEventArgs<TItem>(node) where TItem : class
{

	/// <summary>
	/// Gets the old value.
	/// </summary>
	public string OldValue { get; } = oldValue;

	/// <summary>
	/// Gets or sets the new value.
	/// </summary>
	public string NewValue { get; set; } = newValue;
}

/// <summary>
/// The TreeNodeBeforeEditEventArgs class allows a pending edit to be canceled.
/// </summary>
public class TreeBeforeSelectionChangeEventArgs<TItem> : CancelEventArgs where TItem : class
{
	/// <summary>
	/// Initializes a new instance of the TreeBeforeSelectionChangeEventArgs class.
	/// </summary>
	/// <param name="newNode">The new node that will be selected.</param>
	/// <param name="oldNode">The old node that was previously selected.</param>
	public TreeBeforeSelectionChangeEventArgs(TreeNode<TItem>? newNode, TreeNode<TItem>? oldNode)
	{
		NewNode = newNode;
		OldNode = oldNode;
	}

	/// <summary>
	/// Gets the old node that was previously selected.
	/// </summary>
	public TreeNode<TItem>? OldNode { get; }

	/// <summary>
	/// Gets the new node that will be selected.
	/// </summary>
	public TreeNode<TItem>? NewNode { get; }

}