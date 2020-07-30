using System;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Collections.Generic;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using PanoramicData.Blazor.Services;
using PanoramicData.Blazor.Exceptions;

namespace PanoramicData.Blazor
{
	public partial class PDTable<TItem> : ISortableComponent, IPageableComponent
	{
		/// <summary>
		/// Injected log service.
		/// </summary>
		[Inject] protected ILogger<PDTable<TItem>> Logger { get; set; } = new NullLogger<PDTable<TItem>>();

		/// <summary>
		/// Injected navigation manager.
		/// </summary>
		[Inject] protected NavigationManager NavigationManager { get; set; } = null!;

		/// <summary>
		/// Child HTML content.
		/// </summary>
		[Parameter] public RenderFragment ChildContent { get; set; } = null!;

		/// <summary>
		/// Gets or sets the IDataProviderService instance to use to fetch data.
		/// </summary>
		[Parameter] public IDataProviderService<TItem> DataProvider { get; set; } = null!;

		/// <summary>
		/// A Linq expression that selects the item field that contains the key value.
		/// </summary>
		[Parameter] public Func<TItem, object>? KeyField { get; set; }

		/// <summary>
		/// Gets or sets the CSS class to apply to the tables container element.
		/// </summary>
		[Parameter] public string TableClass { get; set; } = string.Empty;

		/// <summary>
		/// Gets or sets the CSS class to apply to the table header element.
		/// </summary>
		[Parameter] public string THeadClass { get; set; } = string.Empty;

		/// <summary>
		/// Gets or sets a function that calculates and returns CSS Classes for the row (TR element).
		/// </summary>
		[Parameter] public Func<TItem, string>? RowClass { get; set; }

		/// <summary>
		/// Gets or sets the message to be displayed when no data is available.
		/// </summary>
		[Parameter] public string NoDataMessage { get; set; } = "No data";

		/// <summary>
		/// Gets or sets a delegate to be called if an exception occurs.
		/// </summary>
		[Parameter] public EventCallback<Exception> ExceptionHandler { get; set; }

		/// <summary>
		/// Gets or sets the default sort criteria.
		/// </summary>
		[Parameter] public SortCriteria? DefaultSort { get; set; }

		/// <summary>
		/// Event callback fired whenever the sort criteria has changed.
		/// </summary>
		[Parameter] public EventCallback<SortCriteria> SortChanged { get; set; }

		/// <summary>
		/// Gets or sets the default page criteria.
		/// </summary>
		[Parameter] public PageCriteria? DefaultPage { get; set; }

		/// <summary>
		/// Callback fired whenever the component changes the currently displayed page.
		/// </summary>
		[Parameter] public EventCallback<PageCriteria> PageChanged { get; set; }

		/// <summary>
		/// Search text to be passed to IDataProvider when querying for data.
		/// </summary>
		[Parameter] public string? SearchText { get; set; }

		/// <summary>
		/// Allows an application defined configuration to be applied to the available columns
		/// at runtime.
		/// </summary>
		[Parameter] public List<PDColumnConfig>? ColumnsConfig { get; set; }

		/// <summary>
		/// Gets or sets whether selection is enabled and the method in which it works.
		/// </summary>
		[Parameter] public TableSelectionMode SelectionMode { get; set; }

		/// <summary>
		/// Callback fired whenever the current selection changes.
		/// </summary>
		[Parameter] public EventCallback SelectionChanged { get; set; }

		/// <summary>
		/// Callback fired whenever the user clicks on a given item.
		/// </summary>
		[Parameter] public EventCallback<TItem> Click { get; set; }

		/// <summary>
		/// Callback fired whenever the user double-clicks on a given item.
		/// </summary>
		[Parameter] public EventCallback<TItem> DoubleClick { get; set; }

		/// <summary>
		/// Action called whenever data items are loaded.
		/// </summary>
		/// <remarks>The action allows the items to be modified by the calling application.</remarks>
		[Parameter] public Action<List<TItem>>? ItemsLoaded { get; set; }

		/// <summary>
		/// Gets whether the table allows in-place editing.
		/// </summary>
		[Parameter] public bool AllowEdit { get; set; }

		/// <summary>
		/// Gets a full list of all columns.
		/// </summary>
		public List<PDColumn<TItem>> Columns { get; } = new List<PDColumn<TItem>>();

		/// <summary>
		/// Gets the current selection.
		/// </summary>
		public List<string> Selection { get; } = new List<string>();

