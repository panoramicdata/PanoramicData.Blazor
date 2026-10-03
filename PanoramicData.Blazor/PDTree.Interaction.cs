namespace PanoramicData.Blazor;

/// <summary>
/// Mouse, keyboard, selection, expansion and editing behaviour of the <see cref="PDTree{TItem}"/>.
/// </summary>
public partial class PDTree<TItem>
{
	private int _clickCount;
	private Timer? _clickTimer;
	private TreeNode<TItem>? _clickedNode;

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
	/// Selects the given node.
	/// </summary>
	/// <param name="node">The node to select.</param>
	public async Task SelectNode(TreeNode<TItem> node)
		=> await SelectNode(node, true);

	/// <summary>
	/// Selects the given node, optionally entering edit mode if the same node is selected twice.
	/// </summary>
	/// <param name="node">The node to select.</param>
	/// <param name="autoEdit">If true, enters edit mode if the same node is selected twice.</param>
	public async Task SelectNode(TreeNode<TItem> node, bool autoEdit)
	{
		if (!AllowSelection)
		{
			return;
		}

		// end edit mode
		if (SelectedNode != null)
		{
			await CommitEdit().ConfigureAwait(true);
		}

		// select new node
		if (SelectedNode != node)
		{
			await ChangeSelectionAsync(node).ConfigureAwait(true);
		}
		else if (AllowEdit && autoEdit)
		{
			// the same node was selected again
			await BeginEdit().ConfigureAwait(true);
		}
	}

	/// <summary>
	/// Moves the selection to a different node, unless the application cancels the change.
	/// </summary>
	/// <param name="node">The node to select.</param>
	private async Task ChangeSelectionAsync(TreeNode<TItem> node)
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
		for (var parentNode = SelectedNode.ParentNode; parentNode != null; parentNode = parentNode.ParentNode)
		{
			parentNode.IsExpanded = true;
		}

		// notify of change
		await SelectionChange.InvokeAsync(SelectedNode).ConfigureAwait(true);
		StateHasChanged();
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

		// Re-render so that expanding or collapsing from code, not only from the expander icon, updates the page.
		// This goes through the dispatcher because a caller may be on another thread (issue #183).
		await InvokeAsync(StateHasChanged).ConfigureAwait(true);
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
		if (SelectedNode?.IsEditing != true)
		{
			return;
		}

		if (string.IsNullOrWhiteSpace(SelectedNode.EditText))
		{
			SelectedNode.CancelEdit();
			await ExceptionHandler.InvokeAsync(new PDTreeException("A value is required")).ConfigureAwait(true);
		}
		else
		{
			await CommitEditTextAsync(SelectedNode).ConfigureAwait(true);
		}
	}

	/// <summary>
	/// Offers a non-empty edit to the application through <see cref="AfterEdit"/>, then commits or cancels it.
	/// </summary>
	/// <param name="node">The node being edited.</param>
	private async Task CommitEditTextAsync(TreeNode<TItem> node)
	{
		// notify and allow cancel
		var afterEditArgs = new TreeNodeAfterEditEventArgs<TItem>(node, node.Text, node.EditText);
		await AfterEdit.InvokeAsync(afterEditArgs).ConfigureAwait(true);
		if (afterEditArgs.Cancel)
		{
			node.CancelEdit();
			return;
		}

		node.EditText = afterEditArgs.NewValue; // application might of altered
		node.CommitEdit();
		node.ParentNode?.Nodes?.Sort(NodeSort); // re-sort parent
		if (_commonModule != null)
		{
			await _commonModule.InvokeVoidAsync("focus", Id).ConfigureAwait(true);
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
		await HandleEditKeyAsync(args.Code).ConfigureAwait(true);

		if (SelectedNode?.IsEditing == false)
		{
			await HandleNavigationKeyAsync(SelectedNode, args.Code).ConfigureAwait(true);
		}

		await KeyDown.InvokeAsync(args).ConfigureAwait(true);
	}

	/// <summary>
	/// Ends any current edit for the keys that do so: Escape cancels it, Enter / Return commits it.
	/// </summary>
	/// <param name="code">The key code.</param>
	private async Task HandleEditKeyAsync(string code)
	{
		if (code == "Escape")
		{
			CancelEdit();
		}
		else if (code is "Enter" or "Return")
		{
			await CommitEdit().ConfigureAwait(true);
		}
	}

	/// <summary>
	/// Handles the keys that act on the selected node while it is not being edited.
	/// </summary>
	/// <param name="node">The selected node.</param>
	/// <param name="code">The key code.</param>
	private async Task HandleNavigationKeyAsync(TreeNode<TItem> node, string code)
	{
		var action = code switch
		{
			"F2" => BeginEdit(),
			"ArrowRight" => SetExpandedAsync(node, true),
			"ArrowLeft" => SetExpandedAsync(node, false),
			"ArrowDown" => SelectNextAsync(node),
			"ArrowUp" => SelectPreviousAsync(node),
			_ => Task.CompletedTask
		};
		await action.ConfigureAwait(true);
	}

	/// <summary>
	/// Expands or collapses a node, unless it is already in that state.
	/// </summary>
	/// <param name="node">The node to expand or collapse.</param>
	/// <param name="expanded">True to expand the node, false to collapse it.</param>
	private async Task SetExpandedAsync(TreeNode<TItem> node, bool expanded)
	{
		if (node.IsExpanded != expanded)
		{
			await ToggleNodeIsExpandedAsync(node).ConfigureAwait(true);
		}
	}

	/// <summary>
	/// Selects the node displayed after the given node, if there is one.
	/// </summary>
	/// <param name="node">The currently selected node.</param>
	private async Task SelectNextAsync(TreeNode<TItem> node)
	{
		var nextNode = node.GetNext();
		if (nextNode != null)
		{
			await SelectNode(nextNode).ConfigureAwait(true);
		}
	}

	/// <summary>
	/// Selects the node displayed before the given node, if there is one and it is not a hidden root node.
	/// </summary>
	/// <param name="node">The currently selected node.</param>
	private async Task SelectPreviousAsync(TreeNode<TItem> node)
	{
		var prevNode = node.GetPrevious();

		// do not select previous node if that is the hidden root node
		// the root node is level 0 but we somehow use another root that is level 1
		// The level 1 root node is the one we show with ShowRoot
		if (prevNode != null && ((ShowRoot && prevNode.Level != 0) || prevNode.Level > 1))
		{
			await SelectNode(prevNode).ConfigureAwait(true);
		}
	}
}
