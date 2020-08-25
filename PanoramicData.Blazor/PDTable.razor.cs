﻿using System;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Collections.Generic;
using Microsoft.JSInterop;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using PanoramicData.Blazor.Services;
using PanoramicData.Blazor.Exceptions;

namespace PanoramicData.Blazor
{
	public partial class PDTable<TItem> : ISortableComponent, IPageableComponent, IDisposable where TItem: class
	{
		public readonly string IdPrefix = "pd-table-";
		public readonly string IdEditPrefix = "pd-table-edit-";
		private static int _idSequence;
		private bool _dragging;
		private Timer? _editTimer;
		private TableBeforeEditEventArgs<TItem>? _tableBeforeEditArgs;

		private ManualResetEvent BeginEditEvent { get; set; } = new ManualResetEvent(false);
		/// <summary>
		/// Provides access to the parent DragContext if it exists.
		/// </summary>
		[CascadingParameter] public PDDragContext? DragContext { get; set; }

		/// <summary>
		/// Injected log service.
		/// </summary>
		[Inject] protected ILogger<PDTable<TItem>> Logger { get; set; } = new NullLogger<PDTable<TItem>>();

		/// <summary>
		/// Injected navigation manager.
		/// </summary>
		[Inject] protected NavigationManager NavigationManager { get; set; } = null!;

		/// <summary>
		/// Injected javascript interop object.
		/// </summary>
		[Inject] public IJSRuntime? JSRuntime { get; set; }

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
		/// Gets or sets the CSS class to apply to the container element.
		/// </summary>
		[Parameter] public string CssClass { get; set; } = string.Empty;

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
		/// Gets or sets whether rows may be dragged.
		/// </summary>
		[Parameter] public bool AllowDrag { get; set; }

		/// <summary>
		/// Gets or sets whether items may be dropped onto rows.
		/// </summary>
		[Parameter] public bool AllowDrop { get; set; }

		/// <summary>
		/// Callback fired before an item edit begins.
		/// </summary>
		[Parameter] public EventCallback<TableBeforeEditEventArgs<TItem>> BeforeEdit { get; set; }

		/// <summary>
		/// Callback fired after an item edit ends.
		/// </summary>
		[Parameter] public EventCallback<TableAfterEditEventArgs<TItem>> AfterEdit { get; set; }

		/// <summary>
		/// Callback fired whenever a drag operation ends on a row within a DragContext.
		/// </summary>
		[Parameter] public EventCallback<DropEventArgs> Drop { get; set; }

		/// <summary>
		/// Gets the unique identifier of this table.
		/// </summary>
		public string Id { get; private set; } = string.Empty;

		/// <summary>
		/// Gets the current item being edited.
		/// </summary>
		public TItem? EditItem { get; private set; }

		/// <summary>
		/// Gets a full list of all columns.
		/// </summary>
		public List<PDColumn<TItem>> Columns { get; } = new List<PDColumn<TItem>>();

		/// <summary>
		/// Gets the keys of all currently selected items.
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
								dTColumn.SetTitle(columnConfig.Title);
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
		public List<TItem> ItemsToDisplay { get; private set; } = new List<TItem>();

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
		/// <param name="searchText">Optional override for the search text.</param>
		public Task RefreshAsync(string? searchText = null)
			=> GetDataAsync(searchText);

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
		/// <param name="searchText">Optional override for the search text.</param>
		protected async Task GetDataAsync(string? searchText = null)
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
					SearchText = searchText ?? SearchText
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

