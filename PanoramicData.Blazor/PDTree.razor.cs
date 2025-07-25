﻿namespace PanoramicData.Blazor;

/// <summary>
/// Represents a tree component for displaying hierarchical data with support for selection, editing, drag-and-drop, and on-demand loading.
/// </summary>
/// <typeparam name="TItem">The type of the data item associated with each tree node.</typeparam>
public partial class PDTree<TItem> where TItem : class
{
    private const string _idPrefix = "pd-tree-";
    private IJSObjectReference? _commonModule;
    private static int _idSequence;
    private int _clickCount;
    private Timer? _clickTimer;
    private TreeNode<TItem>? _clickedNode;

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
    /// Expands all the branch nodes in the tree.
    /// </summary>
    public void ExpandAll() => RootNode.Walk((n) => { n.IsExpanded = !n.Isleaf; return true; });

    /// <summary>
    /// Expands all the branch nodes in the tree asynchronously.
    /// </summary>
    /// <returns>A task that represents the asynchronous operation.</returns>
    public async Task ExpandAllAsync() => await RootNode.WalkAsync(async (n) =>
  {
      if (!n.IsExpanded && !n.Isleaf)
      {
          if (ExpandOnExpandAll == null || ExpandOnExpandAll(n))
          {
              await ToggleNodeIsExpandedAsync(n).ConfigureAwait(true);
          }
      }

      return true;
  }).ConfigureAwait(true);

    /// <summary>
    /// Collapses all the branch nodes in the tree.
    /// </summary>
    public void CollapseAll() => RootNode.Walk((n) => { n.IsExpanded = false; return true; });

    /// <summary>
    /// Searches all nodes until the given criteria is first matched.
    /// </summary>
    /// <param name="predicate">The predicate to match nodes.</param>
    /// <returns>The first matching <see cref="TreeNode{TItem}"/>, or null if not found.</returns>
    public TreeNode<TItem>? Search(Predicate<TreeNode<TItem>> predicate)
    {
        TreeNode<TItem>? found = null;
        RootNode.Walk((n) =>
        {
            if (predicate(n))
            {
                found = n;
                return false;
            }

            return true;
        });
        return found;
    }

    /// <summary>
    /// Handles the click timer callback for distinguishing between single and double clicks.
    /// </summary>
    /// <param name="state">The state object, typically a <see cref="MouseEventArgs"/>.</param>
    private void ClickTimerCallback(object? state)
    {
        _clickTimer?.Dispose();
        _clickTimer = null;

        InvokeAsync(async () =>
        {
            if (_clickedNode != null)
            {
                if (_clickCount > 1)
                {
                    await SelectNode(_clickedNode, false).ConfigureAwait(true);
                    await ToggleNodeIsExpandedAsync(_clickedNode).ConfigureAwait(true);
                }
                else
                {
                    var autoEdit = state is MouseEventArgs args && args.Button == 0;
                    await SelectNode(_clickedNode, autoEdit).ConfigureAwait(true);
                }

                StateHasChanged();
            }
        });

        _clickCount = 0;
    }

    /// <summary>
    /// Handles a double-click event on a node.
    /// </summary>
    /// <param name="node">The node that was double-clicked.</param>
    /// <param name="args">The mouse event arguments.</param>
    public async Task NodeDoubleClick(TreeNode<TItem> node, MouseEventArgs args)
    {
        await SelectNode(node, false).ConfigureAwait(true);
        await ToggleNodeIsExpandedAsync(node).ConfigureAwait(true);
    }

    /// <summary>
    /// Handles the mouse down event on a node, supporting click and double-click logic.
    /// </summary>
    /// <param name="node">The node that was clicked.</param>
    /// <param name="args">The mouse event arguments.</param>
    public void NodeMouseDown(TreeNode<TItem> node, MouseEventArgs args)
    {
        // ignore right button?
        if (args.Button == 2 && !RightClickSelectsItem)
        {
            return;
        }

        if (_clickTimer == null || node != _clickedNode)
        {
            _clickTimer?.Dispose();
            _clickedNode = node;
            _clickCount = 1;
            _clickTimer = new Timer(ClickTimerCallback, args, 250, Timeout.Infinite);
        }
        else
        {
            _clickCount++;
        }
    }

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
	/// Selects the given node.
	/// </summary>
	/// <param name="node">The node to select.</param>
	/// <param name="autoEdit">If the same node is selected twice should it go into edit mode?</param>
	public async Task SelectNode(TreeNode<TItem> node)
        => await SelectNode(node, true);

