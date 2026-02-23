namespace PanoramicData.Blazor;

public partial class PDFormBody<TItem> : IAsyncDisposable where TItem : class
{
	private IJSObjectReference? _commonModule;

	/// <summary>
	/// Injected javascript interop object.
	/// </summary>
	[Inject] public IJSRuntime JSRuntime { get; set; } = null!;

	/// <summary>
	/// Sets the debounce wait period in milliseconds.
	/// </summary>
	[Parameter] public int DebounceWait { get; set; }

	/// <summary>
	/// Form the component belongs to.
	/// </summary>
	[CascadingParameter] public PDForm<TItem>? Form { get; set; }

	/// <summary>
	/// Gets or sets a linked PDTable instance that can be used to provide field definitions.
	/// </summary>
	[Parameter] public PDTable<TItem>? Table { get; set; }

	/// <summary>
	/// Child HTML content.
	/// </summary>
	[Parameter] public RenderFragment ChildContent { get; set; } = null!;

	/// <summary>
	/// Gets or sets whether the validation indicator should be shown for fields.
	/// </summary>
	[Parameter] public bool ShowValidationIndicator { get; set; } = true;

	/// <summary>
	/// Gets or sets the width, in Pixels, of the Title box.
	/// </summary>
	[Parameter] public int TitleWidth { get; set; } = 200;

	// Keep for backwards compatability so browser refresh is not required to refresh css cache
	private MarkupString WidthCssMarkup => new($".title-box {{ min-width: {TitleWidth}px }}");

	/// <summary>
	/// Gets the inline style for setting the title width using CSS custom properties.
	/// </summary>
	private string GetTitleWidthStyle()
		=> TitleWidth > 0
			? $"--title-width: {TitleWidth}px;"
			: string.Empty;

	public async ValueTask DisposeAsync()
	{
		try
		{
			GC.SuppressFinalize(this);
			if (_commonModule != null)
			{
				await _commonModule.DisposeAsync().ConfigureAwait(true);
			}
		}
		catch
		{
		}
	}

	public List<FieldGroup<TItem>> Group(IEnumerable<FormField<TItem>> fields)
	{
		var index = 0;
		var groups = new List<FieldGroup<TItem>>();
		var dict = new Dictionary<string, FieldGroup<TItem>>();
		foreach (var field in fields)
		{
			if (string.IsNullOrWhiteSpace(field.Group))
			{
				// create separate group for single field
				groups.Add(new FieldGroup<TItem>() { Id = $"group-{++index}", Fields = [field] });
			}
			else
			{
				if (dict.TryGetValue(field.Group, out var group))
				{
					group.Fields.Add(field);
				}
				else
				{
					// create new group
					var g = new FieldGroup<TItem>()
					{
						Id = $"group-{++index}",
						Fields = [field]
					};
					// add to dict for lookup / grouping by id
					dict.Add(field.Group, g);
					// add to results - provides ordering
					groups.Add(g);
				}
			}
		}

		return groups;
	}

	protected override async Task OnAfterRenderAsync(bool firstRender)
	{
		if (firstRender && JSRuntime is not null)
		{
			try
			{
				_commonModule = await JSRuntime.InvokeAsync<IJSObjectReference>("import", JSInteropVersionHelper.CommonJsUrl);
			}
			catch
			{
				// BC-40 - fast page switching in Server Side blazor can lead to OnAfterRender call after page / objects disposed
			}
		}
	}

	protected override void OnParametersSet()
	{
		// set fields from table columns
		if (Table != null && Form != null && Form.Fields.Count == 0)
		{
			foreach (var column in Table.Columns)
			{
				Form.Fields.Add(new FormField<TItem>
				{
					AutoComplete = column.AutoComplete,
					Description = column.Description,
					DescriptionFunc = column.DescriptionFunc,
					Field = column.Field,
					DisplayOptions = column.DisplayOptions,
					Id = column.Id,
					ReadOnlyInCreate = column.ReadOnlyInCreate,
					ReadOnlyInEdit = column.ReadOnlyInEdit,
					ShowCopyButton = column.ShowCopyButton,
					ShowInCreate = column.ShowInCreate,
					ShowInDelete = column.ShowInDelete,
					ShowInEdit = column.ShowInEdit,
					EditTemplate = column.EditTemplate,
					Helper = column.Helper,
				MaxLength = column.MaxLength,
					MinValue = column.MinValue,
					MaxValue = column.MaxValue,
					Title = column.GetTitle(),
					TitleFunc = column.TitleFunc,
					Options = column.Options,
					OptionsAsync = column.OptionsAsync,
					IsPassword = column.IsPassword,
					IsSensitive = column.IsSensitive,
					IsTextArea = column.IsTextArea,
					IsImage = column.IsImage,
					TextAreaRows = column.TextAreaRows,
					ShowValidationResult = column.ShowValidationResult,
					HelpUrl = column.HelpUrl
				});
			}
		}
	}

