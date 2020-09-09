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
    public partial class PDFormPage3
    {
		private readonly PersonDataProvider PersonDataProvider = new PersonDataProvider(5);
		private string _events = string.Empty;

		// properties for unlinked example
		private PDForm<Person> Form { get; set; } = null!;
		private PDTable<Person> Table { get; set; } = null!;
		private Person SelectedPerson { get; set; }

		private async Task OnPersonCreated(Person person)
		{
			_events += $"created: Person {person.FirstName} {person.LastName}{Environment.NewLine}";
			await Table.RefreshAsync().ConfigureAwait(true);
		}

		private void OnPersonUpdated(Person person)
		{
			_events += $"updated: Person {person.FirstName} {person.LastName}{Environment.NewLine}";
		}

		private async Task OnPersonDeleted(Person person)
		{
			_events += $"deleted: Person {person.FirstName} {person.LastName}{Environment.NewLine}";
			await Table.RefreshAsync().ConfigureAwait(true);
		}

		private void OnError(string message)
		{
			_events += $"error: {message}{Environment.NewLine}";
		}

		private void OnFooterClick(string key)
		{
			_events += $"click: key = {key}{Environment.NewLine}";
			if (key == "Cancel")
			{
				SelectedPerson = null;
				Table.ClearSelection();
				Form.SetMode(FormModes.Empty);
			}
		}

		private void OnEditPerson(Person person)
		{
			SelectedPerson = person;
			Form.SetMode(FormModes.Edit);
		}

		private void OnCreatePerson()
		{
			SelectedPerson = new Person();
			Form.SetMode(FormModes.Create);
		}

		private void OnSelectionChanged()
		{
			if (Table?.Selection.Count > 0)
			{
				var id = int.Parse(Table.Selection[0]);
				SelectedPerson = Table.ItemsToDisplay.Find(x => x.Id == id);
				if(SelectedPerson != null)
				{
					Form.SetMode(FormModes.Edit);
				}
			}
		}
	}
}
