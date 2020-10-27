using System;
using System.Linq;
using System.Threading.Tasks;
using System.Collections.Generic;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.WebUtilities;
using Microsoft.AspNetCore.Components.Web;
using PanoramicData.Blazor.Demo.Data;
using PanoramicData.Blazor.Extensions;

namespace PanoramicData.Blazor.Demo.Pages
{
	public partial class PDTablePage
	{
		private string _events = string.Empty;
		private string _searchText = string.Empty;
		private bool _allowDrag = false;
		private bool _allowDrop = false;
		private string _dropZoneCss = "";
		private string _dropMessage = "Drop Zone";
		private PDTable<Person>? _table;
		private PDDragContext? _dragContext;
		private PageCriteria _defaultPage = new PageCriteria(1, 5);
		private SortCriteria _defaultSort = new SortCriteria("Col1", SortDirection.Descending);
		private readonly PersonDataProvider PersonDataProvider = new PersonDataProvider(53);

		/// <summary>
		/// Injected navigation manager.
		/// </summary>
		[Inject] private NavigationManager NavigationManager { get; set; } = null!;

		// columns config enables config to be defined per user or customer etc.
		//private List<PDColumnConfig>? _columnsConfig = new List<PDColumnConfig>
		//	{
		//	    new PDColumnConfig { Id = "col-id", Title = "Forename" },
		//		new PDColumnConfig { Id = "col-first-name", Title = "Forename" },
		//		new PDColumnConfig { Id = "col-last-name", Title = "Surname", Editable = false },
		//		new PDColumnConfig { Id = "col-first-name", Title = "Forename" },
		//	};

		//private string ColumnsConfigJson
		//{
		//	get
		//	{
		//		return JsonConvert.SerializeObject(_columnsConfig, Formatting.Indented);
		//	}
		//	set
		//	{
		//		_columnsConfig = JsonConvert.DeserializeObject<List<PDColumnConfig>>(value);
		//	}
		//}

		private TableSelectionMode SelectionMode { get; set; } = TableSelectionMode.Single;

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

		private void OnClick(Person item)
		{
			_events += $"click: {item.Id}{Environment.NewLine}";
		}

		private void OnDoubleClick(Person item)
		{
			_events += $"double-click: {item.Id}{Environment.NewLine}";
		}

		private void OnBeforeEdit(TableBeforeEditEventArgs<Person> args)
		{
			_events += $"before edit: {args.Item.Id}{Environment.NewLine}";
			// example of preventing an edit
			args.Cancel = args.Item.FirstName == "Alice";
		}

		private void OnDrop(DropEventArgs args)
		{
			if (args?.Payload != null && args.Target is Person row && args.Payload is IEnumerable<Person> rows)
			{
				_events += $"drop: {string.Join(", ", rows.Select(x => x.FirstName))} onto {row.FirstName}{Environment.NewLine}";
			}
		}

		private void OnDragEnter(DragEventArgs args)
		{
			if(_dragContext?.Payload == null)
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
			if(_dragContext?.Payload != null)
			{
				var items = (List<Person>)_dragContext.Payload;
				_dropMessage = string.Join(", ", items.Select(x => x.FirstName));
			}
		}

		private OptionInfo[] GetLocationOptions(FormField<Person> field, Person item)
		{
			var options = new List<OptionInfo>();
			for (var i = 0; i < PersonDataProvider.Locations.Length; i++)
			{
				options.Add(new OptionInfo { Text = PersonDataProvider.Locations[i], Value = i, IsSelected = item?.Location == i });
			}
			return options.ToArray();
		}
	}
}
