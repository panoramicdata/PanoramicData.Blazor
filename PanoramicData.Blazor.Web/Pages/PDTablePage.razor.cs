using System;
using System.Collections.Generic;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.WebUtilities;
using Newtonsoft.Json;
using PanoramicData.Blazor.Web.Data;
using PanoramicData.Blazor.Extensions;

namespace PanoramicData.Blazor.Web.Pages
{
	public partial class PDTablePage
	{
		private int _pageSize = 5;
		private string _searchText = string.Empty;
		private string _events = string.Empty;
		private PDTable<TestRow>? _table;
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
		}

		private void SortChangeHandler(SortCriteria criteria)
		{
			_events += $"sort changed: {criteria.Key}, {criteria.Direction}{Environment.NewLine}";
			// Update the URI for bookmarking
			var direction = criteria.Direction == SortDirection.Ascending ? "asc" : "desc";
			NavigationManager.SetUri(new Dictionary<string, object> { { "sort", $"{criteria.Key}|{direction}" } });
		}
	}
}
