namespace PanoramicData.Blazor.Models;

public class FieldGroup<TItem> where TItem : class
{
	public List<FormField<TItem>> Fields { get; set; } = new();

	public string Title => Fields.FirstOrDefault()?.Grouping?.GroupName
			?? Fields?.FirstOrDefault()?.Title
			?? string.Empty;

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
