namespace PanoramicData.Blazor.Models;

/// <summary>
/// Represents a named group of <see cref="FormField{TItem}"/> instances displayed together within a <see cref="PDForm{TItem}"/>.
/// </summary>
/// <typeparam name="TItem">The model type that the form fields are bound to.</typeparam>
public class FieldGroup<TItem> where TItem : class
{
	/// <summary>Gets or sets the unique identifier for this group.</summary>
	public string Id { get; set; } = string.Empty;

	/// <summary>Gets or sets the list of form fields that belong to this group.</summary>
	public List<FormField<TItem>> Fields { get; set; } = [];

	/// <summary>
	/// Gets the display title of the group, taken from the first field's <see cref="FormField{TItem}.Group"/> or <see cref="FormField{TItem}.Title"/>.
	/// </summary>
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

	/// <summary>
	/// Returns true if the supplied form has at least one validation error for any field in this group.
	/// </summary>
	/// <param name="form">The form instance whose error dictionary is inspected.</param>
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
