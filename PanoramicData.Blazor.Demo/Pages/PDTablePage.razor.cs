﻿namespace PanoramicData.Blazor.Demo.Pages;

public partial class PDTablePage
{
	private string _searchText = string.Empty;
	private PageCriteria _pageCriteria = new(1, 100);
	private SortCriteria _sortCriteria = new("Last Name", SortDirection.Descending);
	private readonly PersonDataProvider _personDataProvider = new();
	private object[] _ages = [];
	private bool AllowDrag { get; set; }
	private bool AllowDrop { get; set; }
	private string DropZoneCss { get; set; } = "";
	private PDDragContext? DragContext { get; set; }
	private string DropMessage { get; set; } = "Drop Zone";
	private bool Enabled { get; set; } = true;
	private PDTable<Person> Table { get; set; } = null!;

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
			var sortFieldSpecs = requestedSortFields[0]?.Split('|') ?? [];
			if (sortFieldSpecs.Length == 2)
			{
				_sortCriteria = new SortCriteria(sortFieldSpecs[0], sortFieldSpecs[1] == "desc" ? SortDirection.Descending : SortDirection.Ascending);
			}
		}

		// Page
		if (query.TryGetValue("page", out var requestedPage) && query.TryGetValue("pageSize", out var requestedPageSize))
		{
			_pageCriteria = new PageCriteria(Convert.ToUInt32(requestedPage[0]), Convert.ToUInt32(requestedPageSize[0]));
		}

		// Search
		if (query?.TryGetValue("search", out var requestedSearch) ?? false)
		{
			_searchText = requestedSearch.ToString();
		}
	}

	protected override async Task OnInitializedAsync()
	{
		// determine unique birth years
		var years = await _personDataProvider.GetDistinctValuesAsync(new(), x => x.Dob!.Value.Year);
		_ages = [.. years.Select(x => (DateTime.Now.Date.Year - Convert.ToInt32(x)) as object).OrderBy(x => x)];
	}

	private async Task SearchAsync()
	{
		// Update the URI for bookmarking
		NavigationManager.SetUri(new Dictionary<string, object> { { "search", $"{_searchText}" } });
		await Table!.RefreshAsync(_searchText).ConfigureAwait(true);
	}

	private void OnSortChange(SortCriteria criteria)
	{
		EventManager?.Add(new Event("SortChange", new EventArgument("Key", criteria.Key), new EventArgument("Direction", criteria.Direction)));

		// Update the URI for bookmarking
		var direction = criteria.Direction == SortDirection.Ascending ? "asc" : "desc";
		NavigationManager.SetUri(new Dictionary<string, object> { { "sort", $"{criteria.Key}|{direction}" } });
	}

	private void OnAfterEdit(TableAfterEditEventArgs<Person> args)
	{
		EventManager?.Add(new Event("AfterEdit", new EventArgument("Person", args.Item.FirstName), new EventArgument("Cancel", args.Cancel), new EventArgument("NewValues", args.ToString())));
	}


	private void OnAfterEditCommitted(TableAfterEditCommittedEventArgs<Person> args)
	{
		EventManager?.Add(new Event("AfterEditCommitted", new EventArgument("Person", args.Item.FirstName), new EventArgument("NewValues", args.ToString())));
	}

	private void OnBeforeEdit(TableBeforeEditEventArgs<Person> args)
	{
		EventManager?.Add(new Event("BeforeEdit", new EventArgument("Person", args.Item.FirstName), new EventArgument("SelectionStart", args.SelectionStart), new EventArgument("SelectionEnd", args.SelectionEnd)));

		// example of preventing an edit
		args.Cancel = args.Item.FirstName == "Alice";
	}

	private void OnPageChange(PageCriteria criteria)
	{
		EventManager?.Add(new Event("PageChange", new EventArgument("Page", criteria.Page), new EventArgument("PageSize", criteria.PageSize)));

		// Update the URI for bookmarking
		NavigationManager.SetUri(new Dictionary<string, object> { { "page", $"{criteria.Page}" }, { "pageSize", $"{criteria.PageSize}" } });
	}

	private void OnSelectionChange()
	{
		var selection = Table?.GetSelectedItems();
		if (selection != null)
		{
			EventManager?.Add(new Event("SelectionChange", new EventArgument("Selection", string.Join(", ", selection.Select(x => x.FirstName)))));
		}
	}

	private void OnClick(Person item) => EventManager?.Add(new Event("Click", new EventArgument("Person", item.FirstName)));

	private void OnDoubleClick(Person item) => EventManager?.Add(new Event("DoubleClick", new EventArgument("Person", item.FirstName)));

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

	private void OnDragEnter(DragEventArgs _)
	{
		if (DragContext?.Payload == null)
		{
			DropZoneCss = "bad";
		}
		else
		{
			DropZoneCss = "good";
		}
	}

	private void OnDragLeave(DragEventArgs _) => DropZoneCss = "";

	private void OnDragDrop(DragEventArgs _)
	{
		// get item that was dragged (TestRow)
		DropMessage = "Boom!";
		if (DragContext?.Payload != null)
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

	private async Task OnSearchValueChanged(string value)
	{
		_searchText = value;
		await SearchAsync().ConfigureAwait(true);
	}

	private async Task OnSearchCleared() =>
		//_searchText = string.Empty;
		await SearchAsync().ConfigureAwait(true);

	private static OptionInfo[] GetLocationOptions(FormField<Person> _, Person item)
	{
		var options = new List<OptionInfo>();
		for (var i = 0; i < PersonDataProvider.Locations.Length; i++)
		{
			options.Add(new OptionInfo { Text = PersonDataProvider.Locations[i], Value = i, IsSelected = item?.Location == i });
		}

		return [.. options];
	}

	private async Task OnEditCommand()
	{
		if (Table!.IsEditing)
		{
			await Table.CommitEditAsync().ConfigureAwait(true);
		}
		else
		{
			await Table.BeginEditAsync().ConfigureAwait(true);
		}
	}

	private static object[] GetFormattedDobOptions()
	{
		var dobOptions = new List<object>();
		foreach (var person in PersonDataProvider.GetAllPersons())
		{
			if (person.Dob.HasValue)
			{
				dobOptions.Add(person.Dob.Value.ToString("MM/dd/yyyy"));
			}
		}

		return [.. dobOptions.Distinct().Order()];
	}
}