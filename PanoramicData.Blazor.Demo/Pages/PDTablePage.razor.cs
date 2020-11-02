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
		private string _searchText = string.Empty;
		private PageCriteria _defaultPage = new PageCriteria(1, 5);
		private SortCriteria _defaultSort = new SortCriteria("Col1", SortDirection.Descending);
		private readonly PersonDataProvider PersonDataProvider = new PersonDataProvider(53);
		private bool AllowDrag { get; set; }
		private bool AllowDrop { get; set; }
		private string DropZoneCss { get; set; } = "";
		private PDDragContext? DragContext { get; set; }
		private string DropMessage { get; set; } = "Drop Zone";
		private PDTable<Person>? Table { get; set; }

		[Inject] private NavigationManager NavigationManager { get; set; } = null!;

		[CascadingParameter] protected EventManager? EventManager { get; set; }

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
			await Table!.RefreshAsync().ConfigureAwait(true);
		}

		private void OnSortChange(SortCriteria criteria)
		{
			EventManager?.Add(new Event("SortChange", new EventArgument("Key", criteria.Key), new EventArgument("Direction", criteria.Direction)));

			// Update the URI for bookmarking
			var direction = criteria.Direction == SortDirection.Ascending ? "asc" : "desc";
			NavigationManager.SetUri(new Dictionary<string, object> { { "sort", $"{criteria.Key}|{direction}" } });
		}

		private void OnPageChange(PageCriteria criteria)
		{
			EventManager?.Add(new Event("PageChange", new EventArgument("Page", criteria.Page), new EventArgument("PageSize", criteria.PageSize)));

			// Update the URI for bookmarking
			NavigationManager.SetUri(new Dictionary<string, object> { { "page", $"{criteria.Page}" }, { "pageSize", $"{criteria.PageSize}" } });
		}

		private void OnSelectionChange()
		{
			EventManager?.Add(new Event("SelectionChange"));
		}

		private void OnClick(Person item)
		{
			EventManager?.Add(new Event ("Click", new EventArgument("Person", item.FirstName)));
		}

		private void OnDoubleClick(Person item)
		{
			EventManager?.Add(new Event("DoubleClick", new EventArgument("Person", item.FirstName)));
		}

		private void OnBeforeEdit(TableBeforeEditEventArgs<Person> args)
		{
			EventManager?.Add(new Event("BeforeEdit", new EventArgument("Person", args.Item.FirstName)));

			// example of preventing an edit
			args.Cancel = args.Item.FirstName == "Alice";
		}

		private void OnDrop(DropEventArgs args)
		{
			if (args?.Payload != null && args.Target is Person row && args.Payload is IEnumerable<Person> rows)
			{
				EventManager?.Add(new Event("Drop",
					new EventArgument("Target", row.FirstName),
					new EventArgument("Payload", string.Join(", ", rows.Select(x => x.FirstName)))
				));
			}
		}

		private void OnDragEnter(DragEventArgs args)
		{
			if(DragContext?.Payload == null)
			{
				DropZoneCss = "bad";
			}
			else
			{
				DropZoneCss = "good";
			}
		}

		private void OnDragLeave(DragEventArgs args)
		{
			DropZoneCss = "";
		}

		private void OnDragDrop(DragEventArgs args)
		{
			// get item that was dragged (TestRow)
			DropMessage = "Boom!";
			if(DragContext?.Payload != null)
			{
				var items = (List<Person>)DragContext.Payload;
				DropMessage = string.Join(", ", items.Select(x => x.FirstName));
			}
		}

		private async Task OnSearchKeyPress(KeyboardEventArgs args)
		{
			if (args.Code == "Enter")
			{
				await SearchAsync().ConfigureAwait(true);
			}
		}

		private async Task OnSearchChanged(string text)
		{
			_searchText = text;
			if (text == string.Empty)
			{
				StateHasChanged();

				await SearchAsync().ConfigureAwait(true);
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