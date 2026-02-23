namespace PanoramicData.Blazor;

public partial class PDField<TItem> where TItem : class
{
	/// <summary>
	/// The parent PDForm instance.
	/// </summary>
	[CascadingParameter(Name = "FormBody")]
	public PDFormBody<TItem> FormBody { get; set; } = null!;

	/// <summary>
	/// The Id - this should be unique per column in a table
	/// </summary>
	[Parameter] public string Id { get; set; } = string.Empty;

	/// <summary>
	/// A Linq expression that selects the field to be data bound to.
	/// </summary>
	[Parameter] public Expression<Func<TItem, object>>? Field { get; set; }

	/// <summary>
	/// If set will override the Field's name
	/// </summary>
	[Parameter]
	public string? Title { get; set; }

	/// <summary>
	/// Gets or sets a function that returns the title for the field.
	/// </summary>
	/// <remarks>When set, takes precedence over the <see cref="Title"/> property.</remarks>
	[Parameter]
	public Func<TItem?, string>? TitleFunc { get; set; }

	/// <summary>
	/// Gets or sets the autocomplete attribute value.
	/// </summary>
	[Parameter] public string AutoComplete { get; set; } = string.Empty;

	/// <summary>
	/// Gets or sets optional display options.
	/// </summary>
	[Parameter] public FieldDisplayOptions DisplayOptions { get; set; } = new FieldDisplayOptions();

	/// <summary>
	/// Gets or sets a short description of the fields purpose. Overrides DisplayAttribute description if set.
	/// </summary>
	[Parameter] public string? Description { get; set; }

	/// <summary>
	/// Gets or sets a function that returns the description for the field.
	/// </summary>
	[Parameter]
	public Func<FormField<TItem>, PDForm<TItem>?, string> DescriptionFunc { get; set; } = Constants.Functions.FormFieldDescription;

	/// <summary>
	/// Gets or sets name of the group the field belongs to.
	/// </summary>
	[Parameter] public string Group { get; set; } = string.Empty;

	/// <summary>
	/// Gets or sets text that is displayed in various ways depending on the control type. For example
	/// in a textbox will be displayed when no text has been entered as a place holder.
	/// </summary>
	[Parameter] public string Label { get; set; } = string.Empty;

	/// <summary>
	/// Gets or sets whether a 'copy to clipboard' button is displayed for the field.
	/// </summary>
	[Parameter] public Func<TItem?, bool> ShowCopyButton { get; set; } = Constants.Functions.False;

	/// <summary>
	/// Gets or sets a function that determines whether this field is visible when the form mode is Edit.
	/// </summary>
	[Parameter] public Func<TItem?, bool> ShowInEdit { get; set; } = Constants.Functions.True;

	/// <summary>
	/// Gets or sets a function that determines whether this field is visible when the form mode is Create.
	/// </summary>
	[Parameter] public Func<TItem?, bool> ShowInCreate { get; set; } = Constants.Functions.True;

	/// <summary>
	/// Gets or sets a function that determines whether this field is visible when the form mode is Create.
	/// </summary>
	[Parameter] public Func<TItem?, bool> ShowInDelete { get; set; } = Constants.Functions.False;

	/// <summary>
	/// Gets or sets a function that determines whether this field is read-only when the form mode is Edit.
	/// </summary>
	[Parameter] public Func<TItem?, bool> ReadOnlyInEdit { get; set; } = Constants.Functions.False;

	/// <summary>
	/// Gets or sets a function that determines whether this field is read-only when the form mode is Create.
	/// </summary>
	[Parameter] public Func<TItem?, bool> ReadOnlyInCreate { get; set; } = Constants.Functions.False;

	/// <summary>
	/// Gets a function that returns available value choices.
	/// </summary>
	[Parameter] public Func<FormField<TItem>, TItem?, OptionInfo[]>? Options { get; set; }

	/// <summary>
	/// Gets an asynchronous function that returns available value choices.
	/// </summary>
	[Parameter] public Func<FormField<TItem>, TItem?, Task<OptionInfo[]>>? OptionsAsync { get; set; }

	/// <summary>
	/// Gets whether this field contains passwords or other sensitive information.
	/// </summary>
	[Parameter] public bool IsPassword { get; set; }

	/// <summary>
	/// Gets or sets a function that determines whether this field contains sensitive values that should not be shown.
	/// </summary>
	[Parameter] public Func<TItem?, PDForm<TItem>?, bool> IsSensitive { get; set; } = Constants.Functions.FormFieldIsSensitive;

	/// <summary>
	/// Gets or sets whether this field contains longer sections of text.
	/// </summary>
	[Parameter] public bool IsTextArea { get; set; }

	/// <summary>
	/// Gets or sets whether this field contains an image
	/// If the field is a string, then the string is treated as the image URL
	/// </summary>
	[Parameter] public bool IsImage { get; set; }

	/// <summary>
	/// Gets or sets the number of rows of text displayed by default in a text area.,
	/// </summary>
	[Parameter] public int TextAreaRows { get; set; } = 4;

	/// <summary>
	/// Gets or sets the maximum length for entered text.
	/// </summary>
	[Parameter] public int? MaxLength { get; set; }

	/// <summary>
	/// Gets or sets the maximum value allowed for numeric fields.
	/// </summary>
	[Parameter] public double? MaxValue { get; set; }

	/// <summary>
	/// Gets or sets the minimum value allowed for numeric fields.
	/// </summary>
	[Parameter] public double? MinValue { get; set; }

	/// <summary>
	/// Gets or sets whether the validation result should be shown when displayed.
	/// </summary>
	[Parameter] public bool ShowValidationResult { get; set; } = true;

	/// <summary>
	/// Gets or sets an HTML template for editing.
	/// </summary>
	[Parameter] public RenderFragment<TItem?>? EditTemplate { get; set; }

	/// <summary>
	/// Gets or sets an HTML template for the fields editor.
	/// </summary>
	[Parameter] public RenderFragment<TItem>? Template { get; set; }

	/// <summary>
	/// Gets or sets an optional helper for filling in the field.
	/// </summary>
	[Parameter] public FormFieldHelper<TItem>? Helper { get; set; }

	/// <summary>
	/// Gets or sets a URL to an external context sensitive help page.
	/// </summary>
	[Parameter] public string? HelpUrl { get; set; }

	public string GetTitle(TItem? item = default)
	{
		if (TitleFunc is not null)
		{
			return TitleFunc(item);
		}

		if (Title is not null)
		{
			return Title;
		}

		var memberInfo = Field?.GetPropertyMemberInfo();
		return memberInfo is PropertyInfo propInfo
			? propInfo.GetCustomAttribute<DisplayAttribute>()?.Name ?? propInfo.Name
			: memberInfo?.Name ?? string.Empty;
	}

	protected override async Task OnInitializedAsync()
	{
		if (FormBody is null || FormBody.Form is null)
		{
			throw new InvalidOperationException("Error initializing field. " +
				"FormBody reference is null which implies it did not initialize or that the field " +
				$"type '{typeof(TItem)}' does not match the form type.");
		}

		await FormBody.Form.AddFieldAsync(this).ConfigureAwait(true);
	}
}