    /// <summary>
    /// Selects the given node, optionally entering edit mode if the same node is selected twice.
    /// </summary>
    /// <param name="node">The node to select.</param>
    /// <param name="autoEdit">If true, enters edit mode if the same node is selected twice.</param>
    public async Task SelectNode(TreeNode<TItem> node, bool autoEdit)
    {
        if (AllowSelection)
        {
            // end edit mode
            if (SelectedNode != null)
            {
                await CommitEdit().ConfigureAwait(true);
            }

            // select new node
            if (SelectedNode == node)
            {
                // if the same node
                if (AllowEdit && autoEdit)
                {
                    await BeginEdit().ConfigureAwait(true);
                }
            }
            else
            {
                // allow app to pre-process or cancel change
                var beforeEventArgs = new TreeBeforeSelectionChangeEventArgs<TItem>(node, SelectedNode);
                await BeforeSelectionChange.InvokeAsync(beforeEventArgs).ConfigureAwait(true);
                if (beforeEventArgs.Cancel)
                {
                    return;
                }

                if (SelectedNode != null)
                {
                    SelectedNode.IsSelected = false;
                }

                SelectedNode = node;
                SelectedNode.IsSelected = true;

                // ensure all parent nodes are expanded
                var parentNode = SelectedNode.ParentNode;
                while (parentNode != null)
                {
                    parentNode.IsExpanded = true;
                    parentNode = parentNode.ParentNode;
                }

                // notify of change
                await SelectionChange.InvokeAsync(SelectedNode).ConfigureAwait(true);
                StateHasChanged();
            }
        }
    }

    /// <summary>
    /// Toggles the given node's expanded state, loading children if necessary.
    /// </summary>
    /// <param name="node">The node to be expanded or collapsed.</param>
    public async Task ToggleNodeIsExpandedAsync(TreeNode<TItem> node)
    {
        var wasExpanded = node.IsExpanded;

        if (node.Isleaf)
        {
            return;
        }

        // if expanding and Nodes is null then request data
        if (!wasExpanded && node.Nodes == null)
        {
            // fetch direct child items
            var key = node.Data is null ? null : KeyField!(node.Data!).ToString();
            var items = await GetDataAsync(key).ConfigureAwait(true);
            // add new nodes to existing node
            node.Nodes = []; // indicates data fetched, even if no items returned
            UpdateModel(items);

            // notify any listeners that new data fetched
            await NodeUpdated.InvokeAsync(node).ConfigureAwait(true);
        }

        // expand / collapse and notify
        node.IsExpanded = !wasExpanded;
        if (wasExpanded)
        {
            if (LoadOnDemand && ClearOnCollapse)
            {
                node.Nodes = null;
            }

            await NodeCollapsed.InvokeAsync(node).ConfigureAwait(true);
        }
        else
        {
            await NodeExpanded.InvokeAsync(node).ConfigureAwait(true);
        }
    }

    /// <summary>
    /// Refreshes the entire tree.
    /// </summary>
    /// <remarks>Refreshes all expanded nodes, from top down. Will try to maintain selected node.</remarks>
    public async Task RefreshAsync()
    {
        // find selected node path
        var path = new Stack<string>();
        var node = SelectedNode;
        while (node != null && node != RootNode)
        {
            path.Push(node.Key);
            node = node.ParentNode;
        }

        // node should now be root node
        if (node == RootNode)
        {
            // refresh from top down
            while (path.Count > 0)
            {
                // find previous selected node
                var key = path.Pop();
                var nextNode = node.Nodes?.FirstOrDefault(x => x.Key == key);
                if (nextNode is null)
                {
                    break;
                }

                node = nextNode;

                // refresh node
                await RefreshNodeAsync(node).ConfigureAwait(true);
            }

            // re-select last refreshed node
            await SelectNode(node).ConfigureAwait(true);
        }
    }

    /// <summary>
    /// Recursively refreshes all expanded child nodes of the specified node.
    /// </summary>
    /// <param name="node">The node whose children to refresh.</param>
    private async Task RefreshRecurse(TreeNode<TItem> node)
    {
        // refresh each expanded child node
        if (node?.Nodes != null)
        {
            foreach (var childNode in node.Nodes.Where(x => !x.Isleaf && x.IsExpanded))
            {
                // collaspe, refresh node and re-expand
                await RefreshNodeAsync(childNode).ConfigureAwait(true);
                childNode.IsExpanded = true;

                // recurse down tree
                await RefreshRecurse(childNode).ConfigureAwait(true);
            }
        }
    }

