using System;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Collections.Generic;
using PanoramicData.Blazor.Services;
using PanoramicData.Blazor.Demo.Data;

namespace PanoramicData.Blazor.Demo.Pages
{
    public partial class PDFormPage
    {
		private readonly PersonDataProvider _personDataProvider = new PersonDataProvider(5);
		private string _events = string.Empty;

		// properties for unlinked example
		private PDForm<Person> Form { get; set; } = null!;
		private List<Person> People { get; set; } = new List<Person>();
		private Person? SelectedPerson { get; set; }

		public PDFormPage()
		{
			RefreshPeople();
		}

		private void OnFooterClick(string key)
		{
			_events += $"click: key = {key}{Environment.NewLine}";
			if (key == "Cancel")
			{
				SelectedPerson = null;
				Form.SetMode(FormModes.Hidden);
			}
		}

		private void OnPersonCreated(Person person)
		{
			_events += $"created: Person {person.FirstName} {person.LastName}{Environment.NewLine}";
			RefreshPeople();
		}

		private void OnPersonUpdated(Person person)
		{
			_events += $"updated: Person {person.FirstName} {person.LastName}{Environment.NewLine}";
			RefreshPeople();
		}

		private void OnPersonDeleted(Person person)
		{
			_events += $"deleted: Person {person.FirstName} {person.LastName}{Environment.NewLine}";
			RefreshPeople();
		}

		private void OnError(string message)
		{
			_events += $"error: {message}{Environment.NewLine}";
		}

		private void RefreshPeople()
		{
			_personDataProvider
				.GetDataAsync(new Services.DataRequest<Person> { Take = 100 }, CancellationToken.None)
				.ContinueWith(PopulatePeopleResult);
		}

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
		private void OnEditPerson(Person? person)
		{
			SelectedPerson = person;
			Form.SetMode(FormModes.Edit);
		}

		private void OnCreatePerson()
		{
			SelectedPerson = new Person();
			Form.SetMode(FormModes.Create);
		}

	}
}