		/// <summary>
		/// Begins editing of the given item.
		/// </summary>
		public async Task BeginEdit()
		{
			if (AllowEdit && !IsEditing && SelectionMode != TableSelectionMode.None && Selection.Count == 1 && KeyField != null)
			{
				// find item to edit
				var item = ItemsToDisplay.Find(x => KeyField(x).ToString() == Selection[0]);
				if (item != null)
				{
					// notify and allow for cancel
					EditItem = item;
					_tableBeforeEditArgs = new TableBeforeEditEventArgs<TItem>(EditItem);
					await InvokeAsync(async () => await BeforeEdit.InvokeAsync(_tableBeforeEditArgs).ConfigureAwait(true)).ConfigureAwait(true);
					if (!_tableBeforeEditArgs.Cancel)
					{
						IsEditing = true;
						BeginEditEvent.Set();
						await InvokeAsync(StateHasChanged).ConfigureAwait(true);
					}
				}
			}
		}

		/// <summary>
		/// Commits the current edit.
		/// </summary>
		public async Task CommitEdit()
		{
			if (IsEditing && EditItem != null)
			{
				// build dictionary of edit values
				var args = new TableAfterEditEventArgs<TItem>(EditItem);
				foreach (var column in ActualColumnsToDisplay)
				{
					if (IsColumnInEditMode(column, EditItem))
					{
						var newValue = await JSRuntime.InvokeAsync<string>("getValue", $"{IdEditPrefix}{column.Id}").ConfigureAwait(true);
						args.NewValues.Add(column.Id, newValue);
					}
				}

				// notify and allow cancel
				await AfterEdit.InvokeAsync(args).ConfigureAwait(true);
				if (!args.Cancel)
				{
					// apply edit value to each column
					foreach (var column in ActualColumnsToDisplay)
					{
						if (IsColumnInEditMode(column, EditItem))
						{
							// get new value and attempt to set on edit item
							var newValue = args.NewValues[column.Id];
							try
							{
								column.SetValue(EditItem, newValue);
							}
							catch (Exception ex)
							{
								throw new PDTableException($"Failed to assign value {newValue} to column {column.Id}", ex);
							}
						}
					}
				}

				StateHasChanged();
				EditItem = null;
				IsEditing = false;
				JSRuntime?.InvokeVoidAsync("focus", Id);
			}
		}

		/// <summary>
		/// Cancels the current edit.
		/// </summary>
		public void CancelEdit()
		{
			if(IsEditing)
			{
				EditItem = null;
				IsEditing = false;
				JSRuntime?.InvokeVoidAsync("focus", Id);
			}
		}

		/// <summary>
		/// Gets a dictionary of additional attributes to be added to each row.
		/// </summary>
		public Dictionary<string, object> RowAttributes
		{
			get
			{
				var dict = new Dictionary<string, object>();
				if(AllowDrag)
				{
					dict.Add("draggable", "true");
				}
				if (AllowDrop)
				{
					dict.Add("ondragover", "event.preventDefault();");
				}
				return dict;
			}
		}

		protected override void OnInitialized()
		{
			Id = $"{IdPrefix}{++_idSequence}";
			if (DataProvider is null)
			{
				throw new PDTableException($"{nameof(DataProvider)} must not be null.");
			}
			_editTimer = new Timer(OnEditTimer, null, Timeout.Infinite, Timeout.Infinite);
		}

		protected override void OnParametersSet()
		{
			// validate parameter constraints
			if (SelectionMode != TableSelectionMode.None && KeyField == null)
			{
				throw new PDTableException("KeyField attribute must be specified when enabling selection.");
			}
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
					if (DefaultPage != null)
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

			// focus first editor after edit mode begins
			if (BeginEditEvent.WaitOne(0) && Columns.Count > 0)
			{
				// find first editable column
				var key = string.Empty;
				foreach(var column in Columns)
				{
					var editable = column.Editable;
					// override with dynamic config?
					var config = ColumnsConfig?.Find(x => x.Id == column.Id);
					if(config?.Editable ?? editable)
					{
						key = column.Id;
						break;
					}
				}
				if (key != string.Empty)
				{
					await JSRuntime.InvokeVoidAsync("selectText", $"{IdEditPrefix}{key}", _tableBeforeEditArgs!.SelectionStart, _tableBeforeEditArgs!.SelectionEnd).ConfigureAwait(true);
					BeginEditEvent.Reset();
				}
			}
		}

