namespace PanoramicData.Blazor.Demo.Pages;

public partial class PDFormPage3
{
	private readonly PersonDataProvider _personDataProvider = new();
	private readonly PageCriteria _pageCriteria = new(1, 10);
	private readonly SortCriteria _sortCriteria = new("DateCreatedCol", SortDirection.Descending);

	//private bool ShowDescriptions { get; set; }
	private PDForm<Person> Form { get; set; } = null!;
	private PDFormBody<Person> FormBody { get; set; } = null!;
	private PDTable<Person> Table { get; set; } = null!;
	private Person? SelectedPerson { get; set; }

	[CascadingParameter] protected EventManager? EventManager { get; set; }

	private async Task OnPersonCreated(Person person)
	{
		EventManager?.Add(new Event("PersonCreated", new EventArgument("Forename", person.FirstName), new EventArgument("Surname", person.LastName)));
		await Table.RefreshAsync().ConfigureAwait(true);
	}

	private async Task OnPersonUpdated(Person person)
	{
		EventManager?.Add(new Event("PersonUpdated", new EventArgument("Forename", person.FirstName), new EventArgument("Surname", person.LastName)));
		await Table.RefreshAsync().ConfigureAwait(true);
	}

	private async Task OnPersonDeleted(Person person)
	{
		EventManager?.Add(new Event("PersonDeleted", new EventArgument("Forename", person.FirstName), new EventArgument("Surname", person.LastName)));
		await Table.RefreshAsync().ConfigureAwait(true);
	}

	private void OnError(string message)
	{
		EventManager?.Add(new Event("Error", new EventArgument("Message", message)));
	}

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
		if (Table?.Selection.Count > 0)
		{
			var id = int.Parse(Table.Selection[0]);
			SelectedPerson = Table.ItemsToDisplay.Find(x => x.Id == id);
			if (SelectedPerson != null)
			{
				await Form.EditItemAsync(SelectedPerson, FormModes.Edit).ConfigureAwait(true);
			}
		}
	}

	private static OptionInfo[] GetLocationOptions(FormField<Person> _, Person item)
	{
		var options = new List<OptionInfo>();
		for (var i = 0; i < PersonDataProvider.Locations.Length; i++)
		{
			options.Add(new OptionInfo
			{
				Text = PersonDataProvider.Locations[i],
				Value = i,
				IsSelected = item?.Location == i,
				IsDisabled = PersonDataProvider.Locations[i] == "Sydney"
			});
		}
		return options.ToArray();
	}

	private async Task OnInitialsInput(ChangeEventArgs args)
	{
		// custom processing - all chars to have single period separator and uppercase
		var newValue = args.Value?.ToString()?.Replace(".", "");
		newValue = newValue == null ? string.Empty : String.Join(".", newValue.ToArray()).ToUpper();
		await Form.SetFieldValueAsync(Form.Fields.First(x => x.Id == "InitialsCol"), newValue).ConfigureAwait(true);
	}

	private async Task OnEmailInput(ChangeEventArgs args)
	{
		await Form.SetFieldValueAsync(Form.Fields.First(x => x.Id == "EmailCol"), args?.Value ?? string.Empty).ConfigureAwait(true);
	}

	private void OnCustomValidate(CustomValidateArgs<Person> args)
	{
		if (args.Item != null)
		{
			var fieldName = args.Field.GetName();
			if (fieldName == "Initials")
			{
				if (args.Item.Initials == "L.O.L")
				{
					args.AddErrorMessages.Add("Initials", "Laugh out loud - really?");
				}
			}

			if (fieldName == "Location" || fieldName == "Department")
			{
				const string? errorMessage = "Peckham location only has Sales departments";
				var locationField = Form.Fields.Find(x => x.Id == "location");
				var isPeckham = locationField != null && Form.GetFieldStringValue(locationField) == "4";
				var departmentField = Form.Fields.Find(x => x.Id == "department");
				var isSales = departmentField != null && Form.GetFieldStringValue(departmentField) == "Sales";
				if (isPeckham && !isSales)
				{
					args.AddErrorMessages.Add("Location", errorMessage);
					args.AddErrorMessages.Add("Department", errorMessage);
				}
				else
				{
					args.RemoveErrorMessages.Add("Location", errorMessage);
					args.RemoveErrorMessages.Add("Department", errorMessage);
				}
			}
		}
	}
}
