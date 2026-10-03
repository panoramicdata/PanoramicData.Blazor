namespace PanoramicData.Blazor.Demo.Pages;

public partial class PDFormPage2
{
	private readonly PersonDataProvider _personDataProvider = new();

	// Per-example form + modal refs
	protected PDModal Modal1 { get; set; } = null!;
	protected PDModal Modal2 { get; set; } = null!;
	protected PDModal Modal3 { get; set; } = null!;
	protected PDModal Modal4 { get; set; } = null!;
	protected PDModal Modal5 { get; set; } = null!;
	protected PDModal Modal6 { get; set; } = null!;

	protected PDForm<Person> Form1 { get; set; } = null!;
	protected PDForm<Person> Form2 { get; set; } = null!;
	protected PDForm<Person> Form3 { get; set; } = null!;
	protected PDForm<Person> Form4 { get; set; } = null!;
	protected PDForm<Person> Form5 { get; set; } = null!;
	protected PDForm<Person> Form6 { get; set; } = null!;

	// Per-example selected person (index = example number - 1)
	private readonly Person?[] _selected = new Person?[6];

	private List<Person> People { get; set; } = [];

	[CascadingParameter] protected EventManager? EventManager { get; set; }

	public PDFormPage2()
	{
		RefreshPeople();
	}

	// ── Opens an example's form in its modal ──
	private async Task ShowExampleAsync(int example, PDForm<Person> form, PDModal modal, Person person, FormModes mode, bool? validate)
	{
		_selected[example - 1] = person;
		await form.EditItemAsync(person, mode, validate).ConfigureAwait(true);
		await modal.ShowAsync().ConfigureAwait(true);
	}

	// ── Shared handlers ──
	private async Task OnFooterClickAsync(string key, PDModal modal)
	{
		EventManager?.Add(new Event("FooterClick", new EventArgument("Key", key)));
		if (key == "Cancel")
		{
			await modal.HideAsync().ConfigureAwait(true);
		}
	}

	private async Task OnPersonSavedAsync(Person person, PDModal modal)
	{
		EventManager?.Add(new Event("PersonSaved", new EventArgument("Forename", person.FirstName), new EventArgument("Surname", person.LastName)));
		await modal.HideAsync().ConfigureAwait(true);
		RefreshPeople();
	}

	private async Task OnPersonDeletedAsync(Person person)
	{
		EventManager?.Add(new Event("PersonDeleted", new EventArgument("Forename", person.FirstName), new EventArgument("Surname", person.LastName)));
		await Modal3.HideAsync().ConfigureAwait(true);
		RefreshPeople();
	}

	private void OnError(string message) =>
		EventManager?.Add(new Event("Error", new EventArgument("Message", message)));

	private void RefreshPeople() => _personDataProvider
		.GetDataAsync(new DataRequest<Person>
		{
			Take = 5,
			SortFieldExpression = x => x.DateCreated,
			SortDirection = SortDirection.Descending
		}, CancellationToken.None)
		.ContinueWith(PopulatePeopleResult);

	private void PopulatePeopleResult(Task<DataResponse<Person>> resultTask)
	{
		if (!resultTask.IsFaulted)
		{
			People.Clear();
			People.AddRange(resultTask.Result.Items);
			InvokeAsync(() => StateHasChanged());
		}
	}
}
