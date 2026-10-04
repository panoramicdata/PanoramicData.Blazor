namespace PanoramicData.Blazor.Demo.Pages;

public partial class PDFormPage3
{
	private readonly PersonDataProvider _personDataProvider = new();
	private readonly PageCriteria _pageCriteria = new(1, 10);
	private readonly SortCriteria _sortCriteria = new("DateCreatedCol", SortDirection.Descending);

	private PDForm<Person> Form { get; set; } = null!;
	private PDFormBody<Person> FormBody { get; set; } = null!;
	private PDTable<Person> Table { get; set; } = null!;
	private Person? SelectedPerson { get; set; }

	[CascadingParameter] protected EventManager? EventManager { get; set; }

	private static string GetIdDescription(PDForm<Person> form) => form.Item is Person item
		? $"{item.Id} ({(item.Id % 2 == 0 ? "even" : "odd")})"
		: string.Empty;

	private async Task OnPersonChanged(string eventName, Person person)
	{
		EventManager?.Add(new Event(eventName, new EventArgument("Forename", person.FirstName), new EventArgument("Surname", person.LastName)));
		await Table.RefreshAsync().ConfigureAwait(true);
	}

	private void OnError(string message) => EventManager?.Add(new Event("Error", new EventArgument("Message", message)));

	private async Task OnFooterClick(string key)
	{
		EventManager?.Add(new Event("FooterClick", new EventArgument("Key", key)));

		if (key == "Cancel")
		{
			SelectedPerson = null;
			await Table.ClearSelectionAsync().ConfigureAwait(true);
			await Form.EditItemAsync(null, FormModes.Empty).ConfigureAwait(true);
		}
	}

	private async Task OnCreatePerson()
	{
		SelectedPerson = new Person();
		await Form.EditItemAsync(SelectedPerson, FormModes.Create).ConfigureAwait(true);
	}

	private async Task OnSelectionChanged()
	{
		if (Table.Selection.Count == 0)
		{
			return;
		}

		var id = int.Parse(Table.Selection[0]);
		SelectedPerson = Table.ItemsToDisplay.Find(x => x.Id == id);
		if (SelectedPerson != null)
		{
			await Form.EditItemAsync(SelectedPerson, FormModes.Edit).ConfigureAwait(true);
		}
	}

	private static OptionInfo[] GetLocationOptions(Person item) => [.. PersonDataProvider.Locations
		.Select((location, index) => new OptionInfo
		{
			Text = location,
			Value = index,
			IsSelected = item?.Location == index,
			IsDisabled = location == "Sydney"
		})];
}
