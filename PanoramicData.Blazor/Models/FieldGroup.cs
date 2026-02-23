namespace PanoramicData.Blazor.Models;

public class FieldGroup<TItem> where TItem : class
{
	public string Id { get; set; } = string.Empty;

	public List<FormField<TItem>> Fields { get; set; } = [];

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

	/// <summary>
	/// Returns the group title, evaluating the first field's <see cref="FormField{TItem}.TitleFunc"/> if set.
	/// </summary>
	/// <param name="item">The current item.</param>
	public string GetTitle(TItem? item = default)
	{
		var firstField = Fields.FirstOrDefault();
		return string.IsNullOrWhiteSpace(firstField?.Group)
			? (firstField?.GetTitle(item) ?? string.Empty)
			: firstField.Group;
	}

	public bool HasErrors(PDForm<TItem>? form)
	{
		if (form != null)
		{
			foreach (var field in Fields)
			{
				if (form?.Errors?.ContainsKey(field.GetName() ?? string.Empty) == true)
				{
					return true;
				}
			}
		}

		return false;
	}
}
