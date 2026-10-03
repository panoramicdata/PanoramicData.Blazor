namespace PanoramicData.Blazor;

public partial class PDTable<TItem>
{
	/// <summary>
	/// Gets whether an edit can begin: editing is enabled and exactly one row is selected.
	/// </summary>
	private bool CanBeginEdit =>
		IsEnabled
		&& AllowEdit
		&& !IsEditing
		&& SelectionMode != TableSelectionMode.None
		&& Selection.Count == 1
		&& KeyField != null;

	/// <summary>
	/// Begins editing of the given item.
	/// </summary>
	public async Task BeginEditAsync()
	{
		if (CanBeginEdit)
		{
			// Find item to edit
			var item = ItemsToDisplay.Find(x => KeyField!(x).ToString() == Selection[0]);
			if (item != null)
			{
				// Notify and allow for cancel
				EditItem = item;
				_tableBeforeEditArgs = new TableBeforeEditEventArgs<TItem>(EditItem);
				await InvokeAsync(async () => await BeforeEdit.InvokeAsync(_tableBeforeEditArgs).ConfigureAwait(true)).ConfigureAwait(true);
				if (!_tableBeforeEditArgs.Cancel)
				{
					_editValues.Clear();
					IsEditing = true;
					BeginEditEvent.Set();
					await InvokeAsync(StateHasChanged).ConfigureAwait(true);
				}
			}
		}
	}

	/// <summary>
	/// Captures an edit value for a column.
	/// </summary>
	/// <param name="column">The edited column.</param>
	/// <param name="value">Edited value.</param>
	public void OnEditInput(PDColumn<TItem> column, object? value) => _editValues[column.Id] = value;

	/// <summary>
	/// Captures an edit value for a column id.
	/// </summary>
	/// <param name="columnId">Column id.</param>
	/// <param name="value">Edited value.</param>
	public void OnEditInput(string columnId, object? value) => _editValues[columnId] = value;

	/// <summary>
	/// Commits the current edit.
	/// </summary>
	public async Task CommitEditAsync()
	{
		if (IsEditing && EditItem != null)
		{
			// Notify and allow cancel
			var args = BuildAfterEditArgs(EditItem);
			await AfterEdit.InvokeAsync(args).ConfigureAwait(true);
			if (!args.Cancel && !await TryApplyEditValuesAsync(args).ConfigureAwait(true))
			{
				return;
			}

			// save changes if configured to do so and data provider is given
			var delta = new Dictionary<string, object?>();
			if (SaveChanges && DataProvider != null)
			{
				delta = BuildEditDelta();
				await DataProvider.UpdateAsync(EditItem, delta, default).ConfigureAwait(true);
			}

			IsEditing = false;

			await NotifyEditCommittedAsync(delta).ConfigureAwait(true);
		}
	}

	/// <summary>
	/// Builds the after-edit arguments holding the captured value of each column being edited.
	/// </summary>
	private TableAfterEditEventArgs<TItem> BuildAfterEditArgs(TItem editItem)
	{
		var args = new TableAfterEditEventArgs<TItem>(editItem);
		foreach (var column in ActualColumnsToDisplay)
		{
			if (_editValues.TryGetValue(column.Id, out object? value) && IsColumnInEditMode(column, editItem))
			{
				args.NewValues.Add(column.Id, value);
			}
		}

		return args;
	}

	/// <summary>
	/// Applies the new values to the item being edited.
	/// </summary>
	/// <returns>False when a value could not be applied (the exception has been reported); otherwise true.</returns>
	private async Task<bool> TryApplyEditValuesAsync(TableAfterEditEventArgs<TItem> args)
	{
		foreach (var column in ActualColumnsToDisplay)
		{
			if (args.NewValues.TryGetValue(column.Id, out object? newValue))
			{
				try
				{
					column.SetValue(EditItem!, newValue);
				}
				catch (Exception ex)
				{
					await ExceptionHandler.InvokeAsync(ex).ConfigureAwait(true);
					return false;
				}
			}
		}

		return true;
	}

