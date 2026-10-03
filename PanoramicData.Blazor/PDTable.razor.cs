using PanoramicData.Blazor.Helpers;

namespace PanoramicData.Blazor;

/// <summary>
/// Data table component with sorting, filtering, paging, selection, editing, and state persistence support.
/// </summary>
/// <typeparam name="TItem">Row item type.</typeparam>
public partial class PDTable<TItem> :
	ISortableComponent,
	IPageableComponent,
	IAsyncDisposable,
	IEnablable
	where TItem : class
{
	private bool _dragging;
	private Timer? _editTimer;
	private string? _lastSearchText;
	private string? _lastViewKey;
	private int _lastColumnCount;
	private IJSObjectReference? _commonModule;
	private bool _mouseDownOriginatedFromTable;
	private readonly string _idEditPrefix = "pd-table-edit-";
	private TableBeforeEditEventArgs<TItem>? _tableBeforeEditArgs;
	private readonly Dictionary<string, object?> _editValues = [];

	private ManualResetEvent BeginEditEvent { get; set; } = new ManualResetEvent(false);

	/// <summary>
	/// Gets or sets logger used by the component.
	/// </summary>
	[Inject] protected ILogger<PDTable<TItem>> Logger { get; set; } = new NullLogger<PDTable<TItem>>();

	/// <summary>
	/// Gets or sets navigation manager.
	/// </summary>
	[Inject] protected NavigationManager NavigationManager { get; set; } = null!;

	/// <summary>
	/// Gets or sets JavaScript runtime used by the component.
	/// </summary>
	[Inject] public IJSRuntime JSRuntime { get; set; } = null!;

	/// <summary>
	/// Gets or sets block overlay service used during data operations.
	/// </summary>
	[Inject] protected IBlockOverlayService BlockOverlayService { get; set; } = null!;

	/// <summary>
	/// Gets or sets drag context for row drag-and-drop operations.
	/// </summary>
	[CascadingParameter] public PDDragContext? DragContext { get; set; }

	/// <summary>
	/// Gets or sets state manager used for table state persistence.
	/// </summary>
	[CascadingParameter] public IAsyncStateManager? StateManager { get; set; }

	#region Parameters

	/// <summary>
	/// Gets or sets whether columns can be resized.
	/// </summary>
	[Parameter] public bool AllowColumnResize { get; set; } = true;

	/// <summary>
	/// Gets or sets whether columns can be sorted.
	/// </summary>
	[Parameter] public bool AllowColumnSort { get; set; } = true;

	/// <summary>
	/// Gets or sets whether new items can be created.
	/// </summary>
	[Parameter] public bool AllowCreate { get; set; }

	/// <summary>
	/// Gets or sets whether items can be deleted.
	/// </summary>
	[Parameter] public bool AllowDelete { get; set; }

	/// <summary>
	/// Gets or sets whether rows can be dragged.
	/// </summary>
	[Parameter] public bool AllowDrag { get; set; }

	/// <summary>
	/// Gets or sets whether items can be dropped onto the table.
	/// </summary>
	[Parameter] public bool AllowDrop { get; set; }

	/// <summary>
	/// Gets or sets whether items can be edited.
	/// </summary>
	[Parameter] public bool AllowEdit { get; set; }

	/// <summary>
	/// Gets or sets whether multiple rows can be selected.
	/// </summary>
	[Parameter] public bool AllowMultiSelect { get; set; }

	/// <summary>
	/// Gets or sets whether paging is enabled.
	/// </summary>
	[Parameter] public bool AllowPaging { get; set; } = true;

	/// <summary>
	/// Gets or sets whether rows can be selected.
	/// </summary>
	[Parameter] public bool AllowSelection { get; set; } = true;

	/// <summary>
	/// An event callback that is invoked before a new item is created.
	/// </summary>
	[Parameter] public EventCallback<DataRequest<TItem>> BeforeCreate { get; set; }

	/// <summary>
	/// An event callback that is invoked before an item is deleted.
	/// </summary>
	[Parameter] public EventCallback<TItem> BeforeDelete { get; set; }

	/// <summary>
	/// Gets or sets the size of the buttons in the toolbar.
	/// </summary>
	[Parameter] public ButtonSizes ButtonSize { get; set; } = ButtonSizes.Medium;

	/// <summary>
	/// Gets or sets the child content of the component, which is typically a set of PDColumn components.
	/// </summary>
	[Parameter] public RenderFragment? ChildContent { get; set; }

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
	/// An event callback that is invoked when an item is dropped onto the table.
	/// </summary>
	[Parameter] public EventCallback<DropEventArgs> Drop { get; set; }

	/// <summary>
	/// Gets or sets a delegate to be called if an exception occurs.
	/// </summary>
	[Parameter] public EventCallback<Exception> ExceptionHandler { get; set; }

	/// <summary>
	/// Gets or sets whether the export button is shown.
	/// </summary>
	[Parameter] public bool ExportButton { get; set; } = true;

	/// <summary>
	/// A template for the table footer.
	/// </summary>
	[Parameter] public RenderFragment? FooterTemplate { get; set; }

	/// <summary>
	/// A template for the table header.
	/// </summary>
	[Parameter] public RenderFragment? HeaderTemplate { get; set; }

	/// <summary>
	/// Gets or sets the height of the table.
	/// </summary>
	[Parameter] public string Height { get; set; } = "100%";

	/// <summary>
	/// Gets or sets the unique identifier for the component.
	/// </summary>
	[Parameter] public string Id { get; set; } = $"pd-table-{ComponentIdSequence.Next()}";

	/// <summary>
	/// Gets or sets whether the table is currently loading data.
	/// </summary>
	[Parameter] public bool IsLoading { get; set; }

	/// <summary>
	/// Gets or sets the maximum number of possible filter values to show.
	/// </summary>
	[Parameter] public int FilterMaxValues { get; set; } = 50;

	/// <summary>
	/// Gets or sets whether table interaction is enabled.
	/// </summary>
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
	/// Gets or sets the CSS class to apply to the Pager, if present
	/// </summary>
	[Parameter] public string PagerCssClass { get; set; } = string.Empty;

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
	/// Callback fired whenever the page size changes.
	/// </summary>
	[Parameter] public EventCallback<PageCriteria> PageSizeChanged { get; set; }

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
	/// Gets or sets whether a refresh keeps the selection (issue #151). A refresh is a fetch for the same
	/// search text, sort and page as the previous one, such as <see cref="RefreshAsync()"/> on an
	/// auto-refreshing page. When true, the selection is kept, keys whose rows have gone are dropped, and
	/// <see cref="SelectionChanged"/> is raised only if that changed the selection. When false (the default),
	/// every fetch clears the selection unless <see cref="RetainSelectionOnPage"/> is set, as before.
	/// </summary>
	/// <remarks>
	/// Off by default because callers that cache the selected row and rely on a refresh to clear it would
	/// otherwise keep a stale copy. With this on, read the selection back with <see cref="GetSelectedItems"/>
	/// after a refresh rather than holding row objects across it.
	/// </remarks>
	[Parameter] public bool RetainSelectionOnRefresh { get; set; }

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

	/// <summary>
	/// Gets or sets whether editing begins on double click instead of single click selection.
	/// </summary>
	[Parameter] public bool EditOnDoubleClick { get; set; }

	/// <summary>
	/// Gets or sets the maximum height of the table container. When set, enables scrollable mode.
	/// </summary>
	/// <remarks>Use CSS values like "400px", "50vh", etc. When null, the table renders as before without scrolling.</remarks>
	[Parameter] public string? MaxHeight { get; set; }

	/// <summary>
	/// Gets or sets whether the table header (thead) stays visible at the top while the body scrolls.
	/// Only applies when MaxHeight is set.
	/// </summary>
	[Parameter] public bool StickyHeader { get; set; }

	/// <summary>
	/// Gets or sets whether the pager stays at the bottom outside the scroll area.
	/// Only applies when MaxHeight is set.
	/// </summary>
	[Parameter] public bool StickyPager { get; set; }

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
				.Where(c => c.ShowInList && c.State.Visible
					&& ColumnGroupHelper.IsInActiveGroup(c.GroupName, ActiveColumnGroup))
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
	/// Gets whether the table is currently in edit mode.
	/// </summary>
	public bool IsEditing { get; private set; }

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
	/// Gets a dictionary of additional attributes to be added to the main div element.
	/// </summary>
	public Dictionary<string, object> DivAttributes
	{
		get
		{
			return [];
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

		return dict;
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

	/// <summary>
	/// Disables table interaction.
	/// </summary>
	public void Disable()
	{
		IsEnabled = false;
		StateHasChanged();
	}

	/// <summary>
	/// Enables table interaction.
	/// </summary>
	public void Enable()
	{
		IsEnabled = true;
		StateHasChanged();
	}

	/// <summary>
	/// Sets whether table interaction is enabled.
	/// </summary>
	/// <param name="isEnabled">True to enable interaction; otherwise false.</param>
	public void SetEnabled(bool isEnabled)
	{
		IsEnabled = isEnabled;
		StateHasChanged();
	}

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
					column.Filter.UpdateFrom(SearchText);
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
