using System;
using System.Collections.Generic;
using System.Threading;

namespace PanoramicData.Blazor
{
	/// <summary>
	/// The TreeNode class is used to describe a single node of a hierarchical data structure.
	/// </summary>
	public class TreeNode<T> where T : class
	{
		private static int _idSequence;

		internal int Id { get; }

		/// <summary>
		/// Gets or sets the unique identifier for this node.
		/// </summary>
		public string Key { get; set; } = string.Empty;

		/// <summary>
		/// Gets or sets the display text for this node.
		/// </summary>
		public virtual string Text { get; set; } = string.Empty;

		/// <summary>
		/// Gets the data item associated with this node.
		/// </summary>
		public T? Data { get; internal set; }

		/// <summary>
		/// Gets whether this node is a leaf node, i.e. does not / will not ever contain child nodes.
		/// </summary>
		public virtual bool Isleaf => Nodes?.Count == 0;

		/// <summary>
		/// Gets or sets whether this node is expanded or not. Only applied to branch nodes (non-leaf nodes).
		/// </summary>
		public bool IsExpanded { get; set; }

		/// <summary>
		/// Gets or sets whether this node is currently selected.
		/// </summary>
		public bool IsSelected { get; set; }

		/// <summary>
		/// Gets or sets this nodes parent.
		/// </summary>
		public TreeNode<T>? ParentNode { get; set; }

		/// <summary>
		/// Gets a list of child nodes for this node.
		/// </summary>
		/// <Remarks>When IsLeaf is false and this property is null then it is considered unloaded.
		/// This allows the concept of on-demand / lazy loading of sub-nodes.</Remarks>
		public List<TreeNode<T>>? Nodes { get; set; }

		internal string EditText { get; set; } = string.Empty;

		internal ManualResetEvent BeginEditEvent { get; set; } = new ManualResetEvent(false);

		/// <summary>
		/// Initializes a new instance of the TreeNode class.
		/// </summary>
		public TreeNode()
		{
			Id = ++_idSequence;
		}

		/// <summary>
		/// Attempts to find the node with the specified key.
		/// </summary>
		/// <param name="key">Key of the node to find.</param>
		/// <returns>If found the TreeNode instance otherwise null.</returns>
		public TreeNode<T>? Find(string key)
		{
			TreeNode<T>? node = null;
			Walk((n) =>
			{
				if (n.Key == key)
				{
					node = n;
					return false; // stop search
				}
				return true;
			});
			return node;
		}

		/// <summary>
		/// Function applied to this node and all child nodes or until the function returns false.
		/// </summary>
		/// <param name="fn">Function to be called for each node. Returns false to stop walking.</param>
		public bool Walk(Func<TreeNode<T>, bool> fn)
		{
			if (!fn(this))
			{
				return false;
			}
			else if (Nodes != null)
			{
				foreach (var subNode in Nodes)
				{
					if (!subNode.Walk(fn))
					{
						return false;
					}
				}
			}
			return true;
		}

		/// <summary>
		/// Gets whether this node is currently being edited.
		/// </summary>
		public bool IsEditing { get; private set; }

		internal void BeginEdit()
		{
			EditText = Text;
			IsEditing = true;
			BeginEditEvent.Set();
		}

		internal void CancelEdit()
		{
			if (IsEditing)
			{
				EditText = Text;
				IsEditing = false;
			}
		}

		internal void CommitEdit()
		{
			if (IsEditing)
			{
				// validate new text
				if (string.IsNullOrWhiteSpace(EditText))
				{
					CancelEdit();
				}
				else
				{
					Text = EditText;
					IsEditing = false;
				}
			}
		}
	}
}