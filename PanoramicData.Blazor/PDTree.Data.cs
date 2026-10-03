namespace PanoramicData.Blazor;

/// <summary>
/// Data loading and model building of the <see cref="PDTree{TItem}"/>.
/// </summary>
public partial class PDTree<TItem>
{
	/// <summary>
	/// Refreshes the entire tree in place.
	/// </summary>
	/// <remarks>
	/// Fetched items are merged into the existing nodes by key (issue #152): a node whose key is still
	/// present keeps its object, its expanded state and its loaded children; new keys are added and gone
	/// keys removed. The selected node therefore stays selected (the same object). It is re-announced through
	/// <see cref="SelectionChange"/> only when <see cref="RaiseSelectionChangeOnRefresh"/> is true (the default). If it
	/// has gone, its nearest surviving ancestor is selected and the change raised once.
	/// With <see cref="LoadOnDemand"/>, every node whose children have been loaded is re-queried.
	/// </remarks>
	public async Task RefreshAsync()
	{
		// remember the selected node's ancestry, nearest first, in case it does not survive
		var selected = SelectedNode;
		var ancestorKeys = GetAncestorKeys(selected);

		if (LoadOnDemand)
		{
			await RefreshLoadedChildrenAsync(RootNode).ConfigureAwait(true);
		}
		else
		{
			var items = await GetDataAsync().ConfigureAwait(true);
			MergeModel(items, RootNode, wholeTree: true);
			await NodeUpdated.InvokeAsync(RootNode).ConfigureAwait(true);
		}

		if (selected != null)
		{
			await RestoreSelectionAsync(selected, ancestorKeys).ConfigureAwait(true);
		}

		StateHasChanged();
	}

	/// <summary>
	/// The keys of a node's ancestors below the root, nearest first.
	/// </summary>
	/// <param name="node">The node, or null for none.</param>
	private List<string> GetAncestorKeys(TreeNode<TItem>? node)
	{
		var ancestorKeys = new List<string>();
		for (var ancestor = node?.ParentNode; ancestor != null && ancestor != RootNode; ancestor = ancestor.ParentNode)
		{
			ancestorKeys.Add(ancestor.Key);
		}

		return ancestorKeys;
	}

	/// <summary>
	/// After a refresh, selects the nearest surviving ancestor of a selected node that has gone, or re-announces
	/// a selected node that survived when <see cref="RaiseSelectionChangeOnRefresh"/> is true.
	/// </summary>
	/// <param name="selected">The node selected before the refresh.</param>
	/// <param name="ancestorKeys">The keys of its ancestors before the refresh, nearest first.</param>
	private async Task RestoreSelectionAsync(TreeNode<TItem> selected, List<string> ancestorKeys)
	{
		if (RootNode.Find(selected.Key) != selected)
		{
			var survivor = ancestorKeys
				.Select(RootNode.Find)
				.FirstOrDefault(node => node != null) ?? RootNode;
			await SelectNode(survivor, false).ConfigureAwait(true);
		}
		else if (RaiseSelectionChangeOnRefresh)
		{
			// the same node object is still selected; re-announce it as earlier versions did
			await SelectionChange.InvokeAsync(selected).ConfigureAwait(true);
		}
	}

	/// <summary>
	/// Re-queries the children of a node loaded on demand, merges them in place, then does the same for
	/// every child whose own children have been loaded.
	/// </summary>
	/// <param name="node">The node whose children to refresh.</param>
	private async Task RefreshLoadedChildrenAsync(TreeNode<TItem> node)
	{
		var key = node.Data is null ? null : KeyField!(node.Data).ToString();
		var items = await GetDataAsync(key).ConfigureAwait(true);
		MergeModel(items, node, wholeTree: false);
		await NodeUpdated.InvokeAsync(node).ConfigureAwait(true);

		foreach (var child in node.Nodes!.Where(child => child.Nodes != null && !IsLeafItem(child)).ToList())
		{
			await RefreshLoadedChildrenAsync(child).ConfigureAwait(true);
		}
	}

