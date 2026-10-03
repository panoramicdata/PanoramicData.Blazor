using System.Globalization;

namespace PanoramicData.Blazor.Demo.Pages;

/// <summary>
/// Custom field editors and custom validation for the PDForm demo.
/// </summary>
public partial class PDFormPage3
{
	private async Task OnInitialsInput(ChangeEventArgs args)
	{
		// custom processing - all chars to have single period separator and uppercase
		var letters = Convert.ToString(args.Value, CultureInfo.InvariantCulture)?.Replace(".", "") ?? string.Empty;
		var newValue = string.Join(".", letters.ToArray()).ToUpperInvariant();
		await Form.SetFieldValueAsync(Form.Fields.First(x => x.Id == "InitialsCol"), newValue).ConfigureAwait(true);
	}

	private async Task OnEmailInput(ChangeEventArgs args) => await Form.SetFieldValueAsync(Form.Fields.First(x => x.Id == "EmailCol"), args.Value ?? string.Empty).ConfigureAwait(true);

	private void OnCustomValidate(CustomValidateArgs<Person> args)
	{
		if (args.Item is null)
		{
			return;
		}

		var fieldName = args.Field.GetName();
		if (fieldName == "Initials" && args.Item.Initials == "L.O.L")
		{
			args.AddErrorMessages.Add("Initials", "Laugh out loud - really?");
		}

		if (fieldName is "Location" or "Department")
		{
			ValidateLocationDepartment(args);
		}
	}

	private void ValidateLocationDepartment(CustomValidateArgs<Person> args)
	{
		const string? errorMessage = "Peckham location only has Sales departments";
		var isPeckham = FieldHasValue("location", "4");
		var isSales = FieldHasValue("department", "Sales");
		var messages = isPeckham && !isSales ? args.AddErrorMessages : args.RemoveErrorMessages;
		messages.Add("Location", errorMessage);
		messages.Add("Department", errorMessage);
	}

	private bool FieldHasValue(string fieldId, string value) =>
		Form.Fields.Find(x => x.Id == fieldId) is { } field && Form.GetFieldStringValue(field) == value;
}
