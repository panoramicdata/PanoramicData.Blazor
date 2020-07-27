namespace PanoramicData.Blazor
{
	/// <summary>
	/// The TreeNodeEventArgs class holds details on a node event.
	/// </summary>
	public class TreeNodeEventArgs<TItem> where TItem : class
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
		public TreeNode<TItem> Node { get; }
	}

	/// <summary>
	/// The TreeNodeCancelEventArgs class allows an event to be canceled.
	/// </summary>
	public class TreeNodeCancelEventArgs<TItem> : TreeNodeEventArgs<TItem> where TItem : class
	{
		/// <summary>
		/// Initializes a new instance of the TreeNodeCancelEventArgs class.
		/// </summary>
		/// <param name="node">The node the event relates to.</param>
		public TreeNodeCancelEventArgs(TreeNode<TItem> node)
			: base(node)
		{
		}

		/// <summary>
		/// Gets or sets whether the operation attempted should be canceled.
		/// </summary>
		public bool Cancel { get; set; }
	}

	/// <summary>
	/// The TreeNodeBeforeEditEventArgs class allows a pending edit to be canceled.
	/// </summary>
	public class TreeNodeBeforeEditEventArgs<TItem> : TreeNodeCancelEventArgs<TItem> where TItem : class
	{
		/// <summary>
		/// Initializes a new instance of the TreeNodeBeforeEditEventArgs class.
		/// </summary>
		/// <param name="node">The node the event relates to.</param>
		public TreeNodeBeforeEditEventArgs(TreeNode<TItem> node)
			: base(node)
		{
		}
	}

	/// <summary>
	/// The TreeNodeAfterEditEventArgs class provides details of a completed edit and allows for cancellation.
	/// </summary>
	public class TreeNodeAfterEditEventArgs<TItem> : TreeNodeCancelEventArgs<TItem> where TItem : class
	{
		/// <summary>
		/// Initializes a new instance of the TreeNodeBeforeEditEventArgs class.
		/// </summary>
		/// <param name="node">The node the event relates to.</param>
		public TreeNodeAfterEditEventArgs(TreeNode<TItem> node, string oldValue, string newValue)
			: base(node)
		{
			OldValue = oldValue;
			NewValue = newValue;
		}

		/// <summary>
		/// Gets the old value.
		/// </summary>
		public string OldValue { get; }

		/// <summary>
		/// Gets or sets the new value.
		/// </summary>
		public string NewValue { get; set; }
	}
}
