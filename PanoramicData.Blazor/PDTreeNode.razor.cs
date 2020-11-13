using System.Threading.Tasks;
using System.Collections.Generic;
using Microsoft.JSInterop;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;

namespace PanoramicData.Blazor
{
	public partial class PDTreeNode<TItem> where TItem : class
	{
		[Inject] public IJSRuntime? JSRuntime { get; set; }

		/// <summary>
		/// The parent PDTable instance.
		/// </summary>
		[CascadingParameter(Name = "Tree")]
		public PDTree<TItem> Tree { get; set; } = null!;

		/// <summary>
		/// Provides access to the parent DragContext if it exists.
		/// </summary>
		[CascadingParameter] public PDDragContext? DragContext { get; set; }

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
		[Parameter] public RenderFragment<TreeNode<TItem>>? NodeTemplate { get; set; }

		/// <summary>
		/// Event raised at the end of an edit.
		/// </summary>
		[Parameter] public EventCallback EndEdit { get; set; }

		/// <summary>
		/// Event raised whenever a key down event is generated on the tree node.
		/// </summary>
		[Parameter] public EventCallback<KeyboardEventArgs> KeyDown { get; set; }

		/// <summary>
		/// Gets or sets whether the node may be dragged.
		/// </summary>
		[Parameter] public bool AllowDrag { get; set; }

		/// <summary>
		/// Gets or sets whether items may be dropped onto the node.
		/// </summary>
		[Parameter] public bool AllowDrop { get; set; }

		/// <summary>
		/// Callback fired whenever a drag operation ends on a node within a DragContext.
		/// </summary>
		[Parameter] public EventCallback<DropEventArgs> Drop { get; set; }

		protected async override Task OnAfterRenderAsync(bool firstRender)
		{
			if (Node != null)
			{
				// focus and select text in edit box after first rendered
				if (Node.IsEditing && Node.BeginEditEvent.WaitOne(0))
				{
					await JSRuntime.InvokeVoidAsync("panoramicData.selectText", $"PDTNE{Node.Id}", 0, Node.Text.Length).ConfigureAwait(true);
					Node.BeginEditEvent.Reset();
				}
			}
		}

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

		private async Task OnEndEdit()
		{
			await EndEdit.InvokeAsync(null).ConfigureAwait(true);
		}

		private Dictionary<string, object> NodeAttributes
		{
			get
			{
				var dict = new Dictionary<string, object>();
				if (AllowDrag)
				{
					dict.Add("draggable", "true");
				}
				if (AllowDrop)
				{
					dict.Add("ondragover", "event.preventDefault();");
				}
				return dict;
			}
		}

		private async Task OnDragDrop(MouseEventArgs args)
		{
			if (DragContext != null && Node != null)
			{
				await Drop.InvokeAsync(new DropEventArgs(Node, DragContext.Payload, args.CtrlKey)).ConfigureAwait(true);
			}
		}

		private async Task OnDrop(DropEventArgs args)
		{
			await Drop.InvokeAsync(args).ConfigureAwait(true);
		}
	}
}