	public bool IsShown(FormField<TItem> field) => IsShown(field, null);

	public bool IsShown(FormField<TItem> field, FormModes? mode)
	{
		mode ??= Form?.Mode;

		return (mode == FormModes.Create && field.ShowInCreate(Form?.GetItemWithUpdates())) ||
			((mode == FormModes.Edit || mode == FormModes.ReadOnly) && field.ShowInEdit(Form?.GetItemWithUpdates())) ||
			(mode == FormModes.Delete && field.ShowInDelete(Form?.GetItemWithUpdates()));
	}

	public bool IsReadOnly(FormField<TItem> field) =>
		(Form?.Mode == FormModes.Create && field.ReadOnlyInCreate(Form?.GetItemWithUpdates())) ||
		(Form?.Mode == FormModes.Edit && field.ReadOnlyInEdit(Form?.GetItemWithUpdates())) ||
		Form?.Mode == FormModes.Delete ||
		Form?.Mode == FormModes.Cancel ||
		Form?.Mode == FormModes.ReadOnly;

	public string GetEditorClass(FormField<TItem> field) => Form?.Errors.ContainsKey(field.GetName() ?? "") == true ? "invalid" : "";

	private async Task OnHelperClick(FormField<TItem> field)
	{
		if (field != null && Form != null && (field?.Helper?.Click != null || field?.Helper?.ClickAsync != null))
		{
			FormFieldResult result = new() { Canceled = true };
			if (field.Helper?.Click != null)
			{
				result = field.Helper.Click(field);
			}
			else if (field.Helper?.ClickAsync != null)
			{
				result = await field.Helper.ClickAsync(field).ConfigureAwait(true);
			}

			if (!result.Canceled && result.NewValue != null)
			{
				await Form.SetFieldValueAsync(field, result.NewValue).ConfigureAwait(true);
			}
		}
	}

	private async Task OnHelpUrlClick(FormField<TItem> field)
	{
		if (_commonModule != null)
		{
			await _commonModule.InvokeVoidAsync("openUrl", field.HelpUrl, "pd-help-page");
		}
	}

	private string GetValidationCssClass(FormField<TItem> field)
	{
		var fieldName = field.GetName();
		if (IsReadOnly(field) || !field.ShowValidationResult)
		{
			return string.Empty;
		}
		else if (fieldName != null && !field.SuppressErrors && Form?.Errors?.ContainsKey(fieldName) == true)
		{
			return "alert-danger";
		}
		else if (fieldName != null && field.SuppressErrors && Form?.Errors?.ContainsKey(fieldName) == true)
		{
			return "alert-warning";
		}
		// check if field is required
		//		else if (Form != null && field.GetIsRequired() && string.IsNullOrWhiteSpace(Form?.GetFieldValue(field)?.ToString()))
		//		{
		//			return "alert-warning";
		//		}
		else
		{
			return "alert-success";
		}
	}

	private string GetValidationCssClass(IEnumerable<FormField<TItem>> fields)
	{
		var classes = new List<string>();
		foreach (var field in fields)
		{
			var key = GetValidationCssClass(field!);
			if (key == "alert-danger")
			{
				return "alert-danger";
			}

			classes.Add(key);
		}

		if (classes.Contains("alert-warning"))
		{
			return "alert-warning";
		}

		return classes.Contains("alert-success") ? "alert-success" : string.Empty;
	}

	private static string GetValidationIconForCssClass(string cssClass) => "d-table-cell pt-1 fas fa-fw " + cssClass switch
	{
		"alert-danger" => "fa-exclamation-circle",
		"alert-warning" => "fa-asterisk",
		"alert-success" => "fa-check-circle",
		_ => ""
	};
}