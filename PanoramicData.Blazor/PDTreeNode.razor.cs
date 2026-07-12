namespace PanoramicData.Blazor;

/// <summary>
/// A Blazor component that renders a single node within a <see cref="PDTree{TItem}"/> hierarchy.
/// </summary>
public partial class PDTreeNode<TItem> where TItem : class
{
	private IJSObjectReference? _commonModule;
	private int _dragOverCount;

	/// <summary>
	/// Gets the injected JavaScript runtime.
	/// </summary>
	[Inject] public IJSRuntime JSRuntime { get; set; } = null!;

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
	/// Provides access to the parent ContextMenu if it exists.
	/// </summary>
	[CascadingParameter] public PDContextMenu? ContextMenu { get; set; }

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
	/// Gets or sets whether nodes can be dropped before or after other nodes.
	/// </summary>
	[Parameter] public bool AllowDropInBetween { get; set; }

	/// <summary>
	/// Callback fired whenever a drag operation ends on a node within a DragContext.
	/// </summary>
	[Parameter] public EventCallback<DropEventArgs> Drop { get; set; }

	/// <inheritdoc />
	protected async override Task OnAfterRenderAsync(bool firstRender)
	{
		if (firstRender && JSRuntime is not null)
		{
			try
			{
				_commonModule = await JSRuntime.InvokeAsync<IJSObjectReference>("import", JSInteropVersionHelper.CommonJsUrl);
			}
			catch
			{
				// BC-40 - fast page switching in Server Side blazor can lead to OnAfterRender call after page / objects disposed
			}
		}

		if (Node != null)
		{
			// focus and select text in edit box after first rendered
			if (Node.IsEditing && Node.BeginEditEvent.WaitOne(0))
			{
				if (_commonModule != null)
				{
					await _commonModule.InvokeVoidAsync("selectText", $"PDTNE{Node.Id}", 0, Node.Text.Length).ConfigureAwait(true);
				}

				Node.BeginEditEvent.Reset();
			}

			// Fix #38: register a native dragstart listener so we can call setDragImage on
			// the real dataTransfer object (Blazor DragEventArgs doesn't carry it)
			if (AllowDrag && _commonModule != null)
			{
				await _commonModule.InvokeVoidAsync("initDragImage", $"pdtnc-{Node.Id}").ConfigureAwait(true);
			}
		}
	}

	private void OnContentMouseDown(MouseEventArgs args)
	{
		if (Node != null)
		{
			Tree.NodeMouseDown(Node, args);
		}
	}

	private void OnDragStart(DragEventArgs _)
	{
		// need to set the data being dragged
		if (DragContext != null && Node?.Data != null)
		{
			// get all selected items
			var items = new List<TItem>
			{
				Node.Data
			};
			DragContext.Payload = items;
		}
	}

	private async Task OnToggleExpandAsync()
	{
		if (Node != null)
		{
			await Tree.ToggleNodeIsExpandedAsync(Node).ConfigureAwait(true);
		}
	}

	private async Task OnEndEdit() => await EndEdit.InvokeAsync(null).ConfigureAwait(true);

	private static Dictionary<string, object> TreeAttributes
	{
		get
		{
			return [];
		}
	}

	private Dictionary<string, object> ContentAttributes
	{
		get
		{
			var dict = new Dictionary<string, object>();
			if (AllowDrag && Node?.IsEditing != true)
			{
				dict.Add("draggable", "true");
			}

			return dict;
		}
	}

	private bool IsDragOverValid => Node?.Data is null
		|| Tree?.IsDropValid is null
		|| Tree.IsDropValid(Node.Data, DragContext?.Payload);

	private void OnDragEnter(DragEventArgs _)
	{
		if (AllowDrop)
		{
			_dragOverCount++;
			StateHasChanged();
		}
	}

	private void OnDragLeave(DragEventArgs _)
	{
		if (AllowDrop)
		{
			_dragOverCount = Math.Max(0, _dragOverCount - 1);
			StateHasChanged();
		}
	}

	private async Task OnDragDrop(MouseEventArgs args)
	{
		_dragOverCount = 0;
		if (DragContext != null && Node != null && IsDragOverValid)
		{
			await Drop.InvokeAsync(new DropEventArgs(Node, DragContext.Payload, args.CtrlKey)).ConfigureAwait(true);
		}
	}

	private async Task OnDrop(DropEventArgs args) => await Drop.InvokeAsync(args).ConfigureAwait(true);

	private async Task OnSeparatorDrop(DropEventArgs args)
	{
		var newArgs = new DropEventArgs(Node, args.Payload, args.Ctrl, args.Before);
		await Drop.InvokeAsync(newArgs).ConfigureAwait(true);
	}
}