		/// <summary>
		/// Gets a calculated list of actual columns to be displayed.
		/// </summary>
		protected List<PDColumn<TItem>> ActualColumnsToDisplay
		{
			get
			{
				var availableColumnIds = Columns
									.Select(c => c.Id)
									.ToList();

				var columns =
					ColumnsConfig?
						.Where(columnConfig => availableColumnIds.Contains(columnConfig.Id))
						.Select(columnConfig =>
						{
							var dTColumn = Columns.Single(c => c.Id == columnConfig.Id);
		   					if (columnConfig.Title != default)
							{
								dTColumn.Title = columnConfig.Title;
							}
							return dTColumn;
						})
					?? Columns;

				return columns
					.Where(c => c.ShowInList)
					.ToList();
			}
		}

		/// <summary>
		/// Gets the items to be displayed as rows.
		/// </summary>
		public IEnumerable<TItem> ItemsToDisplay { get; private set; } = Enumerable.Empty<TItem>();

		/// <summary>
		/// Has the table been initialized?
		/// </summary>
		protected bool TableInitialised { get; set; } = false;

		/// <summary>
		/// Current page number, when paging enabled.
		/// </summary>
		protected int Page { get; set; } = 1;

		/// <summary>
		/// When set, turns on paging and specifies the number of displayed items per page.
		/// </summary>
		protected int? PageSize { get; set; }

		/// <summary>
		/// Total number of pages available.
		/// </summary>
		protected int? PageCount { get; set; }

		protected override void OnInitialized()
		{
			if (DataProvider is null)
			{
				throw new PDTableException($"{nameof(DataProvider)} must not be null.");
			}
		}

		protected async override Task OnParametersSetAsync()
		{
			// validate parameter constraints
			if (SelectionMode != TableSelectionMode.None && KeyField == null)
			{
				throw new PDTableException("KeyField attribute must be specified when enabling selection.");
			}
			await RefreshAsync().ConfigureAwait(true);
		}

		protected override async Task OnAfterRenderAsync(bool firstRender)
		{
			// If this is the first time we've finished rendering, then all the columns
			// have been added to the table so we'll go and get the data for the first time
			if (firstRender)
			{
				try
				{
					// default sort
					if (DefaultSort != null)
					{
						var columnToSortBy = Columns.SingleOrDefault(c => c.Id == DefaultSort.Key || c.Title == DefaultSort.Key);
						if (columnToSortBy != null)
						{
							await columnToSortBy.SortByAsync(DefaultSort.Direction).ConfigureAwait(true);
						}
					}

					// default page
					if(DefaultPage != null)
					{
						Page = DefaultPage.Page;
						PageSize = DefaultPage.PageSize;
					}
				}
				catch (Exception ex)
				{
					await HandleExceptionAsync(ex).ConfigureAwait(true);
				}
				finally
				{
					TableInitialised = true;
					StateHasChanged();
				}

				try
				{
					await GetDataAsync().ConfigureAwait(true);
					StateHasChanged();
				}
				catch (Exception ex)
				{
					await HandleExceptionAsync(ex).ConfigureAwait(true);
				}
			}
		}

		/// <summary>
		/// Adds the given column to the list of available columns.
		/// </summary>
		/// <param name="column">The PDColumn to be added.</param>
		public async Task AddColumnAsync(PDColumn<TItem> column)
		{
			try
			{
				Columns.Add(column);
				StateHasChanged();
			}
			catch (Exception ex)
			{
				await HandleExceptionAsync(ex).ConfigureAwait(false);
			}
		}

		/// <summary>
		/// Centralized method to process exceptions.
		/// </summary>
		/// <param name="ex">Exception that has been raised.</param>
		public async Task HandleExceptionAsync(Exception ex)
		{
			Logger.LogError(ex, ex.Message);
			await ExceptionHandler.InvokeAsync(ex).ConfigureAwait(true);
		}

		/// <summary>
		/// Refresh the grid by performing a re-query.
		/// </summary>
		public Task RefreshAsync()
			=> GetDataAsync();

		/// <summary>
		/// Instructs the component to show the specified page.
		/// </summary>
		/// <param name="criteria">Details of the page to be displayed.</param>
		public async Task PageAsync(PageCriteria criteria)
		{
			if (PageSize.HasValue && criteria.Page > 0 && (!PageCount.HasValue || criteria.Page <= PageCount))
			{
				Page = criteria.Page;
				PageSize = criteria.PageSize;
				await GetDataAsync().ConfigureAwait(true);
				await PageChanged.InvokeAsync(criteria).ConfigureAwait(true);
			}
		}

