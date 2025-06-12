namespace PanoramicData.Blazor;

public partial class PDFormHeader<TItem> where TItem : class
{
	/// <summary>
	/// Form the component belongs to.
	/// </summary>
	[CascadingParameter] public PDForm<TItem>? Form { get; set; }

	/// <summary>
	/// Gets or sets the custom child content to be displayed in the header.
	/// </summary>
	[Parameter] public RenderFragment? ChildContent { get; set; }

	/// <summary>
	/// provides a function that will get a short description of the item being edited.
	/// </summary>
	[Parameter] public Func<TItem, string>? ItemDescription { get; set; }

	/// <summary>
	/// Gets or sets the help text for the form.
	/// </summary>
	[Parameter] public string HelpText { get; set; } = string.Empty;

	/// <summary>
	/// Gets or sets the title for when the form is in cancel mode.
	/// </summary>
	/// <remarks>If present, the placeholder {0} will be substituted by the result of GetItemDescription() </remarks>
	[Parameter] public string CancelTitle { get; set; } = string.Empty;

	/// <summary>
	/// Gets or sets the title for a create form. if omitted then an automatic title is generated.
	/// </summary>
	/// <remarks>If present, the placeholder {0} will be substituted by the result of GetItemDescription() </remarks>
	[Parameter] public string CreateTitle { get; set; } = string.Empty;

	/// <summary>
	/// Gets or sets the title for an edit form. if omitted then an automatic title is generated.
	/// </summary>
	/// <remarks>If present, the placeholder {0} will be substituted by the result of GetItemDescription() </remarks>
	[Parameter] public string EditTitle { get; set; } = string.Empty;

	/// <summary>
	/// Gets or sets the title for a delete form. if omitted then an automatic title is generated.
	/// </summary>
	/// <remarks>If present, the placeholder {0} will be substituted by the result of GetItemDescription() </remarks>
	[Parameter] public string DeleteTitle { get; set; } = string.Empty;

	private static string GetClassDescription() =>
		// look for Display attribute and use name if provided, else fall-back to using class name.
		typeof(TItem).GetCustomAttribute<DisplayAttribute>()?.Name ?? typeof(TItem).Name;

	private string GetItemDescription() => Form?.Item is null || ItemDescription is null ? PDFormHeader<TItem>.GetClassDescription() : ItemDescription(Form.Item);
}
