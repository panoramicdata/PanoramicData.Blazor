using PanoramicData.Blazor.Helpers;

namespace PanoramicData.Blazor;

/// <summary>
/// Represents a tree component for displaying hierarchical data with support for selection, editing, drag-and-drop, and on-demand loading.
/// </summary>
/// <typeparam name="TItem">The type of the data item associated with each tree node.</typeparam>
public partial class PDTree<TItem> : IDisposable where TItem : class
{
    private const string _idPrefix = "pd-tree-";
    private IJSObjectReference? _commonModule;

    #region Injected Parameters

    /// <summary>
    /// Gets or sets the JavaScript runtime for JS interop.
    /// </summary>
    [Inject] public IJSRuntime JSRuntime { get; set; } = null!;

    /// <summary>
    /// Gets or sets the block overlay service for displaying overlays during async operations.
    /// </summary>
    [Inject] protected IBlockOverlayService BlockOverlayService { get; set; } = null!;

    #endregion

    #region Cascading Parameters

    /// <summary>
    /// Provides access to the parent <see cref="PDDragContext"/> if it exists.
    /// </summary>
    [CascadingParameter] public PDDragContext? DragContext { get; set; }

    #endregion

    #region Parameters

    /// <summary>
    /// Callback fired after a node edit ends.
    /// </summary>
    [Parameter] public EventCallback<TreeNodeAfterEditEventArgs<TItem>> AfterEdit { get; set; }

    /// <summary>
    /// Gets or sets whether nodes may be dragged.
    /// </summary>
    [Parameter] public bool AllowDrag { get; set; }

    /// <summary>
    /// Gets or sets whether items may be dropped onto nodes.
    /// </summary>
    [Parameter] public bool AllowDrop { get; set; }

    /// <summary>
    /// Gets or sets whether nodes can be dropped before or after other nodes.
    /// </summary>
    [Parameter] public bool AllowDropInBetween { get; set; }

    /// <summary>
    /// Gets or sets whether node edit is allowed.
    /// </summary>
    [Parameter] public bool AllowEdit { get; set; }

    /// <summary>
    /// Gets or sets whether selection is allowed.
    /// </summary>
    [Parameter] public bool AllowSelection { get; set; }

    /// <summary>
    /// Callback fired before a node edit begins.
    /// </summary>
    [Parameter] public EventCallback<TreeNodeBeforeEditEventArgs<TItem>> BeforeEdit { get; set; }

    /// <summary>
    /// Gets or sets an event callback raised just before the selection changes.
    /// </summary>
    [Parameter] public EventCallback<TreeBeforeSelectionChangeEventArgs<TItem>> BeforeSelectionChange { get; set; }

    /// <summary>
    /// Should a node clear its child content on collapse? Doing so will force a re-load of child nodes
    /// if it is re-expanded. Only applicable when LoadOnDemand = true.
    /// </summary>
    [Parameter] public bool ClearOnCollapse { get; set; }

    /// <summary>
    /// Gets or sets the <see cref="IDataProviderService{TItem}"/> instance to use to fetch data.
    /// </summary>
    [Parameter] public IDataProviderService<TItem> DataProvider { get; set; } = null!;

    /// <summary>
    /// Callback fired whenever a drag operation ends on a node within the tree.
    /// </summary>
    [Parameter] public EventCallback<DropEventArgs> Drop { get; set; }

    /// <summary>
    /// Optional predicate that determines whether dropping the current drag payload onto a given node is valid.
    /// Return <c>true</c> for a valid drop (green highlight), <c>false</c> for an invalid drop (red highlight).
    /// When <c>null</c>, all drops are treated as valid.
    /// </summary>
    [Parameter] public Func<TItem, object?, bool>? IsDropValid { get; set; }

    /// <summary>
    /// Gets or sets a delegate to be called if an exception occurs.
    /// </summary>
    [Parameter] public EventCallback<Exception> ExceptionHandler { get; set; }