		/// <summary>
		/// Sort the displayed items.
		/// </summary>
		/// <param name="criteria">Details of the sort operation to be performed.</param>
		public Task SortAsync(SortCriteria criteria)
		{
			var column = Columns.SingleOrDefault(c => string.Equals(c.PropertyInfo?.Name, criteria.Key, StringComparison.InvariantCultureIgnoreCase));
			if (column != null)
			{
				return SortBy(column, criteria.Direction);
			}
			return Task.CompletedTask;
		}

		/// <summary>
		/// Requests data from the data provider using the current settings.
		/// </summary>
		protected async Task GetDataAsync()
		{
			try
			{
				var sortColumn = Columns.SingleOrDefault(c => c.SortColumn);
				var request = new DataRequest<TItem>
				{
					Skip = 0,
					ForceUpdate = false,
					SortFieldExpression = sortColumn?.Field,
					SortDirection = sortColumn?.SortDirection,
					SearchText = SearchText
				};

				// paging
				if (PageSize.HasValue)
				{
					request.Take = PageSize.Value;
					request.Skip = (Page - 1) * PageSize.Value;
				}

				// perform query data
				var response = await DataProvider
					.GetDataAsync(request, CancellationToken.None)
					.ConfigureAwait(true);

				if (PageSize.HasValue)
				{
					PageCount = (response.TotalCount / PageSize.Value) + (response.TotalCount % PageSize.Value > 0 ? 1 : 0);
				}

				// allow calling application to filter/add items etc
				var items = new List<TItem>(response.Items);
				ItemsLoaded?.Invoke(items); // must use an action here and not an EventCallaback as that leads to infinite loop and 100% CPU
				ItemsToDisplay = items;
			}
			finally
			{
			}
		}

		/// <summary>
		/// Sort the data by the specified column.
		/// </summary>
		/// <param name="column">The column to sort by.</param>
		/// <remarks>To disable sorting for any given column, set its Sortable property set to false.</remarks>
		protected async Task SortBy(PDColumn<TItem> column, SortDirection? direction = null)
		{
			if (column.Sortable)
			{
				await column.SortByAsync(direction).ConfigureAwait(true);
				await GetDataAsync().ConfigureAwait(true);
				await SortChanged.InvokeAsync(new SortCriteria { Key = column.Id, Direction = direction ?? column.SortDirection }).ConfigureAwait(true);
			}
		}

		private async Task OnRowMouseDown(MouseEventArgs args, TItem item)
		{
			var key = KeyField!(item)?.ToString();
			if (key != null)
			{
				var alreadySelected = Selection.Contains(key);
				if (SelectionMode == TableSelectionMode.Single)
				{
					if(alreadySelected)
					{
						// if edit allow and not editing and was already selected then begin edit
					}
					else
					{
						Selection.Clear();
						Selection.Add(key);
						await SelectionChanged.InvokeAsync(null).ConfigureAwait(true);
					}
				}
				else if (SelectionMode == TableSelectionMode.Multiple)
				{
					if(args.CtrlKey)
					{
						// toggle current selection
						if(alreadySelected)
						{
							Selection.Remove(key);
						}
						else
						{
							Selection.Add(key);
						}
						await SelectionChanged.InvokeAsync(null).ConfigureAwait(true);
					}
					else
					{
						if (alreadySelected)
						{
							// if edit allow and not editing and was already selected then begin edit
						}
						else
						{
							Selection.Clear();
							Selection.Add(key);
							await SelectionChanged.InvokeAsync(null).ConfigureAwait(true);
						}
					}
				}
			}
		}

		private string GetDynamicRowClasses(TItem item)
		{
			var sb = new StringBuilder();
			if(SelectionMode != TableSelectionMode.None)
			{
				var key = KeyField!(item).ToString();
				if(Selection.Contains(key))
				{
					sb.Append("selected ");
				}
			}
			if (RowClass != null)
			{
				var classes = RowClass(item);
				if(!string.IsNullOrWhiteSpace(classes))
				{
					sb.Append(classes);
				}
			}
			return sb.ToString().Trim();
		}

		private void ContextMenuHandler(MouseEventArgs args, TItem item)
		{
		}
	}
}
