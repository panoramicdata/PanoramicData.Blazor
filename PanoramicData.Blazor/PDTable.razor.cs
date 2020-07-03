﻿using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Collections.Generic;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.WebUtilities;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using PanoramicData.Blazor.Services;
using PanoramicData.Blazor.Exceptions;
using PanoramicData.Blazor.Extensions;

namespace PanoramicData.Blazor
{
	public partial class PDTable<TItem> : ISortableComponent
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
		/// Gets or sets the CSS class to apply to the tables container element.
		/// </summary>
		[Parameter] public string TableClass { get; set; } = string.Empty;

		/// <summary>
		/// Gets or sets the CSS class to apply to the table header element.
		/// </summary>
		[Parameter] public string THeadClass { get; set; } = string.Empty;

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
		[Parameter] public EventCallback<SortCriteria> OnSortChanged { get; set; }

		/// <summary>
		/// When set, turns on paging and specifies the number of displayed items per page.
		/// </summary>
		[Parameter] public int? PageSize { get; set; }

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
		/// Gets a full list of all columns.
		/// </summary>
		public List<PDColumn<TItem>> Columns { get; } = new List<PDColumn<TItem>>();

		/// <summary>
		/// When given provides a sub-set of columns to display, along with the ability
		/// to override column defaults. When not given, all columns will be displayed.
		/// </summary>

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
		protected IEnumerable<TItem> ItemsToDisplay { get; private set; } = Enumerable.Empty<TItem>();

		/// <summary>
		/// Has the table been initialized?
		/// </summary>
		protected bool TableInitialised { get; set; } = false;

		/// <summary>
		/// Current page number, when paging enabled.
		/// </summary>
		protected int CurrentPage { get; set; } = 1;

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

		protected override async Task OnAfterRenderAsync(bool firstRender)
		{
			// If this is the first time we've finished rendering, then all the columns
			// have been added to the table so we'll go and get the data for the first time
			if (firstRender)
			{
				try
				{
					if (DefaultSort != null)
					{
						var columnToSortBy = Columns.SingleOrDefault(c => c.Id == DefaultSort.Key || c.Title == DefaultSort.Key);
						if (columnToSortBy != null)
						{
							await columnToSortBy.SortByAsync(DefaultSort.Direction).ConfigureAwait(true);
						}
					}

					// Get the requested table parameters from the QueryString
					var uri = new Uri(NavigationManager.Uri);
					var query = QueryHelpers.ParseQuery(uri.Query);

					// Paging
					if (query.TryGetValue("page", out var requestedPage) && query.TryGetValue("pageSize", out var requestedPageSize))
					{
						PageSize = Convert.ToInt32(requestedPageSize[0]);
						CurrentPage = Convert.ToInt32(requestedPage[0]);
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
				if(PageSize.HasValue)
				{
					request.Take = PageSize.Value;
					request.Skip = (CurrentPage - 1) * PageSize.Value;
				}

				// perform query data
				var response = await DataProvider
					.GetDataAsync(request, CancellationToken.None)
					.ConfigureAwait(true);

				if(PageSize.HasValue)
				{
					PageCount = (response.TotalCount / PageSize.Value) + (response.TotalCount % PageSize.Value > 0 ? 1 : 0);
				}

				ItemsToDisplay = response.Items;
			}
			finally
			{
			}
		}

		/// <summary>
		/// Refresh the grid by performing a re-query.
		/// </summary>
		public Task RefreshAsync()
			=> GetDataAsync();

		/// <summary>
		/// Sort the displayed items.
		/// </summary>
		/// <param name="sortCriteria">Details of the sort operation to be performed.</param>
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
				await OnSortChanged.InvokeAsync(new SortCriteria { Key = column.Id, Direction = direction ?? column.SortDirection }).ConfigureAwait(true);
			}
		}

		private async void PageChangeHandler(int newPage)
		{
			CurrentPage = newPage;
			// Update the URI for bookmarking
			NavigationManager.SetUri(new Dictionary<string, object> { { "pageSize", $"{PageSize}" }, { "page", $"{CurrentPage}" } });
			await GetDataAsync().ConfigureAwait(true);
			StateHasChanged();
		}
	}
}
