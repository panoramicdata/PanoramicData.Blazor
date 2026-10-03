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

	// Per-example selected person
	private Person? _selected1;
	private Person? _selected2;
	private Person? _selected3;
	private Person? _selected4;
	private Person? _selected5;
	private Person? _selected6;

	private List<Person> People { get; set; } = [];

	[CascadingParameter] protected EventManager? EventManager { get; set; }

	public PDFormPage2()
	{
		RefreshPeople();
	}

	// ── Example 1: Standard Edit / Create ──
	private async Task OnExample1EditAsync(Person person)
	{
		_selected1 = person;
		await Form1.EditItemAsync(_selected1, FormModes.Edit).ConfigureAwait(true);
		await Modal1.ShowAsync().ConfigureAwait(true);
	}

	private async Task OnExample1CreateAsync()
	{
		_selected1 = new Person();
		await Form1.EditItemAsync(_selected1, FormModes.Create).ConfigureAwait(true);
		await Modal1.ShowAsync().ConfigureAwait(true);
	}

	// ── Example 2: ReadOnly ──
	private async Task OnExample2ViewAsync(Person person)
	{
		_selected2 = person;
		await Form2.EditItemAsync(_selected2, FormModes.ReadOnly).ConfigureAwait(true);
		await Modal2.ShowAsync().ConfigureAwait(true);
	}

	// ── Example 3: Edit + Delete ──
	private async Task OnExample3DeleteAsync(Person person)
	{
		_selected3 = person;
		await Form3.EditItemAsync(_selected3, FormModes.Edit).ConfigureAwait(true);
		await Modal3.ShowAsync().ConfigureAwait(true);
	}

	// ── Example 4: Custom button text (Approve / Reject) ──
	private async Task OnExample4ApproveAsync(Person person)
	{
		_selected4 = person;
		await Form4.EditItemAsync(_selected4, FormModes.Edit).ConfigureAwait(true);
		await Modal4.ShowAsync().ConfigureAwait(true);
	}

	// ── Example 5: No Cancel button ──
	private async Task OnExample5NoCancelAsync(Person person)
	{
		_selected5 = person;
		await Form5.EditItemAsync(_selected5, FormModes.Edit).ConfigureAwait(true);
		await Modal5.ShowAsync().ConfigureAwait(true);
	}

	// ── Example 6: Immediate validation on a blank Create form ──
	private async Task OnExample6ValidateAsync()
	{
		_selected6 = new Person(); // completely empty - all required fields will show errors immediately
		await Form6.EditItemAsync(_selected6, FormModes.Create, validate: true).ConfigureAwait(true);
		await Modal6.ShowAsync().ConfigureAwait(true);
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
