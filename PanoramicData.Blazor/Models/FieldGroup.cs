﻿namespace PanoramicData.Blazor.Models;

public class FieldGroup<TItem> where TItem : class
{
	public List<FormField<TItem>> Fields { get; set; } = new();

	public string Title
	{
		get
		{
			var firstField = Fields.FirstOrDefault();
			return string.IsNullOrWhiteSpace(firstField?.Group)
				? (firstField?.Title ?? string.Empty)
				: firstField.Group;
		}
	}

	public bool HasErrors(PDForm<TItem>? form)
	{
		if (form != null)
		{
			foreach (var field in Fields)
			{
				if (field.Name == "StopOn")
				{
					var a = 9;
				}

				if (form?.Errors?.ContainsKey(field.GetName() ?? string.Empty) == true)
				{
					return true;
				}
			}
		}

		return false;
	}
}
