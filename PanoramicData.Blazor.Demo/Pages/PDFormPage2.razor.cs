using System;
using System.Threading;
using System.Threading.Tasks;
using System.Collections.Generic;
using Microsoft.AspNetCore.Components;
using Microsoft.JSInterop;
using PanoramicData.Blazor.Services;
using PanoramicData.Blazor.Demo.Data;

namespace PanoramicData.Blazor.Demo.Pages
{
    public partial class PDFormPage2
    {
		private readonly PersonDataProvider PersonDataProvider = new PersonDataProvider(5);
		private string _events = string.Empty;

		// properties for unlinked example
		private PDForm<Person> Form { get; set; } = null!;
		private List<Person> People { get; set; } = new List<Person>();
		private Person? SelectedPerson { get; set; }

		[Inject] public IJSRuntime? JSRuntime { get; set; }

		public PDFormPage2()
		{
			RefreshPeople();
		}

		private void OnFooterClick(string key)
		{
			_events += $"click: key = {key}{Environment.NewLine}";
			if (key == "Cancel")
			{
				SelectedPerson = null;
				HideDialog();
			}
		}

		private void OnPersonCreated(Person person)
		{
			_events += $"created: Person {person.FirstName} {person.LastName}{Environment.NewLine}";
			HideDialog();
			RefreshPeople();
		}

		private void OnPersonUpdated(Person person)
		{
			_events += $"updated: Person {person.FirstName} {person.LastName}{Environment.NewLine}";
			HideDialog();
			RefreshPeople();
		}

		private void OnPersonDeleted(Person person)
		{
			_events += $"deleted: Person {person.FirstName} {person.LastName}{Environment.NewLine}";
			HideDialog();
			RefreshPeople();
		}

		private void OnError(string message)
		{
			_events += $"error: {message}{Environment.NewLine}";
		}

		private void RefreshPeople()
		{
			PersonDataProvider
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

		private void OnEditPerson(Person person)
		{
			SelectedPerson = person;
			Form.SetMode(FormModes.Edit);
			JSRuntime.InvokeVoidAsync("showBsDialog", "#exampleModal");
		}

		private void OnCreatePerson()
		{
			SelectedPerson = new Person();
			Form.SetMode(FormModes.Create);
			JSRuntime.InvokeVoidAsync("showBsDialog", "#exampleModal");
		}

		private void HideDialog()
		{
			JSRuntime.InvokeVoidAsync("hideBsDialog", "#exampleModal");
		}
	}
}
