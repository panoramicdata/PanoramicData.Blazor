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
			await SelectEditTextAsync(Node).ConfigureAwait(true);
			await InitDragImageAsync(Node).ConfigureAwait(true);
		}
	}

	/// <summary>
	/// Focuses and selects the text in the edit box after it is first rendered.
	/// </summary>
	/// <param name="node">The rendered node.</param>
	private async Task SelectEditTextAsync(TreeNode<TItem> node)
	{
		if (node.IsEditing && node.BeginEditEvent.WaitOne(0))
		{
			if (_commonModule != null)
			{
				await _commonModule.InvokeVoidAsync("selectText", $"PDTNE{node.Id}", 0, node.Text.Length).ConfigureAwait(true);
			}

			node.BeginEditEvent.Reset();
		}
	}

	/// <summary>
	/// Fix #38: registers a native dragstart listener so that setDragImage can be called on the real
	/// dataTransfer object (Blazor DragEventArgs doesn't carry it).
	/// </summary>
	/// <param name="node">The rendered node.</param>
	private async Task InitDragImageAsync(TreeNode<TItem> node)
	{
		if (AllowDrag && _commonModule != null)
		{
			await _commonModule.InvokeVoidAsync("initDragImage", $"pdtnc-{node.Id}").ConfigureAwait(true);
		}
	}

	private void OnContentMouseDown(MouseEventArgs args)
	{
		if (Node != null)
		{
			Tree.NodeMouseDown(Node, args);
		}
	}

	private void OnDragStart()
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

	private void OnDragEnter()
	{
		if (AllowDrop)
		{
			_dragOverCount++;
			StateHasChanged();
		}
	}

	private void OnDragLeave()
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