    /// <summary>
    /// Predicate used to determine whether a node should be expanded when ExpandAll is called.
    /// </summary>
    [Parameter] public Predicate<TreeNode<TItem>>? ExpandOnExpandAll { get; set; }

    /// <summary>
    /// A function that calculates the CSS classes used to show an icon for the given node.
    /// </summary>
    [Parameter] public Func<TItem, int, string>? IconCssClass { get; set; }

    /// <summary>
    /// A function that determines whether the given item is a leaf in the tree.
    /// </summary>
    [Parameter] public Func<TItem, bool>? IsLeaf { get; set; }

    /// <summary>
    /// Callback fired whenever data items are loaded.
    /// </summary>
    /// <remarks>The callback allows the items to be modified by the calling application.</remarks>
    [Parameter] public EventCallback<List<TItem>> ItemsLoaded { get; set; }

    /// <summary>
    /// Callback fired whenever the user presses a key down.
    /// </summary>
    [Parameter] public EventCallback<KeyboardEventArgs> KeyDown { get; set; }

    /// <summary>
    /// A function that selects the field that contains the key value.
    /// </summary>
    [Parameter] public Func<TItem, object>? KeyField { get; set; }

    /// <summary>
    /// Gets or sets whether a non-leaf node will request data where necessary.
    /// </summary>
    [Parameter] public bool LoadOnDemand { get; set; }

    /// <summary>
    /// Callback fired whenever the user collapses a node.
    /// </summary>
    [Parameter] public EventCallback<TreeNode<TItem>> NodeCollapsed { get; set; }

    /// <summary>
    /// Callback fired whenever the user expands a node.
    /// </summary>
    [Parameter] public EventCallback<TreeNode<TItem>> NodeExpanded { get; set; }

    /// <summary>
    /// Gets or sets the template to render for each node.
    /// </summary>
    [Parameter] public RenderFragment<TreeNode<TItem>>? NodeTemplate { get; set; }

    /// <summary>
    /// Callback fired whenever a tree node is updated.
    /// </summary>
    [Parameter] public EventCallback<TreeNode<TItem>> NodeUpdated { get; set; }

    /// <summary>
    /// A function that selects the field that contains the parent key value.
    /// </summary>
    [Parameter] public Func<TItem, object>? ParentKeyField { get; set; }

    /// <summary>
    /// Gets or sets an event callback raised when the component has performed all its initialization.
    /// </summary>
    [Parameter] public EventCallback Ready { get; set; }

    /// <summary>
    /// Gets or sets whether right clicking on an item selects it.
    /// </summary>
    [Parameter] public bool RightClickSelectsItem { get; set; } = true;

    /// <summary>
    /// Gets or sets whether <see cref="RefreshAsync"/> raises <see cref="SelectionChange"/> for a selected node that
    /// survives the refresh. True by default, matching earlier versions, where a refresh always re-announced the
    /// selection; a consumer that reloads a detail view on that event keeps working. Set false for an
    /// auto-refreshing tree, so the selection is not re-announced (and the detail view not reloaded) on every
    /// refresh. A selected node that is removed by the refresh is always announced (issue #152).
    /// </summary>
    [Parameter] public bool RaiseSelectionChangeOnRefresh { get; set; } = true;

    /// <summary>
    /// Gets or sets an event callback raised whenever the selection changes.
    /// </summary>
    [Parameter] public EventCallback<TreeNode<TItem>> SelectionChange { get; set; }

    /// <summary>
    /// Gets or sets whether expanded nodes should show lines to help identify nested levels.
    /// </summary>
    [Parameter] public bool ShowLines { get; set; }

    /// <summary>
    /// Gets or sets whether the root node is displayed.
    /// </summary>
    [Parameter] public bool ShowRoot { get; set; } = true;

    /// <summary>
    /// A function used to determine sort order of child nodes.
    /// </summary>
    [Parameter] public Comparison<TItem>? Sort { get; set; }

