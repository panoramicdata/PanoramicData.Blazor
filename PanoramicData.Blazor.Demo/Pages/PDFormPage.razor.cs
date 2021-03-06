﻿using Microsoft.AspNetCore.Components;
using PanoramicData.Blazor.Demo.Data;
using PanoramicData.Blazor.Services;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace PanoramicData.Blazor.Demo.Pages
{
	public partial class PDFormPage
	{
		private readonly PersonDataProvider _personDataProvider = new();
		private PDForm<Person> Form { get; set; } = null!;
		private List<Person> People { get; set; } = new List<Person>();
		private Person? SelectedPerson { get; set; }

		[CascadingParameter] protected EventManager? EventManager { get; set; }

		public PDFormPage()
		{
			RefreshPeople();
		}

		private void OnFooterClick(string key)
		{
			EventManager?.Add(new Event("FooterClick", new EventArgument("Key", key)));

			if (key == "Cancel")
			{
				SelectedPerson = null;
				Form.SetMode(FormModes.Hidden);
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

		private void OnError(string message)
		{
			EventManager?.Add(new Event("Error", new EventArgument("Message", message)));
		}

		private void RefreshPeople()
		{
			_personDataProvider
				.GetDataAsync(new DataRequest<Person>
				{
					Take = 5,
					SortFieldExpression = (x) => x.DateCreated,
					SortDirection = SortDirection.Descending
				}, CancellationToken.None)
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