	/// <summary>
	/// Builds the delta to save, keyed by the member name of each edited column with a value.
	/// </summary>
	private Dictionary<string, object?> BuildEditDelta()
	{
		var delta = new Dictionary<string, object?>();
		foreach (var kvp in _editValues.Where(kvp => kvp.Value != null))
		{
			var col = Columns.Find(x => x.Id == kvp.Key);
			if (col?.Field?.GetPropertyMemberInfo() is MemberInfo mi)
			{
				delta.Add(mi.Name, kvp.Value);
			}
		}

		return delta;
	}

	/// <summary>
	/// Notifies the application of a successful edit, ends the edit and returns focus to the table.
	/// </summary>
	private async Task NotifyEditCommittedAsync(Dictionary<string, object?> delta)
	{
		// notify app of successful update
		if (EditItem != null)
		{
			var committedArgs = new TableAfterEditCommittedEventArgs<TItem>(EditItem)
			{
				NewValues = delta
			};
			await AfterEditCommitted.InvokeAsync(committedArgs).ConfigureAwait(true);
		}

		EditItem = null;

		if (_commonModule != null)
		{
			await _commonModule.InvokeVoidAsync("focus", Id).ConfigureAwait(true);
		}

		StateHasChanged();
	}

	/// <summary>
	/// Cancels the current edit.
	/// </summary>
	public async Task CancelEdit()
	{
		if (IsEditing)
		{
			EditItem = null;
			IsEditing = false;
			if (_commonModule != null)
			{
				await _commonModule.InvokeVoidAsync("focus", Id).ConfigureAwait(true);
			}
		}
	}

	/// <summary>
	/// Commits edit mode when focus leaves all edit inputs.
	/// </summary>
	public async Task OnEditBlurAsync()
	{
		// if focus has moved to another editor then continue editing otherwise end edit
		if (IsEditing)
		{
			// small delay required for when running in WebAssembly otherwise
			// the call to getFocusedElementId returns null
			await Task.Delay(100).ConfigureAwait(true);
			if (_commonModule != null)
			{
				var id = await _commonModule.InvokeAsync<string>("getFocusedElementId").ConfigureAwait(true);
				if (!id.StartsWith(_idEditPrefix, StringComparison.Ordinal))
				{
					await CommitEditAsync().ConfigureAwait(true);
				}
			}
		}
	}

	private void OnEditTimer()
	{
		if (IsEnabled && !_dragging)
		{
			Task.Run(async () => await BeginEditAsync().ConfigureAwait(true));
		}
	}

	private bool IsColumnInEditMode(PDColumn<TItem> column, TItem item)
	{
		// is editing current row?
		if (IsEditing && item == EditItem)
		{
			// a computed field has no member to write to, so only an edit template can edit it
			if (column.IsComputed && column.EditTemplate is null)
			{
				return false;
			}

			return IsColumnEditable(column);
		}

		return false;
	}

	/// <summary>
	/// Determines whether a column is editable, allowing the application's column configuration to override it.
	/// </summary>
	private bool IsColumnEditable(PDColumn<TItem> column)
	{
		var config = ColumnsConfig?.Find(x => x.Id == column.Id);
		return config?.Editable ?? column.Editable;
	}

	/// <summary>
	/// Once edit mode has begun, selects the text of the first editable column's editor.
	/// </summary>
	private async Task FocusFirstEditorAsync()
	{
		if (BeginEditEvent.WaitOne(0) && Columns.Count > 0 && EditItem != null)
		{
			// find first editable column
			var key = ActualColumnsToDisplay.Find(IsColumnEditable)?.Id ?? string.Empty;

			var row = ItemsToDisplay.IndexOf(EditItem);
			if (key != string.Empty)
			{
				if (_commonModule != null)
				{
					await _commonModule.InvokeVoidAsync("selectText", $"{_idEditPrefix}-{row}-{key}", _tableBeforeEditArgs!.SelectionStart, _tableBeforeEditArgs!.SelectionEnd).ConfigureAwait(true);
				}

				BeginEditEvent.Reset();
			}
		}
	}
}
