namespace PanoramicData.Blazor;

public partial class PDTable<TItem>
{
	/// <summary>
	/// Disposes timers, event subscriptions, and JavaScript resources.
	/// </summary>
	public async ValueTask DisposeAsync()
	{
		try
		{
			GC.SuppressFinalize(this);
			_editTimer?.Dispose();
			_editTimer = null;
			if (PageCriteria != null)
			{
				PageCriteria.PageChanged -= PageCriteria_PageChanged;
				PageCriteria.PageSizeChanged -= PageCriteria_PageSizeChanged;
			}

			if (_commonModule != null)
			{
				await _commonModule.DisposeAsync().ConfigureAwait(true);
				_commonModule = null;
			}
		}
		catch (Exception ex)
		{
			// Disposal is best effort: the JavaScript module cannot be released once the circuit or
			// page has gone, and a failure here must not stop the rest of the page being disposed.
			Logger.LogDebug(ex, "Error disposing table");
		}
	}

	/// <summary>
	/// Initializes component resources and performs first-load behavior.
	/// </summary>
	/// <param name="firstRender">True on first render; otherwise false.</param>
	protected override async Task OnAfterRenderAsync(bool firstRender)
	{
		if (firstRender && JSRuntime is not null)
		{
			await InitializeOnFirstRenderAsync().ConfigureAwait(true);
		}

		// Load previously saved state
		if (StateManager != null)
		{
			try
			{
				await LoadStateAsync();
			}
			catch
			{
				// Loading state too early can cause issues
			}
		}

		// If this is the first time we've finished rendering, then all the columns
		// have been added to the table so we'll go and get the data for the first time
		if (firstRender)
		{
			await LoadInitialDataAsync().ConfigureAwait(true);
			await Ready.InvokeAsync(null).ConfigureAwait(true);
		}

		// Focus first editor after edit mode begins
		await FocusFirstEditorAsync().ConfigureAwait(true);
	}

	/// <summary>
	/// Subscribes to the page criteria, creates the edit timer and loads the common JavaScript module.
	/// </summary>
	private async Task InitializeOnFirstRenderAsync()
	{
		try
		{
			if (DataProvider is null)
			{
				throw new PDTableException($"{nameof(DataProvider)} must not be null.");
			}

			if (PageCriteria != null)
			{
				PageCriteria.PageChanged += PageCriteria_PageChanged;
				PageCriteria.PageSizeChanged += PageCriteria_PageSizeChanged;
			}

			_editTimer = new Timer(_ => OnEditTimer(), null, Timeout.Infinite, Timeout.Infinite);

			// Load common JavaScript
			_commonModule = await JSRuntime.InvokeAsync<IJSObjectReference>("import", JSInteropVersionHelper.CommonJsUrl);
			if (_commonModule != null)
			{
				await _commonModule.InvokeVoidAsync("onTableDragStart", Id);
			}
		}
		catch (Exception)
		{
			// BC-40 - fast page switching in Server Side blazor can lead to OnAfterRender call after page / objects disposed
		}
	}

	/// <summary>
	/// Fetches the data for the first time, when the table loads automatically.
	/// </summary>
	private async Task LoadInitialDataAsync()
	{
		try
		{
			if (AutoLoad)
			{
				await GetDataAsync().ConfigureAwait(true);
				StateHasChanged();
			}
		}
		catch (Exception ex)
		{
			await HandleExceptionAsync(ex).ConfigureAwait(true);
		}
	}

	/// <summary>
	/// Applies parameter updates, including filter synchronization and validation.
	/// </summary>
	protected override void OnParametersSet()
	{
		var currentColumnCount = ActualColumnsToDisplay.Count;

		// Process SearchText if:
		// 1. Columns are available, AND
		// 2. Either SearchText changed OR visible columns changed
		if (currentColumnCount > 0 &&
			(SearchText != _lastSearchText || currentColumnCount != _lastColumnCount))
		{
			_lastSearchText = SearchText;
			_lastColumnCount = currentColumnCount;

			foreach (var column in ActualColumnsToDisplay.Where(x => x.Filterable))
			{
				if (string.IsNullOrWhiteSpace(SearchText))
				{
					column.Filter.Clear();
				}
				else
				{
					column.Filter.UpdateFrom(SearchText ?? string.Empty);
				}
			}
		}

		// Validate parameter constraints
		if (SelectionMode != TableSelectionMode.None && KeyField == null)
		{
			throw new PDTableException("KeyField attribute must be specified when enabling selection.");
		}
	}

	#region State Management

	private async Task LoadStateAsync()
	{
		// load state
		if (StateManager != null)
		{
			await StateManager.InitializeAsync();
			var state = await StateManager.LoadStateAsync<TableState>(Id);
			if (state != null)
			{
				foreach (var kvp in state.Columns)
				{
					var col = Columns.FirstOrDefault(x => x.Id == kvp.Key);
					if (col != null)
					{
						col.State = kvp.Value;
					}
				}
			}
		}
	}

	/// <summary>
	/// Saves current table state via the configured state manager.
	/// </summary>
	public async Task SaveStateAsync()
	{
		// table must have id
		if (!string.IsNullOrEmpty(Id) && StateManager != null)
		{
			// individual column state - must have id
			var state = new TableState
			{
				Columns = Columns.Where(x => !string.IsNullOrWhiteSpace(x.Id)).ToDictionary(x => x.Id, y => y.State)
			};
			await StateManager.SaveStateAsync(Id, state);
		}
	}

	#endregion
}
