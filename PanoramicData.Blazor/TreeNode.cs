using System;
using System.Collections.Generic;

namespace PanoramicData.Blazor
{
	/// <summary>
	/// The TreeNode class is used to describe a single node of a hierarchical data structure.
	/// </summary>
	public class TreeNode<T>
	{
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
		public T Data { get; internal set; }

		/// <summary>
		/// Gets whether this node is a leaf node, i.e. does not / will not ever contain child nodes.
		/// </summary>
		public virtual bool Isleaf => Nodes?.Count == 0;

		/// <summary>
		/// Gets or sets whether this node is expanded or not. Only applied to branch nodes (non-leaf nodes).
		/// </summary>
		public virtual bool IsExpanded { get; set; }

		/// <summary>
		/// Gets or sets whether this node is currently selected.
		/// </summary>
		public virtual bool IsSelected { get; set; }

		/// <summary>
		/// Gets or sets this nodes parent.
		/// </summary>
		public TreeNode<T>? ParentNode { get; set; }

		/// <summary>
		/// Gets a list of child nodes for this node.
		/// </summary>
		/// <Remarks>When IsLeaf is false and this property is null then it is considered unloaded.
		/// This allows the concept of on-demand / lazy loading of sub-nodes.</Remarks>
		public List<TreeNode<T>>? Nodes { get; set; } = new List<TreeNode<T>>(); // default is evaluated and no child nodes
	}
}