    /// <summary>
    /// Refreshes the given node, re-loading sub-nodes if applicable.
    /// </summary>
    /// <param name="node">The node to refresh.</param>
    public async Task RefreshNodeAsync(TreeNode<TItem> node)
    {
        node.IsExpanded = false;
        node.Nodes = null;
        await ToggleNodeIsExpandedAsync(node).ConfigureAwait(true);
    }

    /// <summary>
    /// Requests to remove the specified node from the tree.
    /// </summary>
    /// <param name="node">The node to be removed.</param>
    public async Task RemoveNodeAsync(TreeNode<TItem> node)
    {
        if (node?.ParentNode?.Nodes != null)
        {
            // remove node from parent and select parent
            node.ParentNode.Nodes.Remove(node);
            await SelectNode(node.ParentNode).ConfigureAwait(true);
        }
    }

    /// <summary>
    /// Places the currently selected node into edit mode.
    /// </summary>
    public async Task BeginEdit()
    {
        if (AllowEdit && SelectedNode != null)
        {
            // commit any other edits
            RootNode.Walk((x) => { x.CommitEdit(); return true; });

            // notify and allow cancel
            var args = new TreeNodeBeforeEditEventArgs<TItem>(SelectedNode);
            await BeforeEdit.InvokeAsync(args).ConfigureAwait(true);
            if (!args.Cancel)
            {
                SelectedNode.BeginEdit();
                StateHasChanged();
            }
        }
    }

    /// <summary>
    /// Saves the current edit.
    /// </summary>
    public async Task CommitEdit()
    {
        if (SelectedNode?.IsEditing == true)
        {
            if (string.IsNullOrWhiteSpace(SelectedNode.EditText))
            {
                SelectedNode.CancelEdit();
                await ExceptionHandler.InvokeAsync(new PDTreeException("A value is required")).ConfigureAwait(true);
            }
            else
            {
                // notify and allow cancel
                var afterEditArgs = new TreeNodeAfterEditEventArgs<TItem>(SelectedNode, SelectedNode.Text, SelectedNode.EditText);
                await AfterEdit.InvokeAsync(afterEditArgs).ConfigureAwait(true);
                if (afterEditArgs.Cancel)
                {
                    SelectedNode.CancelEdit();
                }
                else
                {
                    SelectedNode.EditText = afterEditArgs.NewValue; // application might of altered
                    SelectedNode.CommitEdit();
                    SelectedNode?.ParentNode?.Nodes?.Sort(NodeSort); // re-sort parent
                    if (_commonModule != null)
                    {
                        await _commonModule.InvokeVoidAsync("focus", Id).ConfigureAwait(true);
                    }
                }
            }
        }
    }

    /// <summary>
    /// Cancels the current edit.
    /// </summary>
    public void CancelEdit()
    {
        if (SelectedNode?.IsEditing == true)
        {
            SelectedNode.CancelEdit();
        }
    }

    /// <summary>
    /// Retrieves data items from the data provider, optionally for a specific parent key.
    /// </summary>
    /// <param name="key">The parent key to fetch child items for, or null for root items.</param>
    /// <returns>An enumerable of data items.</returns>
    private async Task<IEnumerable<TItem>> GetDataAsync(string? key = null)
    {
        try
        {
            if (DataProvider is null)
            {
                return [];
            }

            var request = new DataRequest<TItem>
            {
                Skip = 0,
                ForceUpdate = false,
                // if load on demand and item key is given then only fetch immediate child items
                SearchText = LoadOnDemand ? key ?? string.Empty : null
            };

            // perform query data
            var response = await DataProvider
                .GetDataAsync(request, CancellationToken.None)
                .ConfigureAwait(true);

            // allow calling application to filter/add items etc
            var items = new List<TItem>(response.Items);
            await ItemsLoaded.InvokeAsync(items).ConfigureAwait(true);

            return items;
        }
        catch (Exception ex)
        {
            await ExceptionHandler.InvokeAsync(ex).ConfigureAwait(true);
            return [];
        }
    }

