using System;
using System.Collections.Generic;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.WebUtilities;
using Newtonsoft.Json;
using PanoramicData.Blazor.Web.Data;
using PanoramicData.Blazor.Extensions;
using System.Threading.Tasks;

namespace PanoramicData.Blazor.Web.Pages
{
	public partial class PDTablePage
	{
		private string _events = string.Empty;
		private string _searchText = string.Empty;
		private PDTable<TestRow>? _table;
		private PageCriteria _defaultPage = new PageCriteria(1, 5);
		private SortCriteria _defaultSort = new SortCriteria("Col1", SortDirection.Descending);

		/// <summary>
		/// Injected navigation manager.
		/// </summary>
		[Inject] private NavigationManager NavigationManager { get; set; } = null!;

		// columns config enables config to be defined per user or customer etc.
		private List<PDColumnConfig>? _columnsConfig = new List<PDColumnConfig>
			{
				new PDColumnConfig { Id = "Col1" },
				new PDColumnConfig { Id = "Col2", Title = "Date Started" },
				new PDColumnConfig { Id = "Col3" },
				new PDColumnConfig { Id = "Col4" },
				new PDColumnConfig { Id = "Col5" }
			};

		private string ColumnsConfigJson
		{
			get
			{
				return JsonConvert.SerializeObject(_columnsConfig, Formatting.Indented);
			}
			set
			{
				_columnsConfig = JsonConvert.DeserializeObject<List<PDColumnConfig>>(value);
			}
		}

		// dummy data provider
		public TestDataProvider DataProvider { get; }  = new TestDataProvider();

		protected override void OnInitialized()
		{
			// Get the requested table parameters from the QueryString
			var uri = new Uri(NavigationManager.Uri);
			var query = QueryHelpers.ParseQuery(uri.Query);

			// Sort
			if (query.TryGetValue("sort", out var requestedSortFields))
			{
				var sortFieldSpecs = requestedSortFields[0].Split('|');
				if (sortFieldSpecs.Length == 2)
				{
					_defaultSort = new SortCriteria(sortFieldSpecs[0], sortFieldSpecs[1] == "desc" ? SortDirection.Descending : SortDirection.Ascending);
				}
			}

			// Page
			if (query.TryGetValue("page", out var requestedPage) && query.TryGetValue("pageSize", out var requestedPageSize))
			{
				_defaultPage = new PageCriteria(Convert.ToInt32(requestedPage[0]), Convert.ToInt32(requestedPageSize[0]));
			}

			// Search
			if (query.TryGetValue("search", out var requestedSearch))
			{
				_searchText = requestedSearch;
			}
		}

		private async Task SearchAsync()
		{
			// Update the URI for bookmarking
			NavigationManager.SetUri(new Dictionary<string, object> { { "search", $"{_searchText}" } });
			await _table!.RefreshAsync().ConfigureAwait(true);
		}

		private void SortChangeHandler(SortCriteria criteria)
		{
			_events += $"sort changed: key = {criteria.Key}, dir = {criteria.Direction}{Environment.NewLine}";
			// Update the URI for bookmarking
			var direction = criteria.Direction == SortDirection.Ascending ? "asc" : "desc";
			NavigationManager.SetUri(new Dictionary<string, object> { { "sort", $"{criteria.Key}|{direction}" } });
		}

		private void PageChangeHandler(PageCriteria criteria)
		{
			_events += $"page changed: page = {criteria.Page}, page size = {criteria.PageSize}{Environment.NewLine}";
			// Update the URI for bookmarking
			NavigationManager.SetUri(new Dictionary<string, object> { { "page", $"{criteria.Page}" }, { "pageSize", $"{criteria.PageSize}" } });
		}
	}
}
