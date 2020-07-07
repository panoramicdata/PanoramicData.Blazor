using Microsoft.AspNetCore.Components;

namespace PanoramicData.Blazor
{
	public partial class PDTreeNode
    {
		/// <summary>
		/// Gets or sets the TreeNode to be rendered.
		/// </summary>
		[Parameter] public TreeNode Node { get; set; }

		/// <summary>
		/// Gets or sets whether the node when expanded, should show a line to help identify its boundary.
		/// </summary>
		[Parameter] public bool ShowLines { get; set; }
	}
}
