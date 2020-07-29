using System;
using System.Collections.Generic;
using System.Linq;
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

		internal string EditText { get; set; } = string.Empty;

		internal ManualResetEvent BeginEditEvent { get; set; } = new ManualResetEvent(false);

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
		/// Gets whether this node is currently being edited.
		/// </summary>
		public bool IsEditing { get; private set; }

		/// <summary>
		/// Gets a list of child nodes for this node.
		/// </summary>
		/// <Remarks>When IsLeaf is false and this property is null then it is considered unloaded.
		/// This allows the concept of on-demand / lazy loading of sub-nodes.</Remarks>
		public List<TreeNode<T>>? Nodes { get; set; }

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

		public override string ToString()
		{
			return $"{Key}";
		}

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

		/// <summary>
		/// Traverse the tree from this node in an attempt to find the next visible node.
		/// </summary>
		/// <returns>The next visible node otherwise null.</returns>
		public TreeNode<T>? GetNext()
		{
			// descend to first child node?
			if (IsExpanded && Nodes?.Count > 0)
			{
				return Nodes[0];
			}

			// traverse back up tree attempting to find next node
			var childNode = this;
			var parentNode = this.ParentNode;
			while (parentNode != null)
			{
				// can move to sibling?
				if (parentNode.Nodes != null)
				{
					var i = parentNode.Nodes.IndexOf(childNode);
					if (i > -1 && i < parentNode.Nodes.Count - 1)
					{
						return parentNode.Nodes[i + 1];
					}
				}

				// move up to parent and retry
				childNode = parentNode;
				parentNode = parentNode.ParentNode;
			}

			return null;
		}

		/// <summary>
		/// Traverse the tree from this node in an attempt to find the previous visible node.
		/// </summary>
		/// <returns>The previous visible node otherwise null.</returns>
		public TreeNode<T>? GetPrevious()
		{
			// declare a recursive local function that will return the further visible child
			static TreeNode<T> Descend(TreeNode<T> node)
			{
				if (node.IsExpanded && node.Nodes?.Count > 0)
				{
					return Descend(node.Nodes[node.Nodes.Count - 1]);
				}
				return node;
			}

			// get previous sibling
			if (ParentNode?.Nodes != null)
			{
				var i = ParentNode.Nodes.IndexOf(this);

				// if first child then move to parent
				if(i == 0)
				{
					return ParentNode;
				}
				if (i > 0)
				{
					// descend sibling as far as possible
					return Descend(ParentNode.Nodes[i - 1]);
				}
			}
			return null;
		}
	}
}