	/// <summary>
	/// Whether a node's item is declared a leaf by <see cref="IsLeaf"/>, so has no children to query.
	/// </summary>
	private bool IsLeafItem(TreeNode<TItem> node)
		=> IsLeaf != null && node.Data != null && IsLeaf(node.Data);

	/// <summary>
	/// Merges fetched items into the existing nodes by key, keeping the node objects, expanded state and
	/// loaded children of every key still present, adding new keys and removing keys that have gone.
	/// </summary>
	/// <param name="items">The fetched items.</param>
	/// <param name="scope">The node being refreshed: the root for a whole-tree fetch, otherwise the parent whose children were fetched.</param>
	/// <param name="wholeTree">True when <paramref name="items"/> is every item in the tree; false when it is the direct children of <paramref name="scope"/>.</param>
	private void MergeModel(IEnumerable<TItem> items, TreeNode<TItem> scope, bool wholeTree)
	{
		var existing = GetNodesInScope(scope, wholeTree);
		var seen = new Dictionary<string, TreeNode<TItem>>();
		var modifiedParents = new HashSet<TreeNode<TItem>>();

		foreach (var item in items)
		{
			var key = GetItemKey(item);
			var parentNode = wholeTree ? FindParentNode(item, seen) : scope;
			var node = PlaceNode(key, item, parentNode, existing);
			ApplyItem(node, item, parentNode);

			seen[key] = node;
			modifiedParents.Add(parentNode);
		}

		// remove nodes this fetch no longer returns
		foreach (var node in existing.Where(pair => !seen.ContainsKey(pair.Key)).Select(pair => pair.Value))
		{
			node.ParentNode?.Nodes?.Remove(node);
		}

		scope.Nodes ??= [];
		foreach (var parent in modifiedParents)
		{
			parent.Nodes?.Sort(NodeSort);
		}
	}

	/// <summary>
	/// The nodes a fetch is authoritative for, by key: every descendant of the root for a whole-tree fetch,
	/// otherwise the scope's direct children. Any of them not fetched again has gone.
	/// </summary>
	private static Dictionary<string, TreeNode<TItem>> GetNodesInScope(TreeNode<TItem> scope, bool wholeTree)
	{
		var existing = new Dictionary<string, TreeNode<TItem>>();
		if (!wholeTree)
		{
			foreach (var node in scope.Nodes ?? [])
			{
				existing.TryAdd(node.Key, node);
			}

			return existing;
		}

		scope.Walk(node =>
		{
			if (node != scope)
			{
				existing.TryAdd(node.Key, node);
			}

			return true;
		});

		return existing;
	}

	/// <summary>
	/// Finds the parent node for an item of a whole-tree fetch, from its <see cref="ParentKeyField"/>.
	/// </summary>
	private TreeNode<TItem> FindParentNode(TItem item, Dictionary<string, TreeNode<TItem>> seen)
	{
		var parentKey = ParentKeyField?.Invoke(item)?.ToString();
		if (string.IsNullOrWhiteSpace(parentKey))
		{
			return RootNode;
		}

		var parentNode = seen.TryGetValue(parentKey, out var fetchedParent) ? fetchedParent : RootNode.Find(parentKey);
		return parentNode ?? throw new PDTreeException($"A parent item with key '{parentKey}' could not be found");
	}

	/// <summary>
	/// Returns the existing node for a key, moved under <paramref name="parentNode"/> if its parent changed,
	/// or a new node added to <paramref name="parentNode"/>.
	/// </summary>
	private TreeNode<TItem> PlaceNode(string key, TItem item, TreeNode<TItem> parentNode, Dictionary<string, TreeNode<TItem>> existing)
	{
		if (existing.TryGetValue(key, out var node))
		{
			// same key: keep the node object, its expanded state and its loaded children
			if (node.ParentNode != parentNode)
			{
				node.ParentNode?.Nodes?.Remove(node);
				AddChild(parentNode, node);
			}

			return node;
		}

		node = CreateNode(key, item);
		AddChild(parentNode, node);
		return node;
	}

