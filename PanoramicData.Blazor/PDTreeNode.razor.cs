using System.Threading.Tasks;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;

namespace PanoramicData.Blazor
{
	public partial class PDTreeNode<TItem> where TItem : class
    {
		/// <summary>
		/// The parent PDTable instance.
		/// </summary>
		[CascadingParameter(Name = "Tree")]
		public PDTree<TItem> Tree { get; set; } = null!;

		/// <summary>
		/// Gets or sets the TreeNode to be rendered.
		/// </summary>
		[Parameter] public TreeNode<TItem>? Node { get; set; }

		/// <summary>
		/// Gets or sets whether the node when expanded, should show a line to help identify its boundary.
		/// </summary>
		[Parameter] public bool ShowLines { get; set; }

		/// <summary>
		/// Gets or sets the template to render.
		/// </summary>
		[Parameter]
		public RenderFragment<TreeNode<TItem>>? NodeTemplate { get; set; }

		private async Task OnContentClickAsync()
		{
			if(Node != null)
			{
				await Tree.SelectNode(Node).ConfigureAwait(true);
			}
		}

		private async Task OnToggleExpandAsync()
		{
			if (Node != null)
			{
				await Tree.ToggleNodeIsExpandedAsync(Node).ConfigureAwait(true);
			}
		}

		private async Task ContextMenuHandler(MouseEventArgs args)
		{
		}
	}
}
