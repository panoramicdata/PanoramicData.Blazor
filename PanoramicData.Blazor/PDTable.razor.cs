﻿using System.Reflection.Metadata;

namespace PanoramicData.Blazor;

public partial class PDTable<TItem> :
	ISortableComponent,
	IPageableComponent,
	IAsyncDisposable,
	IEnablable
	where TItem : class
{
	private bool _dragging;
	private Timer? _editTimer;
	private static int _idSequence;
	private string? _lastSearchText;
	private IJSObjectReference? _commonModule;
	private bool _mouseDownOriginatedFromTable;
	private readonly string _idEditPrefix = "pd-table-edit-";
	private CancellationTokenSource? _cancellationTokenSource;
	private TableBeforeEditEventArgs<TItem>? _tableBeforeEditArgs;
	private readonly Dictionary<string, object?> _editValues = [];
	//private readonly Dictionary<string, string> _keyProperties = new();

	private ManualResetEvent BeginEditEvent { get; set; } = new ManualResetEvent(false);

	[Inject] protected ILogger<PDTable<TItem>> Logger { get; set; } = new NullLogger<PDTable<TItem>>();

	[Inject] protected NavigationManager NavigationManager { get; set; } = null!;

	[Inject] public IJSRuntime JSRuntime { get; set; } = null!;

	[Inject] protected IBlockOverlayService BlockOverlayService { get; set; } = null!;

	[CascadingParameter] public PDDragContext? DragContext { get; set; }

	[CascadingParameter] public IAsyncStateManager? StateManager { get; set; }

	#region Parameters

	/// <summary>
	/// Callback fired after an item edit ends.
	/// </summary>
	[Parameter] public EventCallback<TableAfterEditEventArgs<TItem>> AfterEdit { get; set; }

	/// <summary>
	/// Callback fired after an item edit ends and has been successfully saved.
	/// </summary>
	[Parameter] public EventCallback<TableAfterEditCommittedEventArgs<TItem>> AfterEditCommitted { get; set; }

	/// <summary>
	/// Callback fired after a fetch has completed
	/// </summary>
	[Parameter] public EventCallback AfterFetch { get; set; }

	/// <summary>
	/// Gets or sets whether rows may be dragged.
	/// </summary>
	[Parameter] public bool AllowDrag { get; set; }

	/// <summary>
	/// Gets or sets whether items may be dropped onto rows.
	/// </summary>
	[Parameter] public bool AllowDrop { get; set; }

	/// <summary>
	/// Gets whether the table allows in-place editing.
	/// </summary>
	[Parameter] public bool AllowEdit { get; set; }

	/// <summary>
	/// Determines whether items are fetched from the DataProvider when the component is
	/// first rendered.
	/// </summary>
	[Parameter] public bool AutoLoad { get; set; } = true;

	/// <summary>
	/// Callback fired before an item edit begins.
	/// </summary>
	[Parameter] public EventCallback<TableBeforeEditEventArgs<TItem>> BeforeEdit { get; set; }

	/// <summary>
	/// Callback fired before a fetch is started
	/// </summary>
	[Parameter] public EventCallback BeforeFetch { get; set; }

	/// <summary>
	/// Child HTML content.
	/// </summary>
	[Parameter] public RenderFragment ChildContent { get; set; } = null!;

	/// <summary>
	/// Callback fired whenever the user clicks on a given item.
	/// </summary>
	[Parameter] public EventCallback<TItem> Click { get; set; }

	/// <summary>
	/// Allows an application defined configuration to be applied to the available columns
	/// at runtime.
	/// </summary>
	[Parameter] public List<PDColumnConfig>? ColumnsConfig { get; set; }

	/// <summary>
	/// Gets or sets the CSS class to apply to the container element.
	/// </summary>
	[Parameter] public string CssClass { get; set; } = string.Empty;

	/// <summary>
	/// Gets or sets the IDataProviderService instance to use to fetch data.
	/// </summary>
	[Parameter] public IDataProviderService<TItem> DataProvider { get; set; } = null!;

	/// <summary>
	/// Callback fired whenever the user double-clicks on a given item.
	/// </summary>
	[Parameter] public EventCallback<TItem> DoubleClick { get; set; }

	/// <summary>
	/// Function that calculates and returns the download url attribute for each row.
	/// </summary>
	[Parameter] public Func<TItem, string?> DownloadUrlFunc { get; set; } = (_) => null;

	/// <summary>
	/// Callback fired whenever a drag operation ends on a row within a DragContext.
	/// </summary>
	[Parameter] public EventCallback<DropEventArgs> Drop { get; set; }

	/// <summary>
	/// Should edit begin on double click? if not then single click will begin edit mode.
	/// </summary>
	[Parameter] public bool EditOnDoubleClick { get; set; }

	/// <summary>
	/// Gets or sets a delegate to be called if an exception occurs.
	/// </summary>
	[Parameter] public EventCallback<Exception> ExceptionHandler { get; set; }

	/// <summary>
	/// Gets or sets the maximum number of possible filter values to show.
	/// </summary>
	[Parameter] public int FilterMaxValues { get; set; } = 50;

	/// <summary>
	/// Gets the unique identifier of this table.
	/// </summary>
	[Parameter] public string Id { get; set; } = $"pd-table-{++_idSequence}";

	[Parameter] public bool IsEnabled { get; set; } = true;

	/// <summary>
	/// Action called whenever data items are loaded.
	/// </summary>
	/// <remarks>The action allows the items to be modified by the calling application.</remarks>
	[Parameter] public Action<List<TItem>>? ItemsLoaded { get; set; }

	/// <summary>
	/// Callback fired whenever the user presses a key down.
	/// </summary>
	[Parameter] public EventCallback<KeyboardEventArgs> KeyDown { get; set; }

	/// <summary>
	/// A LINQ expression that selects the item field that contains the key value.
	/// </summary>
	[Parameter] public Func<TItem, object>? KeyField { get; set; }

	/// <summary>
	/// Gets or sets the message to be displayed when no data is available.
	/// </summary>
	[Parameter] public string NoDataMessage { get; set; } = "No data";

	/// <summary>
	/// Gets or sets a function that calculates and returns CSS Classes for the row (TR element).
	/// </summary>
	[Parameter] public Func<TItem, string>? RowClass { get; set; }

	/// <summary>
	/// Gets or sets a function that determines whether the given row is enabled or not.
	/// </summary>
	[Parameter] public Func<TItem, bool> RowIsEnabled { get; set; } = (_) => true;

	/// <summary>
	/// Gets or sets the CSS class to apply to the tables container element.
	/// </summary>
	[Parameter] public string TableClass { get; set; } = string.Empty;

	/// <summary>
	/// Gets or sets the CSS class to apply to the table header element.
	/// </summary>
	[Parameter] public string THeadClass { get; set; } = string.Empty;

	/// <summary>
	/// Callback fired whenever the component changes the currently displayed page.
	/// </summary>
	[Parameter] public EventCallback<PageCriteria> PageChanged { get; set; }

	/// <summary>
	/// Gets or sets the default page criteria.
	/// </summary>
	[Parameter] public PageCriteria? PageCriteria { get; set; }

	/// <summary>
	/// Gets or sets whether the pager (if shown) is positioned at the top or bottom of the table.
	/// </summary>
	[Parameter] public PagerPositions PagerPosition { get; set; }

	/// <summary>
	/// Gets or sets the possible page sizes offered to the user.
	/// </summary>
	[Parameter] public uint[] PageSizeChoices { get; set; } = [10, 25, 50, 100, 250, 500];

	/// <summary>
	/// Gets or sets an event callback raised when the component has perform all it initialization.
	/// </summary>
	[Parameter] public EventCallback Ready { get; set; }

	/// <summary>
	/// Gets or sets whether the selection is maintained across pages.
	/// </summary>
	[Parameter] public bool RetainSelectionOnPage { get; set; }

	/// <summary>
	/// Gets whether right-clicking selects a row versus left-clicking.
	/// </summary>
	[Parameter] public bool RightClickSelectsRow { get; set; } = true;

	/// <summary>
	/// Gets whether the table will save changes via the DataProvider (if set).
	/// </summary>
	[Parameter] public bool SaveChanges { get; set; }

	/// <summary>
	/// Search text to be passed to IDataProvider when querying for data.
	/// </summary>
	[Parameter] public string? SearchText { get; set; }

	/// <summary>
	/// Event callback for when search text has changed.
	/// </summary>
	[Parameter] public EventCallback<string?> SearchTextChanged { get; set; }

	/// <summary>
	/// Callback fired whenever the current selection changes.
	/// </summary>
	[Parameter] public EventCallback SelectionChanged { get; set; }

	/// <summary>
	/// Gets or sets whether selection is enabled and the method in which it works.
	/// </summary>
	[Parameter] public TableSelectionMode SelectionMode { get; set; }

	/// <summary>
	/// Gets or sets whether the checkboxes should be shown for multiple selection.
	/// </summary>
	[Parameter] public bool ShowCheckboxes { get; set; }

	/// <summary>
	/// Gets or sets whether the Overlay Service is used when fetching data.
	/// </summary>
	[Parameter] public bool ShowOverlay { get; set; } = true;

	/// <summary>
	/// Gets or sets whether the pager is displayed.
	/// </summary>
	[Parameter] public bool ShowPager { get; set; } = true;

	/// <summary>
	/// Gets or sets the button and form control sizes.
	/// </summary>
	[Parameter] public ButtonSizes? Size { get; set; }

	/// <summary>
	/// Event callback fired whenever the sort criteria has changed.
	/// </summary>
	[Parameter] public EventCallback<SortCriteria> SortChanged { get; set; }

	/// <summary>
	/// Gets or sets the default sort criteria.
	/// </summary>
	[Parameter] public SortCriteria SortCriteria { get; set; } = new SortCriteria();

	/// <summary>
	/// Gets or sets whether the contents of all cells are user selectable by default.
	/// </summary>
	[Parameter] public bool UserSelectable { get; set; }

	#endregion

	/// <summary>
	/// Gets the current item being edited.
	/// </summary>
	public TItem? EditItem { get; private set; }

	/// <summary>
	/// Gets a full list of all columns.
	/// </summary>
	public List<PDColumn<TItem>> Columns { get; } = [];

	/// <summary>
	/// Gets the keys of all currently selected items.
	/// </summary>
	public List<string> Selection { get; } = [];

	/// <summary>
	/// Gets a calculated list of actual columns to be displayed.
	/// </summary>
	public List<PDColumn<TItem>> ActualColumnsToDisplay
	{
		get
		{
			var availableColumnIds = Columns.ConvertAll(c => c.Id);

			var columns =
				ColumnsConfig?
					.Where(columnConfig => availableColumnIds.Contains(columnConfig.Id))
					.Select(columnConfig =>
					{
						var dTColumn = Columns.Single(c => c.Id == columnConfig.Id);
						if (columnConfig.Title != default)
						{
							dTColumn.SetTitle(columnConfig.Title);
						}

						return dTColumn;
					})
				?? Columns;

			return [.. columns
				.Where(c => c.ShowInList && c.State.Visible)
				.OrderBy(c => c.State.Ordinal)];
		}
	}

	/// <summary>
	/// Gets the items to be displayed as rows.
	/// </summary>
	public List<TItem> ItemsToDisplay { get; private set; } = [];

	/// <summary>
	/// Current page number, when paging enabled.
	/// </summary>
	protected int Page { get; set; } = 1;

	/// <summary>
	/// Gets whether the component is currently busy.
	/// </summary>
	public bool IsBusy => _cancellationTokenSource != null;

	/// <summary>
	/// Gets whether the component is has been cancelled.
	/// </summary>
	public bool IsCancelled => _cancellationTokenSource?.IsCancellationRequested == true;

	/// <summary>
	/// Gets whether the table is currently in edit mode.
	/// </summary>
	public bool IsEditing { get; private set; }

	/// <summary>
	/// Adds the given column to the list of available columns.
	/// </summary>
	/// <param name="column">The PDColumn to be added.</param>
	public async Task AddColumnAsync(PDColumn<TItem> column)
	{
		try
		{
			// improve on default column id - this will improve state persistence
			if (Regex.IsMatch(column.Id, @"^col-\d+$"))
			{
				var name = string.IsNullOrEmpty(column.Name) ? column.GetTitle() : column.Name;
				if (!string.IsNullOrWhiteSpace(name))
				{
					var simpleName = name.ExtractAlphanumericChars().ToLower(CultureInfo.InvariantCulture);
					if (!string.IsNullOrWhiteSpace(simpleName))
					{
						column.SetId($"col-{simpleName}");
					}
				}
			}

			Columns.Add(column);
			if (column.Id == SortCriteria?.Key || column.GetTitle() == SortCriteria?.Key)
			{
				column.SortDirection = SortCriteria!.Direction;
			}

			if (column.Filterable && column.Field != null)
			{
				// obtain filter key
				column.Filter.Key = string.IsNullOrWhiteSpace(column.FilterKey) ? column.GetFilterKey() : column.FilterKey;

				// determine property name
				column.Filter.PropertyName = column.GetPropertyName();

				// update mapping
				if (DataProvider is IFilterProviderService<TItem> fs)
				{
					fs.KeyPropertyMappings.Add(column.Filter.Key, column.Filter.PropertyName);
				}

			}

			StateHasChanged();
		}
		catch (Exception ex)
		{
			await HandleExceptionAsync(ex).ConfigureAwait(false);
		}
	}

	/// <summary>
	/// Cancels the current operation.
	/// </summary>
	public async Task CancelAsync()
	{
		if (_cancellationTokenSource?.IsCancellationRequested == false)
		{
			await _cancellationTokenSource.CancelAsync();
		}
	}

	/// <summary>
	/// Forces the table to indicate its state has changed.
	/// </summary>
	public void SetStateHasChanged() => StateHasChanged();

	/// <summary>
	/// Centralized method to process exceptions.
	/// </summary>
	/// <param name="ex">Exception that has been raised.</param>
	public async Task HandleExceptionAsync(Exception ex)
	{
		Logger.LogError(ex, "{Message}", ex.Message);
		await ExceptionHandler.InvokeAsync(ex).ConfigureAwait(true);
	}

	/// <summary>
	/// Refresh the grid by performing a re-query.
	/// </summary>
	/// <param name="searchText">Optional override for the search text.</param>
	public Task RefreshAsync() => RefreshAsync(null);

	public Task RefreshAsync(string? searchText)
		=> GetDataAsync(searchText);

	/// <summary>
	/// Instructs the component to show the specified page.
	/// </summary>
	/// <param name="criteria">Details of the page to be displayed.</param>
	public async Task PageAsync(PageCriteria criteria)
	{
		SetPageCriteria(criteria);
		await GetDataAsync().ConfigureAwait(true);
		await PageChanged.InvokeAsync(criteria).ConfigureAwait(true);
	}

	/// <summary>
	/// Sort the displayed items.
	/// </summary>
	/// <param name="criteria">Details of the sort operation to be performed.</param>
	public Task SortAsync(SortCriteria criteria)
	{
		var column = Columns.SingleOrDefault(c => string.Equals(c.PropertyInfo?.Name, criteria.Key, StringComparison.OrdinalIgnoreCase));
		if (column != null)
		{
			return SortByAsync(column, criteria.Direction);
		}

		return Task.CompletedTask;
	}

	/// <summary>
	/// Requests data from the data provider using the current settings.
	/// </summary>
	/// <param name="searchText">Optional override for the search text.</param>

	protected async Task GetDataAsync()
		=> await GetDataAsync(null);

	protected async Task GetDataAsync(string? searchText)
	{
		try
		{
			// Provide a means to cancel the refresh
			_cancellationTokenSource = new CancellationTokenSource();

			if (ShowOverlay)
			{
				BlockOverlayService.Show();
			}

			await BeforeFetch.InvokeAsync();

			//var sortColumn = Columns.SingleOrDefault(c => c.SortColumn);
			var sortColumn = Columns.Find(x => x.Id == SortCriteria?.Key || x.GetTitle() == SortCriteria?.Key);
			var request = new DataRequest<TItem>
			{
				Skip = 0,
				ForceUpdate = false,
				SortFieldExpression = sortColumn?.Field,
				SortDirection = sortColumn?.SortDirection,
				SearchText = searchText ?? SearchText
			};

			// Paging
			if (PageCriteria != null)
			{
				request.Take = (int)PageCriteria.PageSize;
				request.Skip = (int)PageCriteria.PreviousItems;
			}

			// Clear selection
			if (!RetainSelectionOnPage)
			{
				await ClearSelectionAsync().ConfigureAwait(true);
			}

			// Perform query data
			var response = await DataProvider
				.GetDataAsync(request, _cancellationTokenSource.Token)
				.ConfigureAwait(true);

			// Allow calling application to filter/add items etc.
			var items = new List<TItem>(response.Items);
			ItemsLoaded?.Invoke(items); // must use an action here and not an EventCallaback as that leads to infinite loop and 100% CPU
			ItemsToDisplay = items;

			// Update pager state
			if (PageCriteria != null)
			{
				PageCriteria.TotalCount = (uint)(response.TotalCount ?? 0);
			}
		}
		catch (Exception ex)
		{
			await ExceptionHandler.InvokeAsync(ex).ConfigureAwait(true);
		}
		finally
		{
			_cancellationTokenSource?.Dispose();
			_cancellationTokenSource = null;

			if (ShowOverlay)
			{
				BlockOverlayService.Hide();
			}

			await AfterFetch.InvokeAsync();
		}
	}

	/// <summary>
	/// Sort the data by the specified column.
	/// </summary>
	/// <param name="column">The column to sort by.</param>
	/// <remarks>To disable sorting for any given column, set its Sortable property set to false.</remarks>

	protected async Task SortByAsync(PDColumn<TItem> column)
	{
		await SortByAsync(column, null);
	}

	protected async Task SortByAsync(PDColumn<TItem> column, SortDirection? direction)
	{
		if (column.Sortable && !string.IsNullOrWhiteSpace(column.Id))
		{
			// If direction specified then sort as requested
			if (direction.HasValue)
			{
				column.SortDirection = direction.Value;
			}
			else
			{
				// If column already sorted then reverse direction
				if (column.Id == SortCriteria?.Key || column.GetTitle() == SortCriteria?.Key)
				{
					column.SortDirection = column.SortDirection == SortDirection.Ascending ? SortDirection.Descending : SortDirection.Ascending;
				}
				else
				{
					var previousCol = Columns.FirstOrDefault(x => x.Id == SortCriteria?.Key || x.GetTitle() == SortCriteria?.Key);
					if (previousCol != null)
					{
						previousCol.SortDirection = SortDirection.None;
					}

					if (column.DefaultSortDirection != SortDirection.None)
					{
						column.SortDirection = column.DefaultSortDirection;
					}
					else
					{
						var defaultSortDirection = SortDirection.Ascending;
						if (column.Field != null)
						{
							var info = column.Field.GetPropertyMemberInfo();
							if (info?.GetMemberUnderlyingType()?.FullName != typeof(string).FullName)
							{
								defaultSortDirection = SortDirection.Descending;
							}
						}

						column.SortDirection = defaultSortDirection;
					}
				}
			}

			if (SortCriteria != null)
			{
				SortCriteria.Key = column.Id;
				SortCriteria.Direction = column.SortDirection;
			}

			await GetDataAsync().ConfigureAwait(true);
			await SortChanged.InvokeAsync(new SortCriteria { Key = column.Id, Direction = direction ?? column.SortDirection }).ConfigureAwait(true);

			// Ensure column sorted on is still in view
			if (_commonModule != null)
			{
				// Using query selector that allows column names to be non-unique - i.e default col ids are col-1, col-2 etc.
				await _commonModule.InvokeVoidAsync("scrollIntoViewEx", $"#{Id} #{column.Id}", "smooth", "nearest", "center");
			}
		}
	}

	/// <summary>
	/// Begins editing of the given item.
	/// </summary>
	public async Task BeginEditAsync()
	{
		if (IsEnabled && AllowEdit && !IsEditing && SelectionMode != TableSelectionMode.None && Selection.Count == 1 && KeyField != null)
		{
			// Find item to edit
			var item = ItemsToDisplay.Find(x => KeyField(x).ToString() == Selection[0]);
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

	public void OnEditInput(PDColumn<TItem> column, object? value) => _editValues[column.Id] = value;

	public void OnEditInput(string columnId, object? value) => _editValues[columnId] = value;

	private async Task OnFilterChanged(Filter filter)
	{
		var sb = new StringBuilder();
		foreach (var col in ActualColumnsToDisplay.Where(x => x.Filterable && x.Filter.IsValid))
		{
			sb.Append(' ').Append(col.Filter.ToString());
		}

		SearchText = sb.ToString().Trim();
		await SearchTextChanged.InvokeAsync(SearchText).ConfigureAwait(true);
		_lastSearchText = SearchText;

		await RefreshAsync(SearchText).ConfigureAwait(true);
	}

	/// <summary>
	/// Commits the current edit.
	/// </summary>
	public async Task CommitEditAsync()
	{
		var delta = new Dictionary<string, object?>();

		if (IsEditing && EditItem != null)
		{
			// Build dictionary of edit values
			var args = new TableAfterEditEventArgs<TItem>(EditItem);
			foreach (var column in ActualColumnsToDisplay)
			{
				if (_editValues.TryGetValue(column.Id, out object? value) && IsColumnInEditMode(column, EditItem))
				{
					args.NewValues.Add(column.Id, value);
				}
			}

			// Notify and allow cancel
			await AfterEdit.InvokeAsync(args).ConfigureAwait(true);
			if (!args.Cancel)
			{
				// apply edit value to each column
				foreach (var column in ActualColumnsToDisplay)
				{
					if (args.NewValues.TryGetValue(column.Id, out object? newValue))
					{
						try
						{
							column.SetValue(EditItem, newValue);
						}
						catch (Exception ex)
						{
							await ExceptionHandler.InvokeAsync(ex).ConfigureAwait(true);
							return;
						}
					}
				}
			}

			// save changes if configured to do so and data provider is given
			if (SaveChanges && DataProvider != null)
			{
				foreach (var kvp in _editValues)
				{
					if (kvp.Value != null)
					{
						var col = Columns.Find(x => x.Id == kvp.Key);
						if (col?.Field?.GetPropertyMemberInfo() is MemberInfo mi)
						{
							delta.Add(mi.Name, kvp.Value);
						}
					}
				}

				await DataProvider.UpdateAsync(EditItem, delta, default).ConfigureAwait(true);
			}

			IsEditing = false;

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
	/// Gets a dictionary of additional attributes to be added to the main div element.
	/// </summary>
	public Dictionary<string, object> DivAttributes
	{
		get
		{
			var dict = new Dictionary<string, object>();
			if (AllowDrop)
			{
				dict.Add("ondragover", "event.preventDefault();");
			}

			return dict;
		}
	}

	/// <summary>
	/// Gets a dictionary of additional attributes to be added to each row.
	/// </summary>
	public Dictionary<string, object> GetRowAttributes(TItem? item)
	{
		var dict = new Dictionary<string, object>();
		if (AllowDrag && !IsEditing)
		{
			dict.Add("draggable", "true");
			var downloadUrl = item is null ? null : DownloadUrlFunc(item);
			if (!string.IsNullOrWhiteSpace(downloadUrl))
			{
				dict.Add("data-downloadurl", downloadUrl);
			}
		}

		if (AllowDrop)
		{
			dict.Add("ondragover", "event.preventDefault();");
		}

		return dict;
	}

	/// <summary>
	/// Returns an array of all currently selected items.
	/// </summary>
	public TItem[] GetSelectedItems() => KeyField is null
			? []
			: [.. ItemsToDisplay.Where(x => Selection.Contains(KeyField(x).ToString() ?? string.Empty))];

	/// <summary>
	/// Clears the current selection.
	/// </summary>
	public async Task ClearSelectionAsync()
	{
		if (Selection.Count > 0)
		{
			Selection.Clear();
			await SelectionChanged.InvokeAsync(null).ConfigureAwait(true);
			StateHasChanged();
		}
	}

	/// <summary>
	/// Updates the paging criteria.
	/// </summary>
	/// <param name="criteria">New page criteria.</param>
	public void SetPageCriteria(PageCriteria criteria) => PageCriteria = criteria;

	/// <summary>
	/// Updates the sorting criteria.
	/// </summary>
	/// <param name="criteria">New sort criteria.</param>
	public void SetSortCriteria(SortCriteria criteria) => SortCriteria = criteria;

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
			}
		}
		catch
		{
		}
	}

	protected override async Task OnAfterRenderAsync(bool firstRender)
	{
		if (firstRender && JSRuntime is not null)
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

				_editTimer = new Timer(OnEditTimer, null, Timeout.Infinite, Timeout.Infinite);

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

			await Ready.InvokeAsync(null).ConfigureAwait(true);
		}

		// Focus first editor after edit mode begins
		if (BeginEditEvent.WaitOne(0) && Columns.Count > 0 && EditItem != null)
		{
			// find first editable column
			var key = string.Empty;
			foreach (var column in ActualColumnsToDisplay)
			{
				var editable = column.Editable;
				// Override with dynamic config?
				var config = ColumnsConfig?.Find(x => x.Id == column.Id);
				if (config?.Editable ?? editable)
				{
					key = column.Id;
					break;
				}
			}

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

	private async void PageCriteria_PageSizeChanged(object? sender, EventArgs e)
	{
		await RefreshAsync(SearchText).ConfigureAwait(true);
		StateHasChanged();
	}

	private async void PageCriteria_PageChanged(object? sender, EventArgs e)
	{
		if (PageCriteria != null)
		{
			await RefreshAsync(SearchText).ConfigureAwait(true);
			await PageChanged.InvokeAsync(PageCriteria).ConfigureAwait(true);
			StateHasChanged();
		}
	}

	private async Task<string[]> OnFetchFilterValuesAsync(PDColumn<TItem> column, Filter filter)
	{
		var result = Array.Empty<string>();

		if (column.Field is null)
		{
			return result;
		}

		// Allow app to specify suggested values
		if (column.FilterSuggestedValues.Any())
		{
			return [.. column.FilterSuggestedValues.Select(x => Filter.Format(x, filter.UnspecifiedDateTimesAreUtc))];
		}

		// TODO: cache values for period of time?

		// build up search text from filters
		var searchText = new StringBuilder();
		foreach (var col in ActualColumnsToDisplay.Where(c => c.Filterable))
		{
			if (col == column)
			{
				if (filter.IsValid)
				{
					_ = searchText.Append(filter.ToString()).Append(' ');
				}
			}
			else if (col.Filter.IsValid)
			{
				_ = searchText.Append(col.Filter.ToString()).Append(' ');
			}
		}

		// Base request on current filter and sort
		var sortColumn = Columns.Find(x => x.Id == SortCriteria?.Key || x.GetTitle() == SortCriteria?.Key);
		var request = new DataRequest<TItem>
		{
			Take = 1000,
			ForceUpdate = false,
			SortFieldExpression = sortColumn?.Field,
			SortDirection = sortColumn?.SortDirection,
			SearchText = searchText.ToString().Trim()
		};

		// Use more efficient service provider?
		var objectValues = Array.Empty<object>();
		if (DataProvider is IFilterProviderService<TItem> filterService && column.Field != null)
		{
			objectValues = await filterService.GetDistinctValuesAsync(request, column.Field).ConfigureAwait(true);
		}
		else
		{
			// Use main data provider - take has to be applied on base query
			var response = await DataProvider.GetDataAsync(request, default);
			objectValues = [.. response.Items
				.Where(x => column.GetValue(x) != null)
				.Select(x => column.GetValue(x)!.ToString() ?? string.Empty)
				.Distinct()
				.OrderBy(x => x)];
		}

		// Limit to first N
		objectValues = [.. objectValues.Where(x => x != null && x.ToString() != string.Empty).Take(column.FilterMaxValues ?? FilterMaxValues)];

		// Cast to string
		return [.. objectValues.Select(x => string.IsNullOrEmpty(column.Format)
				? x.ToString() ?? string.Empty
				: string.Format(CultureInfo.CurrentCulture, "{0:" + column.Format + "}", x)).Distinct()];
	}

	protected override void OnParametersSet()
	{
		// Has search text changed?
		// Only intercept when columns available to indicate state
		if (ActualColumnsToDisplay.Count > 0 && SearchText != _lastSearchText)
		{
			//Console.WriteLine($"OnParametersSet: SearchText = {SearchText}");
			_lastSearchText = SearchText;
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

	private async Task OnDivMouseDown(MouseEventArgs args)
	{
		// if mouse down event occurred straight from Div then clear selection
		if (!_mouseDownOriginatedFromTable && (args.Button != 2 || RightClickSelectsRow))
		{
			Selection.Clear();
			await SelectionChanged.InvokeAsync(null).ConfigureAwait(true);
		}

		_mouseDownOriginatedFromTable = false;
	}

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

	private async Task OnKeyDownAsync(KeyboardEventArgs args)
	{
		if (IsEditing)
		{
			switch (args.Code)
			{
				case "Escape":
					await CancelEdit().ConfigureAwait(true);
					break;

				case "Enter":
				case "Return":
					await CommitEditAsync().ConfigureAwait(true);
					break;
			}
		}
		else if (IsEnabled)
		{
			switch (args.Code)
			{
				case "F2":
					await BeginEditAsync().ConfigureAwait(true);
					break;

				case "KeyA":
					if (args.CtrlKey && SelectionMode == TableSelectionMode.Multiple)
					{
						await ClearSelectionAsync().ConfigureAwait(true);
						Selection.AddRange(ItemsToDisplay.Select(x => KeyField!(x).ToString() ?? string.Empty));
						await SelectionChanged.InvokeAsync(null).ConfigureAwait(true);
					}

					break;

				case "ArrowUp":
				case "ArrowDown":
				case "PageUp":
				case "PageDown":
				case "Home":
				case "End":
					if (Selection.Count == 1 && KeyField != null)
					{
						var stepSize = args.Code.StartsWith("Page", StringComparison.Ordinal) ? 10 : 1;
						var items = ItemsToDisplay.ToList();
						var item = items.Find(x => KeyField(x).ToString() == Selection[0]);
						if (item != null)
						{
							var idx = items.IndexOf(item);
							if (args.Code == "End" && idx < items.Count - 1)
							{
								var id = KeyField(items[^1]).ToString();
								Selection.Clear();
								if (!string.IsNullOrWhiteSpace(id))
								{
									Selection.Add(id);
									if (_commonModule != null)
									{
										await _commonModule.InvokeVoidAsync("scrollIntoView", id, false).ConfigureAwait(true);
									}
								}
							}
							else if (args.Code == "Home" && idx > 0)
							{
								var id = KeyField(items[0]).ToString();
								Selection.Clear();
								if (!string.IsNullOrWhiteSpace(id))
								{
									Selection.Add(id);
									if (_commonModule != null)
									{
										await _commonModule.InvokeVoidAsync("scrollIntoView", id, false).ConfigureAwait(true);
									}
								}
							}
							else if (args.Code.EndsWith("Up", StringComparison.Ordinal) && idx >= stepSize)
							{
								var id = KeyField(items[idx - stepSize]).ToString();
								Selection.Clear();
								if (!string.IsNullOrWhiteSpace(id))
								{
									Selection.Add(id);
									if (_commonModule != null)
									{
										await _commonModule.InvokeVoidAsync("scrollIntoView", id, false).ConfigureAwait(true);
									}
								}
							}
							else if (args.Code.EndsWith("Down", StringComparison.Ordinal) && idx < items.Count - stepSize)
							{
								var id = KeyField(items[idx + stepSize]).ToString();
								Selection.Clear();
								if (!string.IsNullOrWhiteSpace(id))
								{
									Selection.Add(id);
									if (_commonModule != null)
									{
										await _commonModule.InvokeVoidAsync("scrollIntoView", id, false).ConfigureAwait(true);
									}
								}
							}

							await SelectionChanged.InvokeAsync(null).ConfigureAwait(true);
						}
					}

					break;
			}
		}

		await KeyDown.InvokeAsync(args).ConfigureAwait(true);
	}

	private async Task OnRowMouseDownAsync(MouseEventArgs args, TItem item)
	{
		// quit if selection not allowed
		if (!IsEnabled || SelectionMode == TableSelectionMode.None || !RowIsEnabled(item))
		{
			return;
		}

		// if right-click on row then only select if clicked on label
		var selectRow = args.Button == 0;
		if (args.Button == 2 && RightClickSelectsRow && _commonModule != null)
		{
			var sourceEl = await _commonModule.InvokeAsync<ElementInfo>("getElementAtPoint", args.ClientX, args.ClientY).ConfigureAwait(true);
			if (sourceEl != null)
			{
				selectRow = sourceEl.Tag == "SPAN" || sourceEl.Tag == "IMG";
			}
		}

		if (selectRow)
		{
			var key = KeyField!(item)?.ToString();
			if (key != null)
			{
				var alreadySelected = Selection.Contains(key);

				// begin edit mode?
				if (AllowEdit && !IsEditing && Selection.Count == 1 && alreadySelected && !args.CtrlKey && args.Button == 0 && !EditOnDoubleClick)
				{
					_editTimer?.Change(100, Timeout.Infinite);
				}
				else
				{
					await SelectItemAsync(key, args.ShiftKey, args.CtrlKey).ConfigureAwait(true);
				}
			}
		}
	}

	private async Task OnRowMouseUpAsync(MouseEventArgs args, TItem item)
	{
		// quit if selection not allowed
		if (item is null || !IsEnabled || SelectionMode == TableSelectionMode.None || !RowIsEnabled(item))
		{
			return;
		}

		var key = KeyField!(item)?.ToString() ?? string.Empty;

		// if right-click on row then only select if clicked on label
		var selectRow = args.Button == 0;
		if (args.Button == 2 && RightClickSelectsRow && _commonModule != null)
		{
			// if row is not already selected - de-select all and select only this item
			if (!Selection.Contains(key))
			{
				selectRow = true;
			}

			//var sourceEl = await _commonModule.InvokeAsync<ElementInfo>("getElementAtPoint", args.ClientX, args.ClientY).ConfigureAwait(true);
			//if (sourceEl != null)
			//{
			//	selectRow = sourceEl.Tag == "SPAN" || sourceEl.Tag == "IMG";
			//}
		}

		if (selectRow)
		{

			if (key != null)
			{
				var alreadySelected = Selection.Contains(key);

				// begin edit mode?
				if (AllowEdit && !IsEditing && Selection.Count == 1 && alreadySelected && !args.CtrlKey && args.Button == 0 && !EditOnDoubleClick)
				{
					_editTimer?.Change(250, Timeout.Infinite);
				}
				else
				{
					await SelectItemAsync(key, args.ShiftKey, args.CtrlKey).ConfigureAwait(true);
				}
			}
		}
	}

	private void OnRowClick(MouseEventArgs _, TItem item)
	{
		if (IsEnabled)
		{
			Click.InvokeAsync(item);
		}
	}

	private async Task OnRowDoubleClick(MouseEventArgs _, TItem item)
	{
		// cancel pending edit mode
		_editTimer?.Change(Timeout.Infinite, Timeout.Infinite);

		if (EditOnDoubleClick)
		{
			await BeginEditAsync();
		}

		if (IsEnabled)
		{
			await DoubleClick.InvokeAsync(item);
		}
	}

	public async Task OnToggleAllSelection(bool on)
	{
		if (IsEnabled && KeyField != null)
		{
			if (on)
			{
				foreach (var item in ItemsToDisplay)
				{
					var key = KeyField(item).ToString();
					if (key != null && !Selection.Contains(key) && RowIsEnabled(item))
					{
						Selection.Add(key);
					}
				}
			}
			else
			{
				foreach (var item in ItemsToDisplay)
				{
					var key = KeyField(item).ToString();
					if (key != null && Selection.Contains(key) && RowIsEnabled(item))
					{
						Selection.Remove(key);
					}
				}
			}

			await SelectionChanged.InvokeAsync(null).ConfigureAwait(true);
		}
	}

	public async Task OnToggleSelection(TItem item, bool on)
	{
		if (IsEnabled && KeyField != null && RowIsEnabled(item))
		{
			var key = KeyField(item).ToString();
			if (on && key != null && !Selection.Contains(key))
			{
				Selection.Add(key);
			}
			else if (key != null && Selection.Contains(key))
			{
				Selection.Remove(key);
			}

			await SelectionChanged.InvokeAsync(null).ConfigureAwait(true);
		}
	}

	private void OnTableMouseDown(MouseEventArgs _) =>
		// store fact that mouse down occurred from Table element
		_mouseDownOriginatedFromTable = true;

	private void OnEditTimer(object? state)
	{
		if (IsEnabled && !_dragging)
		{
			Task.Run(async () => await BeginEditAsync().ConfigureAwait(true));
		}
	}

	private string GetDynamicCellClasses(PDColumn<TItem> col, TItem _)
	{
		var sb = new StringBuilder();
		sb.Append(col.TdClass);
		sb.Append(' ');
		if ((col.UserSelectable ?? UserSelectable) == false)
		{
			sb.Append("noselect ");
		}

		return sb.ToString().Trim();
	}

	private string GetDynamicRowClasses(TItem item)
	{
		var sb = new StringBuilder();
		if (IsSelected(item))
		{
			sb.Append("selected ");
		}

		if (!RowIsEnabled(item))
		{
			sb.Append("disabled ");
		}

		if (RowClass != null)
		{
			var classes = RowClass(item);
			if (!string.IsNullOrWhiteSpace(classes))
			{
				sb.Append(classes);
			}
		}

		return sb.ToString().Trim();
	}

	private bool IsColumnInEditMode(PDColumn<TItem> column, TItem item)
	{
		// is editing current row?
		if (IsEditing && item == EditItem)
		{
			var editable = column.Editable;
			// override with dynamic config?
			var config = ColumnsConfig?.Find(x => x.Id == column.Id);
			return config?.Editable ?? editable;
		}

		return false;
	}

	public bool IsSelected(TItem item)
	{
		if (SelectionMode != TableSelectionMode.None && KeyField != null)
		{
			return Selection.Contains(KeyField(item)?.ToString() ?? string.Empty);
		}

		return false;
	}

	private async Task OnRowDragStart(DragEventArgs args, TItem? rowItem)
	{
		if (!IsEnabled || IsEditing)
		{
			return;
		}

		_dragging = true;

		// need to set the data being dragged
		if (DragContext != null && KeyField != null)
		{
			// get all items to be dragged
			var items = new List<TItem>();

			// if item that initiated drag is in selection then drag entire selection
			// otherwise change selection to single item and drag that
			if (KeyField != null && rowItem != null)
			{
				string key = KeyField(rowItem).ToString() ?? string.Empty;
				if (!Selection.Contains(key))
				{
					await SelectItemAsync(KeyField(rowItem).ToString() ?? string.Empty, args.ShiftKey, args.CtrlKey);
				}
			}

			foreach (var key in Selection)
			{
				var item = ItemsToDisplay.Find(x => KeyField(x).ToString() == key);
				if (item != null)
				{
					items.Add(item);
				}
			}

			DragContext.Payload = items;
		}
	}

	private void OnDragEnd(DragEventArgs _) => _dragging = false;

	private async Task OnRowDragDropAsync(MouseEventArgs args, TItem row)
	{
		if (IsEnabled && DragContext != null)
		{
			await Drop.InvokeAsync(new DropEventArgs(row, DragContext.Payload, args.CtrlKey)).ConfigureAwait(true);
		}
	}

	private async Task OnDragDropAsync(DragEventArgs args)
	{
		if (IsEnabled && DragContext != null)
		{
			await Drop.InvokeAsync(new DropEventArgs(null, DragContext.Payload, args.CtrlKey)).ConfigureAwait(true);
		}
	}

	public async Task SelectItemAsync(string key, bool shiftKey = false, bool ctrlKey = false)
	{
		if (string.IsNullOrWhiteSpace(key))
		{
			return;
		}

		var alreadySelected = Selection.Contains(key);
		if (SelectionMode == TableSelectionMode.Single)
		{
			if (!alreadySelected)
			{
				Selection.Clear();
				Selection.Add(key);
				await SelectionChanged.InvokeAsync(null).ConfigureAwait(true);
			}
		}
		else if (SelectionMode == TableSelectionMode.Multiple)
		{
			if (shiftKey && Selection.Count > 0) // range selection (from last selected to row clicked on)
			{
				Selection.RemoveRange(0, Selection.Count - 1);
				var idxFrom = ItemsToDisplay.FindIndex(x => KeyField!(x)?.ToString() == Selection[0]);
				var idxTo = ItemsToDisplay.FindIndex(x => KeyField!(x)?.ToString() == key);
				if (idxFrom > -1 && idxTo > -1)
				{
					Selection.Clear();
					Selection.AddRange(ItemsToDisplay
						.GetRange(Math.Min(idxFrom, idxTo), (Math.Max(idxFrom, idxTo) - Math.Min(idxFrom, idxTo)) + 1)
						.Select(x => KeyField!(x).ToString() ?? string.Empty));
					await SelectionChanged.InvokeAsync(null).ConfigureAwait(true);
				}
			}
			else if (ctrlKey) // toggle selection
			{
				if (alreadySelected)
				{
					Selection.Remove(key);
				}
				else
				{
					Selection.Add(key);
				}

				await SelectionChanged.InvokeAsync(null).ConfigureAwait(true);
			}
			else // single selection
			{
				Selection.Clear();
				Selection.Add(key);
				await SelectionChanged.InvokeAsync(null).ConfigureAwait(true);
			}
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

	public void Disable()
	{
		IsEnabled = false;
		StateHasChanged();
	}

	public void Enable()
	{
		IsEnabled = true;
		StateHasChanged();
	}

	public void SetEnabled(bool isEnabled)
	{
		IsEnabled = isEnabled;
		StateHasChanged();
	}

	#endregion
}