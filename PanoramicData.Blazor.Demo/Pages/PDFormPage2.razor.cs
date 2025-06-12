namespace PanoramicData.Blazor.Demo.Pages;

public partial class PDFormPage2
{
	private readonly PersonDataProvider _personDataProvider = new();

	private PDModal Modal { get; set; } = null!;
	private PDForm<Person> Form { get; set; } = null!;
	private List<Person> People { get; set; } = [];
	private Person? SelectedPerson { get; set; }

	[Inject] public IJSRuntime? JSRuntime { get; set; }

	[CascadingParameter] protected EventManager? EventManager { get; set; }

	public PDFormPage2()
	{
		RefreshPeople();
	}

	private async Task OnFooterClick(string key)
	{
		EventManager?.Add(new Event("FooterClick", new EventArgument("Key", key)));

		if (key == "Cancel")
		{
			SelectedPerson = null;
			await Modal.HideAsync().ConfigureAwait(true);
		}
	}

	private async Task OnPersonCreatedAsync(Person person)
	{
		EventManager?.Add(new Event("PersonCreated", new EventArgument("Forename", person.FirstName), new EventArgument("Surname", person.LastName)));
		await Modal.HideAsync().ConfigureAwait(true);
		RefreshPeople();
	}

	private async Task OnPersonUpdatedAsync(Person person)
	{
		EventManager?.Add(new Event("PersonUpdated", new EventArgument("Forename", person.FirstName), new EventArgument("Surname", person.LastName)));
		await Modal.HideAsync().ConfigureAwait(true);
		RefreshPeople();
	}

	private async Task OnPersonDeletedAsync(Person person)
	{
		EventManager?.Add(new Event("PersonDeleted", new EventArgument("Forename", person.FirstName), new EventArgument("Surname", person.LastName)));
		await Modal.HideAsync().ConfigureAwait(true);
		RefreshPeople();
	}

	private void OnError(string message) => EventManager?.Add(new Event("Error", new EventArgument("Message", message)));

	private void RefreshPeople() => _personDataProvider
			.GetDataAsync(new DataRequest<Person>
			{
				Take = 5,
				SortFieldExpression = (x) => x.DateCreated,
				SortDirection = SortDirection.Descending
			}, CancellationToken.None)
			.ContinueWith(PopulatePeopleResult);

	private void PopulatePeopleResult(Task<DataResponse<Person>> resultTask)
	{
		if (!resultTask.IsFaulted)
		{
			SelectedPerson = null;
			People.Clear();
			People.AddRange(resultTask.Result.Items);
			InvokeAsync(() => StateHasChanged());
		}
	}

	private async Task OnEditPersonAsync(Person person)
	{
		SelectedPerson = person;
		await Form.EditItemAsync(SelectedPerson, FormModes.Edit).ConfigureAwait(true);
		await Modal.ShowAsync().ConfigureAwait(true);
	}

	private async Task OnCreatePersonAsync()
	{
		SelectedPerson = new Person();
		await Form.EditItemAsync(SelectedPerson, FormModes.Create).ConfigureAwait(true);
		await Modal.ShowAsync().ConfigureAwait(true);
	}
}