    /// <summary>
    /// A function that selects the field to display for the item.
    /// </summary>
    [Parameter] public Func<TItem, object>? TextField { get; set; }

    /// <summary>
    /// A function that returns the tool tip text for a node.
    /// </summary>
    [Parameter] public Func<TItem, string>? ToolTip { get; set; }

    #endregion

    /// <summary>
    /// Gets the unique identifier of this tree.
    /// </summary>
    public string Id { get; private set; } = string.Empty;

    /// <summary>
    /// Gets or sets the root <see cref="TreeNode{TItem}"/> instance.
    /// </summary>
    public TreeNode<TItem> RootNode { get; set; } = new TreeNode<TItem> { Text = "Root" };

    /// <summary>
    /// Gets the currently selected tree node.
    /// </summary>
    public TreeNode<TItem>? SelectedNode { get; private set; }

    /// <summary>
    /// Scrolls the node with the specified ID into view using JavaScript interop.
    /// </summary>
    /// <param name="nodeId">The ID of the node to scroll into view.</param>
    public async Task ScrollNodeIntoViewAsync(string nodeId)
    {
        _commonModule ??= await JSRuntime.InvokeAsync<IJSObjectReference>("import", JSInteropVersionHelper.CommonJsUrl);

        await _commonModule.InvokeVoidAsync("scrollIntoView", nodeId);
    }

	/// <summary>
	/// Scrolls the node into view using JavaScript interop.
	/// </summary>
	/// <param name="node">The node to scroll into view.</param>
	public async Task ScrollNodeIntoViewAsync(TreeNode<TItem> node)
	{
		await ScrollNodeIntoViewAsync($"tree-node-{node.Id}");
	}


    /// <summary>
    /// Called when the component is initialized. Sets the unique tree ID.
    /// </summary>
    protected override void OnInitialized()
    {
        Id = $"{_idPrefix}{ComponentIdSequence.Next()}";
    }

    /// <summary>
    /// Called after the component has rendered. Loads initial data and notifies listeners on first render.
    /// </summary>
    /// <param name="firstRender">True if this is the first render.</param>
    /// <returns>A task that represents the asynchronous operation.</returns>
    protected async override Task OnAfterRenderAsync(bool firstRender)
    {
        if (firstRender && JSRuntime is not null)
        {
            try
            {
                _commonModule = await JSRuntime.InvokeAsync<IJSObjectReference>("import", JSInteropVersionHelper.CommonJsUrl);

                // build initial model and notify listeners
                var items = await GetDataAsync().ConfigureAwait(true);
                UpdateModel(items);

                // notify that node updated
                await NodeUpdated.InvokeAsync(RootNode).ConfigureAwait(true);

                // notify that initialization completed
                await Ready.InvokeAsync(null).ConfigureAwait(true);
                StateHasChanged();
            }
            catch
            {
                // BC-40 - fast page switching in Server Side blazor can lead to OnAfterRender call after page / objects disposed
            }
        }
    }

    /// <summary>
    /// Called when component parameters are set. Validates required parameters.
    /// </summary>
    protected override void OnParametersSet()
    {
        if (KeyField == null)
        {
            throw new PDTreeException("KeyField attribute is required.");
        }

        if (ParentKeyField == null)
        {
            throw new PDTreeException("ParentKeyField attribute is required.");
        }
    }


    /// <summary>
    /// Handles drop events for drag-and-drop operations within the tree.
    /// </summary>
    /// <param name="args">The drop event arguments.</param>
    /// <returns>A task that represents the asynchronous operation.</returns>
    private async Task OnDrop(DropEventArgs args)
    {
        if (args.Target is TreeNode<TItem>)
        {
            await Drop.InvokeAsync(args).ConfigureAwait(true);
        }
    }

    /// <summary>
    /// Disposes of the component resources.
    /// </summary>
    public void Dispose()
    {
        _clickTimer?.Dispose();
        GC.SuppressFinalize(this);
    }
}