		private async Task OnEndEdit()
		{
			if (IsEditing)
			{
				// if focus has moved to another editor then continue editing
				var id = await JSRuntime.InvokeAsync<string>("getFocusedElementId").ConfigureAwait(true);
				if (!id.StartsWith(IdEditPrefix))
				{
					await CommitEdit().ConfigureAwait(true);
				}
			}
		}

		private async Task OnRowMouseDown(MouseEventArgs args, TItem item)
		{
			var key = KeyField!(item)?.ToString();
			if (key != null && SelectionMode != TableSelectionMode.None)
			{
				var alreadySelected = Selection.Contains(key);

				// begin edit mode?
				if(AllowEdit && !IsEditing && Selection.Count == 1 && alreadySelected && !args.CtrlKey && args.Button == 0)
				{
					//_editTimer.Start();
					_editTimer?.Change(500, Timeout.Infinite);
					//await BeginEdit().ConfigureAwait(true);
				}
				else
				{
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
						if (args.CtrlKey)
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
						else if (!alreadySelected)
						{
							Selection.Clear();
							Selection.Add(key);
							await SelectionChanged.InvokeAsync(null).ConfigureAwait(true);
						}
					}
				}
			}
		}

		private void OnRowClick(MouseEventArgs _, TItem item)
		{
			Click.InvokeAsync(item);
		}

		private void OnRowDoubleClick(MouseEventArgs _, TItem item)
		{
			// cancel pending edit mode
			_editTimer?.Change(Timeout.Infinite, Timeout.Infinite);
			DoubleClick.InvokeAsync(item);
		}

		private async Task OnKeyDown(KeyboardEventArgs args)
		{
			if(IsEditing)
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
			}
			else
			{
				switch (args.Code)
				{
					case "F2":
						await BeginEdit().ConfigureAwait(true);
						break;

					case "ArrowUp":
					case "ArrowDown":
						if(Selection.Count == 1 && KeyField != null)
						{
							var items = ItemsToDisplay.ToList();
							var item = items.Find(x => KeyField(x).ToString() == Selection[0]);
							if(item != null)
							{
								var idx = items.IndexOf(item);
								if(args.Code == "ArrowUp" && idx > 0)
								{
									Selection.Clear();
									Selection.Add(KeyField(items[idx - 1]).ToString());
								}
								else if (args.Code == "ArrowDown" && idx < items.Count - 1)
								{
									Selection.Clear();
									Selection.Add(KeyField(items[idx + 1]).ToString());
								}
							}
						}
						break;
				}
			}
		}

		private void OnEditTimer(object state)
		{
			if (!_dragging)
			{
				Task.Run(async () => await BeginEdit().ConfigureAwait(true));
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

		private bool IsColumnInEditMode(PDColumn<TItem> column, TItem item)
		{
			// is editing current row?
			if(IsEditing && item == EditItem)
			{
				var editable = column.Editable;
				// override with dynamic config?
				var config = ColumnsConfig?.Find(x => x.Id == column.Id);
				return config?.Editable ?? editable;
			}
			return false;
		}

		void IDisposable.Dispose()
		{
			_editTimer?.Dispose();
			_editTimer = null;
		}

		private void OnDragStart(DragEventArgs _)
		{
			_dragging = true;

			// need to set the data being dragged
			if (DragContext != null && KeyField != null)
			{
				// get all selected items
				var items = new List<TItem>();
				foreach(var key in Selection)
				{
					var item = ItemsToDisplay.Find(x => KeyField(x).ToString() == key);
					if(item != null)
					{
						items.Add(item);
					}
				}
				DragContext.Payload = items;
			}
		}

		private void OnDragEnd(DragEventArgs _)
		{
			_dragging = false;
		}

		private void OnDragDrop(TItem row)
		{
			if(DragContext != null)
			{
				Drop.InvokeAsync(new DropEventArgs(row, DragContext.Payload));
			}
		}
	}
}
