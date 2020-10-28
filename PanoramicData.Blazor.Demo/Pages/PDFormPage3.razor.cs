﻿using System;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Collections.Generic;
using Microsoft.AspNetCore.Components;
using PanoramicData.Blazor.Services;
using PanoramicData.Blazor.Demo.Data;
using Microsoft.AspNetCore.Components.Web;

namespace PanoramicData.Blazor.Demo.Pages
{
    public partial class PDFormPage3
    {
		private readonly PersonDataProvider PersonDataProvider = new PersonDataProvider(5);
		private string _events = string.Empty;

		private bool ShowDescriptions { get; set; }
		// properties for unlinked example
		private PDForm<Person> Form { get; set; } = null!;
		private PDFormBody<Person> FormBody { get; set; } = null!;
		private PDTable<Person> Table { get; set; } = null!;
		private Person? SelectedPerson { get; set; }

		private async Task OnPersonCreated(Person person)
		{
			_events += $"created: Person {person.FirstName} {person.LastName}{Environment.NewLine}";
			await Table.RefreshAsync().ConfigureAwait(true);
		}

		private async Task OnPersonUpdated(Person person)
		{
			_events += $"updated: Person {person.FirstName} {person.LastName}{Environment.NewLine}";
			await Table.RefreshAsync().ConfigureAwait(true);
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

		private async Task OnFooterClick(string key)
		{
			_events += $"click: key = {key}{Environment.NewLine}";
			if (key == "Cancel")
			{
				SelectedPerson = null;
				await Table.ClearSelectionAsync().ConfigureAwait(true);
				Form.SetMode(FormModes.Empty);
			}
		}

		//private void OnEditPerson(Person person)
		//{
		//	SelectedPerson = person;
		//	Form.SetMode(FormModes.Edit);
		//}

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

		private OptionInfo[] GetLocationOptions(FormField<Person> field, Person item)
		{
			var options = new List<OptionInfo>();
			for(var i = 0; i  < PersonDataProvider.Locations.Length; i++)
			{
				options.Add(new OptionInfo { Text = PersonDataProvider.Locations[i], Value = i, IsSelected = item?.Location == i });
			}
			return options.ToArray();
		}

		private async Task OnInitialsInput(ChangeEventArgs args)
		{
			// custom processing - all chars to have single period separator and uppercase
			var newValue = args.Value.ToString().Replace(".", "");
			newValue = String.Join(".", newValue.ToArray()).ToUpper();
			await FormBody.SetFieldValueAsync(FormBody.Fields.First(x => x.Id == "InitialsCol"), newValue).ConfigureAwait(true);
		}

		private async Task OnEmailInput(ChangeEventArgs args)
		{
			await FormBody.SetFieldValueAsync(FormBody.Fields.First(x => x.Id == "EmailCol"), args.Value).ConfigureAwait(true);
		}

		private void OnClick(MouseEventArgs _)
		{
			ShowDescriptions = !ShowDescriptions;
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
					var errorMessage = "Peckham location only has Sales departments";
					if (args.Item.Location == PersonDataProvider.Locations.ToList().IndexOf("Peckham") && args.Item.Department != Departments.Sales)
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

		private string FormStyle
		{
			get
			{
				return "border-color: red";
			}
		}
	}
}