	/// <summary>
	/// Creates a collapsed node for a newly fetched item. With <see cref="LoadOnDemand"/> its children are
	/// unloaded (null) unless <see cref="IsLeaf"/> says it has none.
	/// </summary>
	private TreeNode<TItem> CreateNode(string key, TItem item)
	{
		return new TreeNode<TItem>
		{
			Key = key,
			IsExpanded = false,
			Nodes = GetInitialChildNodes(item)
		};
	}

	/// <summary>
	/// The child nodes of a node for a newly loaded item: empty, unless with <see cref="LoadOnDemand"/> the
	/// children are unloaded (null) because <see cref="IsLeaf"/> does not say it has none.
	/// </summary>
	private List<TreeNode<TItem>>? GetInitialChildNodes(TItem item)
		=> !LoadOnDemand || IsLeafItemData(item) ? [] : null;

	/// <summary>
	/// Whether <see cref="IsLeaf"/> declares an item a leaf.
	/// </summary>
	private bool IsLeafItemData(TItem item) => IsLeaf != null && IsLeaf(item);

	/// <summary>
	/// Copies an item's current values onto its node.
	/// </summary>
	private void ApplyItem(TreeNode<TItem> node, TItem item, TreeNode<TItem> parentNode)
	{
		node.Text = TextField is null
			? item?.ToString() ?? string.Empty
			: TextField.Invoke(item).ToString() ?? item.ToString() ?? string.Empty;
		node.Data = item;
		node.ParentNode = parentNode;
		node.Level = parentNode.Level + 1;
		node.IconCssClass = IconCssClass is null || item is null
			? string.Empty
			: IconCssClass.Invoke(item, parentNode.Level + 1);
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
		var modifiedNodes = new HashSet<TreeNode<TItem>>();
		var dict = new Dictionary<string, TreeNode<TItem>>();

		foreach (var item in items)
		{
			// get key value and check it is given, then find the parent node
			var key = GetItemKey(item);
			var parentNode = FindParentNode(item, dict);

			// does the node already exist?
			var node = parentNode.Find(key);
			if (node is null)
			{
				// add to parent node and mark parent node for re-sort
				node = new TreeNode<TItem>();
				AddChild(parentNode, node);
				modifiedNodes.Add(parentNode);
			}

			node.Key = key;
			node.IsExpanded = false;
			ApplyItem(node, item, parentNode);
			node.Nodes = GetInitialChildNodes(item);

			// cache node for performance (this also rejects a duplicate key)
			dict.Add(key, node);
		}

		// re-apply sorts where necessary
		foreach (var node in modifiedNodes)
		{
			node.Nodes?.Sort(NodeSort);
		}
	}

	/// <summary>
	/// Gets an item's key from <see cref="KeyField"/>, which every item must supply.
	/// </summary>
	/// <param name="item">The item.</param>
	/// <returns>The item's key.</returns>
	/// <exception cref="PDTreeException">The item has no key.</exception>
	private string GetItemKey(TItem item)
	{
		var key = KeyField!(item)?.ToString();
		if (string.IsNullOrEmpty(key))
		{
			throw new PDTreeException("Items must supply a key value.");
		}

		return key;
	}

	/// <summary>
	/// Adds a node to the end of a parent's child nodes, creating the list if necessary.
	/// </summary>
	/// <param name="parentNode">The parent node.</param>
	/// <param name="node">The child node to add.</param>
	private static void AddChild(TreeNode<TItem> parentNode, TreeNode<TItem> node)
	{
		parentNode.Nodes ??= [];
		parentNode.Nodes.Add(node);
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
			return string.Compare(a.Text, b.Text, StringComparison.Ordinal);
		}
		else
		{
			return Sort(a.Data, b.Data);
		}
	}
}
