namespace PanoramicData.Blazor.Demo.Pages;

public partial class PDFormPage
{
	private readonly PersonDataProvider _personDataProvider = new();
	private readonly FormFieldHelper<Person> _dateHelper = new();
	private PDForm<Person> Form { get; set; } = null!;
	private List<Person> People { get; set; } = [];
	private Person? SelectedPerson { get; set; }

	[Inject] private INavigationCancelService NavigationCancelService { get; set; } = default!;

	[CascadingParameter] protected EventManager? EventManager { get; set; }

	//private FieldStringOptions TextEditorOptions => new FieldStringOptions
	//{
	//	Editor = FieldStringOptions.Editors.TextArea,
	//	Resize = true,
	//	ResizeCssCls = "mh-150-px"
	//};

	// MONACO editor example

	private FieldStringOptions TextEditorOptions
	{
		get
		{
			return new FieldStringOptions
			{
				CssClass = "",
				Editor = FieldStringOptions.Editors.Monaco,
				MonacoOptions = (_) => new BlazorMonaco.Editor.StandaloneEditorConstructionOptions
				{
					AutomaticLayout = true,
					Language = "sql"
				},
				Resize = true,
				ResizeCssCls = "mh-200-px"
			};
		}
	}

	public PDFormPage()
	{
		// configure the date helper
		_dateHelper.Click = (_) =>
		{
			return new FormFieldResult
			{
				Canceled = false,
				NewValue = DateTime.Now
			};
		};
		_dateHelper.IconCssClass = "fas fa-calendar-day";
		_dateHelper.ToolTip = "Set the date and time to now";

		RefreshPeople();
	}

	private void OnFieldUpdated(FieldUpdateArgs<Person> args)
	{
		if (args.Field.Name == "AllowLogin")
		{
			StateHasChanged();
		}
	}

	private async Task OnFooterClick(string key)
	{
		EventManager?.Add(new Event("FooterClick", new EventArgument("Key", key)));

		if (key == "Save")
		{
			await Task.Delay(5000);
		}

		if (key == "Cancel")
		{
			SelectedPerson = null;
			await Form.EditItemAsync(null, FormModes.Hidden).ConfigureAwait(true);
		}
	}

	private void OnPersonCreated(Person person)
	{
		EventManager?.Add(new Event("PersonCreated", new EventArgument("Forename", person.FirstName), new EventArgument("Surname", person.LastName)));
		RefreshPeople();
	}

	private void OnPersonUpdated(Person person)
	{
		EventManager?.Add(new Event("PersonUpdated", new EventArgument("Forename", person.FirstName), new EventArgument("Surname", person.LastName)));
		RefreshPeople();
	}

	private void OnPersonDeleted(Person person)
	{
		EventManager?.Add(new Event("PersonDeleted", new EventArgument("Forename", person.FirstName), new EventArgument("Surname", person.LastName)));
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
	private async Task OnEditPerson(Person? person)
	{
		if (await NavigationCancelService.ProceedAsync().ConfigureAwait(true))
		{
			SelectedPerson = person;
			await Form.EditItemAsync(SelectedPerson, FormModes.Edit).ConfigureAwait(true);
		}
	}

	private async Task OnCreatePerson()
	{
		if (await NavigationCancelService.ProceedAsync().ConfigureAwait(true))
		{
			SelectedPerson = new Person();
			await Form.EditItemAsync(SelectedPerson, FormModes.Create).ConfigureAwait(true);
		}
	}
}