    /// <summary>
    /// Updates the tree model with the specified items, creating or updating nodes as needed.
    /// </summary>
    /// <param name="items">The items to update the model with.</param>
    private void UpdateModel(IEnumerable<TItem> items)
    {
        var modifiedNodes = new List<TreeNode<TItem>>();
        var dict = new Dictionary<string, TreeNode<TItem>>();

        foreach (var item in items)
        {
            // get key value and check is given and unique
            var key = KeyField!(item)?.ToString();
            if (key is null || string.IsNullOrEmpty(key))
            {
                throw new PDTreeException("Items must supply a key value.");
            }

            // get parent key and find parent node
            var parentKey = ParentKeyField?.Invoke(item)?.ToString();
            TreeNode<TItem>? parentNode = null;
            if (parentKey == null || string.IsNullOrWhiteSpace(parentKey))
            {
                parentNode = RootNode;
            }
            else
            {
                parentNode = (dict.TryGetValue(parentKey, out TreeNode<TItem>? value)) ? parentNode = value : RootNode.Find(parentKey);
            }

            if (parentNode == null)
            {
                throw new PDTreeException($"A parent item with key '{parentKey}' could not be found");
            }

            // does the node already exist?
            var node = parentNode.Find(key);
            if (node is null)
            {
                node = new TreeNode<TItem>();
                // add to parent node and mark parent node for re-sort
                (parentNode.Nodes ??= []).Add(node);
                if (!modifiedNodes.Contains(parentNode))
                {
                    modifiedNodes.Add(parentNode);
                }
            }

            node.Key = key;
            node.Text = TextField is null
                ? item?.ToString() ?? string.Empty
                : TextField.Invoke(item).ToString() ?? item.ToString() ?? string.Empty;
            node.IsExpanded = false;
            node.Data = item;
            node.ParentNode = parentNode;
            node.Level = parentNode.Level + 1;
            node.IconCssClass = IconCssClass is null || item is null
                ? string.Empty
                : IconCssClass.Invoke(item, parentNode.Level + 1);
            node.Nodes = LoadOnDemand ? null : [];
            if (LoadOnDemand && IsLeaf != null && item != null && IsLeaf(item))
            {
                node.Nodes = [];
            }

            // cache node for performance
            dict.Add(key, node);
        }

        // re-apply sorts where necessary
        foreach (var node in modifiedNodes)
        {
            node?.Nodes?.Sort(NodeSort);
        }
    }

    /// <summary>
    /// Compares two nodes for sorting, using the <see cref="Sort"/> function if provided.
    /// </summary>
    /// <param name="a">The first node.</param>
    /// <param name="b">The second node.</param>
    /// <returns>An integer indicating the sort order.</returns>
    private int NodeSort(TreeNode<TItem> a, TreeNode<TItem> b)
    {
        if (Sort is null || a.Data is null || b.Data is null)
        {
            return a.Text.CompareTo(b.Text);
        }
        else
        {
            return Sort(a.Data, b.Data);
        }
    }

    /// <summary>
    /// Called when the component is initialized. Sets the unique tree ID.
    /// </summary>
    protected override void OnInitialized()
    {
        Id = $"{_idPrefix}{++_idSequence}";
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
    /// Handles the end of an edit operation, committing the edit and refocusing the tree.
    /// </summary>
    /// <returns>A task that represents the asynchronous operation.</returns>
    private async Task OnEndEdit()
    {
        // end any current edit
        await CommitEdit().ConfigureAwait(true);
        // re-focus the tree
        if (_commonModule != null)
        {
            await _commonModule.InvokeVoidAsync("focus", Id).ConfigureAwait(true);
        }
    }

    /// <summary>
    /// Handles key down events for navigation and editing within the tree.
    /// </summary>
    /// <param name="args">The keyboard event arguments.</param>
    /// <returns>A task that represents the asynchronous operation.</returns>
    private async Task OnKeyDown(KeyboardEventArgs args)
    {
        switch (args.Code)
        {
            case "Escape":
                CancelEdit();
                break;

            case "Enter":
            case "Return":
                await CommitEdit().ConfigureAwait(true);
                break;
        }

        if (SelectedNode?.IsEditing == false)
        {
            switch (args.Code)
            {
                case "F2":
                    await BeginEdit().ConfigureAwait(true);
                    break;

                case "ArrowRight":
                    if (!SelectedNode.IsExpanded)
                    {
                        await ToggleNodeIsExpandedAsync(SelectedNode).ConfigureAwait(true);
                    }

                    break;

                case "ArrowLeft":
                    if (SelectedNode.IsExpanded)
                    {
                        await ToggleNodeIsExpandedAsync(SelectedNode).ConfigureAwait(true);
                    }

                    break;

                case "ArrowDown":
                    var nextNode = SelectedNode.GetNext();
                    if (nextNode != null)
                    {
                        await SelectNode(nextNode).ConfigureAwait(true);
                    }

                    break;

                case "ArrowUp":
                    var prevNode = SelectedNode.GetPrevious();
                    if (prevNode != null)
                    {
                        // do not select previous node if that is the hidden root node
                        if (ShowRoot || prevNode.ParentNode != null)
                        {
                            await SelectNode(prevNode).ConfigureAwait(true);
                        }
                    }

                    break;
            }
        }

        await KeyDown.InvokeAsync(args).ConfigureAwait(true);
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
}