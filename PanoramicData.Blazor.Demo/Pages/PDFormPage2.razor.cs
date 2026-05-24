namespace PanoramicData.Blazor.Demo.Pages;

public partial class PDFormPage2
{
	private readonly PersonDataProvider _personDataProvider = new();

	// Per-example form + modal refs
	private PDModal _modal1 = null!;
	private PDModal _modal2 = null!;
	private PDModal _modal3 = null!;
	private PDModal _modal4 = null!;
	private PDModal _modal5 = null!;
	private PDModal _modal6 = null!;

	private PDForm<Person> _form1 = null!;
	private PDForm<Person> _form2 = null!;
	private PDForm<Person> _form3 = null!;
	private PDForm<Person> _form4 = null!;
	private PDForm<Person> _form5 = null!;
	private PDForm<Person> _form6 = null!;

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
		await _form1.EditItemAsync(_selected1, FormModes.Edit).ConfigureAwait(true);
		await _modal1.ShowAsync().ConfigureAwait(true);
	}

	private async Task OnExample1CreateAsync()
	{
		_selected1 = new Person();
		await _form1.EditItemAsync(_selected1, FormModes.Create).ConfigureAwait(true);
		await _modal1.ShowAsync().ConfigureAwait(true);
	}

	// ── Example 2: ReadOnly ──
	private async Task OnExample2ViewAsync(Person person)
	{
		_selected2 = person;
		await _form2.EditItemAsync(_selected2, FormModes.ReadOnly).ConfigureAwait(true);
		await _modal2.ShowAsync().ConfigureAwait(true);
	}

	// ── Example 3: Edit + Delete ──
	private async Task OnExample3DeleteAsync(Person person)
	{
		_selected3 = person;
		await _form3.EditItemAsync(_selected3, FormModes.Edit).ConfigureAwait(true);
		await _modal3.ShowAsync().ConfigureAwait(true);
	}

	// ── Example 4: Custom button text (Approve / Reject) ──
	private async Task OnExample4ApproveAsync(Person person)
	{
		_selected4 = person;
		await _form4.EditItemAsync(_selected4, FormModes.Edit).ConfigureAwait(true);
		await _modal4.ShowAsync().ConfigureAwait(true);
	}

	// ── Example 5: No Cancel button ──
	private async Task OnExample5NoCancelAsync(Person person)
	{
		_selected5 = person;
		await _form5.EditItemAsync(_selected5, FormModes.Edit).ConfigureAwait(true);
		await _modal5.ShowAsync().ConfigureAwait(true);
	}

	// ── Example 6: Immediate validation on a blank Create form ──
	private async Task OnExample6ValidateAsync(Person person)
	{
		_selected6 = new Person(); // completely empty - all required fields will show errors immediately
		await _form6.EditItemAsync(_selected6, FormModes.Create, validate: true).ConfigureAwait(true);
		await _modal6.ShowAsync().ConfigureAwait(true);
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
		await _modal3.HideAsync().ConfigureAwait(true);
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
