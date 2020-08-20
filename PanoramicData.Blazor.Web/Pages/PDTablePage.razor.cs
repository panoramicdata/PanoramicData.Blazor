using System;
using System.Linq;
using System.Threading.Tasks;
using System.Collections.Generic;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.WebUtilities;
using Microsoft.AspNetCore.Components.Web;
using Newtonsoft.Json;
using PanoramicData.Blazor.Web.Data;
using PanoramicData.Blazor.Extensions;
using Microsoft.CodeAnalysis.Host;

namespace PanoramicData.Blazor.Web.Pages
{
	public partial class PDTablePage
	{
		private string _events = string.Empty;
		private string _searchText = string.Empty;
		private bool _allowDrag = false;
		private bool _allowDrop = false;
		private string _dropZoneCss = "";
		private string _dropMessage = "Drop Zone";
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
				new PDColumnConfig { Id = "Col3", Editable = false },
				new PDColumnConfig { Id = "Col4" },
				new PDColumnConfig { Id = "Col5" }
			};

		[CascadingParameter] PDDragContext? DragContext { get; set; }

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

		private TableSelectionMode SelectionMode { get; set; } = TableSelectionMode.Single;

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

		private void OnSortChange(SortCriteria criteria)
		{
			_events += $"sort changed: key = {criteria.Key}, dir = {criteria.Direction}{Environment.NewLine}";
			// Update the URI for bookmarking
			var direction = criteria.Direction == SortDirection.Ascending ? "asc" : "desc";
			NavigationManager.SetUri(new Dictionary<string, object> { { "sort", $"{criteria.Key}|{direction}" } });
		}

		private void OnPageChange(PageCriteria criteria)
		{
			_events += $"page changed: page = {criteria.Page}, page size = {criteria.PageSize}{Environment.NewLine}";
			// Update the URI for bookmarking
			NavigationManager.SetUri(new Dictionary<string, object> { { "page", $"{criteria.Page}" }, { "pageSize", $"{criteria.PageSize}" } });
		}

		private void OnSelectionChange()
		{
			var keys = _table == null ? "" : string.Join(", ", _table.Selection.ToArray());
			_events += $"selection changed: {keys}{Environment.NewLine}";
		}

		private void OnClick(TestRow item)
		{
			_events += $"click: {item.IntField}{Environment.NewLine}";
		}

		private void OnDoubleClick(TestRow item)
		{
			_events += $"double-click: {item.IntField}{Environment.NewLine}";
		}

		private void OnBeforeEdit(TableBeforeEditEventArgs<TestRow> args)
		{
			_events += $"before edit: {args.Item.IntField}{Environment.NewLine}";
			// example of preventing an edit
			args.Cancel = args.Item.BooleanField;
		}

		private void OnDrop(DropEventArgs args)
		{
			if (args?.Payload != null && args.Target is TestRow row && args.Payload is IEnumerable<TestRow> rows)
			{
				_events += $"drop: {string.Join(", ", rows.Select(x => x.NameField))} onto {row.NameField}{Environment.NewLine}";
			}
		}

		private void OnDragEnter(DragEventArgs args)
		{
			if(DragContext?.Payload == null)
			{
				_dropZoneCss = "bad";
			}
			else
			{
				_dropZoneCss = "good";
			}
		}

		private void OnDragLeave(DragEventArgs args)
		{
			_dropZoneCss = "";
		}

		private void OnDragDrop(DragEventArgs args)
		{
			// get item that was dragged (TestRow)
			_dropMessage = "Boom!";
			if(DragContext?.Payload != null)
			{
				var items = (List<TestRow>)DragContext.Payload;
				_dropMessage = string.Join(", ", items.Select(x => x.NameField));
			}
		}


	}
}
