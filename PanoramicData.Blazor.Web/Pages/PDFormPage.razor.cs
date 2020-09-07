using System;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Collections.Generic;
using PanoramicData.Blazor.Web.Data;
using PanoramicData.Blazor.Services;

namespace PanoramicData.Blazor.Web.Pages
{
    public partial class PDFormPage
    {
		private readonly PersonDataProvider PersonDataProvider = new PersonDataProvider();
		private string _events = string.Empty;

		// properties for unlinked example
		private PDForm<Person> Form1 { get; set; } = null!;
		private List<Person> People { get; set; } = new List<Person>();
		private Person SelectedPerson { get; set; }

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
			PersonDataProvider
				.GetDataAsync(new Services.DataRequest<Person>(), CancellationToken.None)
				.ContinueWith(PopulatePeopleResult);
		}

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
}
