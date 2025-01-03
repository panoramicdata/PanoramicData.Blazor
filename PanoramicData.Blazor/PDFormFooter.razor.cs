namespace PanoramicData.Blazor;

public partial class PDFormFooter<TItem> : IDisposable where TItem : class
{
	private bool _formSet;
	private int _errorCount;
	private string _errorFieldNames = string.Empty;

	/// <summary>
	/// Form the component belongs to.
	/// </summary>
	[CascadingParameter] public PDForm<TItem>? Form { get; set; }

	/// <summary>
	/// Event raised whenever the user clicks on a button.
	/// </summary>
	[Parameter] public EventCallback<string> Click { get; set; }

	/// <summary>
	/// Error count message: placeholders => {0} = count {1} = ''/'s' {2} = field titles
	/// </summary>
	[Parameter] public string ErrorCountMessage { get; set; } = "Please correct the highlighted fields";

	/// <summary>
	/// Gets or sets the button sizes.
	/// </summary>
	[Parameter] public ButtonSizes? Size { get; set; }

	/// <summary>
	/// Should the Save button be shown?
	/// </summary>
	[Parameter] public bool ShowSave { get; set; } = true;

	/// <summary>
	/// Should the Cancel button be shown?
	/// </summary>
	[Parameter] public bool ShowCancel { get; set; } = true;

	/// <summary>
	/// Should the Cancel button be shown when in Read-Only mode?
	/// </summary>
	[Parameter] public bool ShowCancelWhenReadOnly { get; set; } = true;

	/// <summary>
	/// Should the Delete button be shown (only applicable when in Edit mode)?
	/// </summary>
	[Parameter] public bool ShowDelete { get; set; } = true;

	/// <summary>
	/// Should the number of errors be shown (when > 0).
	/// </summary>
	[Parameter] public bool ShowErrorCount { get; set; } = true;

	/// <summary>
	/// Sets the text shown on the save button.
	/// </summary>
	[Parameter] public string SaveButtonText { get; set; } = "Save";

	/// <summary>
	/// Sets the icon CSS classes for the save button.
	/// </summary>
	[Parameter] public string SaveButtonCssClass { get; set; } = "btn-primary";

	/// <summary>
	/// Sets the icon CSS classes for the save button icon.
	/// </summary>
	[Parameter] public string SaveButtonIconCssClass { get; set; } = "fas fa-fw fa-save";

	/// <summary>
	/// Sets the text shown on the cancel button.
	/// </summary>
	[Parameter] public string CancelButtonText { get; set; } = "Cancel";

	/// <summary>
	/// Sets the icon CSS classes for the cancel button.
	/// </summary>
	[Parameter] public string CancelButtonCssClass { get; set; } = "btn-secondary";

	/// <summary>
	/// Sets the icon CSS classes for the cancel button icon.
	/// </summary>
	[Parameter] public string CancelButtonIconCssClass { get; set; } = "fas fa-fw fa-times";

	/// <summary>
	/// Sets the text shown on the delete button.
	/// </summary>
	[Parameter] public string DeleteButtonText { get; set; } = "Delete";

	/// <summary>
	/// Sets the icon CSS classes for the delete button.
	/// </summary>
	[Parameter] public string DeleteButtonCssClass { get; set; } = "btn-danger";

	/// <summary>
	/// Sets the icon CSS classes for the delete button icon.
	/// </summary>
	[Parameter] public string DeleteButtonIconCssClass { get; set; } = "fas fa-fw fa-trash-alt";

	/// <summary>
	/// Sets the text shown on the yes button.
	/// </summary>
	[Parameter] public string YesButtonText { get; set; } = "Yes";

	/// <summary>
	/// Sets the icon CSS classes for the yes button.
	/// </summary>
	[Parameter] public string YesButtonCssClass { get; set; } = "btn-danger";

	/// <summary>
	/// Sets the icon CSS classes for the yes button icon.
	/// </summary>
	[Parameter] public string YesButtonIconCssClass { get; set; } = "fas fa-fw fa-check";

	/// <summary>
	/// Sets the text shown on the no button.
	/// </summary>
	[Parameter] public string NoButtonText { get; set; } = "No";

	/// <summary>
	/// Sets the icon CSS classes for the no button.
	/// </summary>
	[Parameter] public string NoButtonCssClass { get; set; } = "btn-secondary";

	/// <summary>
	/// Sets the icon CSS classes for the no button icon.
	/// </summary>
	[Parameter] public string NoButtonIconCssClass { get; set; } = "fas fa-fw fa-times";

	public void Dispose()
	{
		if (Form != null)
		{
			Form.ErrorsChanged -= Form_ErrorsChanged;
		}

		GC.SuppressFinalize(this);
	}

	private void Form_ErrorsChanged(object? sender, EventArgs e)
	{
		_errorCount = Form!.Errors.Sum(x => x.Value.Count);
		var names = new List<string>();
		foreach (var kvp in Form.Errors)
		{
			if (Form.Fields.FirstOrDefault(x => x.Name == kvp.Key) is FormField<TItem> field)
			{
				names.Add(field.Title);
			}
		}

		_errorFieldNames = string.Join(", ", names);
		InvokeAsync(() => { StateHasChanged(); }).ConfigureAwait(true);
	}

	protected override void OnParametersSet()
	{
		// update state of default buttons
		if (Form != null)
		{
			if (!_formSet)
			{
				_formSet = true;

				// listen for error changes
				Form.ErrorsChanged += Form_ErrorsChanged;
			}
		}
	}

	private async Task OnCancelAsync(MouseEventArgs args)
	{
		if (Form?.Item != null)
		{
			if (Form.ConfirmCancel && Form.Delta.Count > 0)
			{
				await Form.EditItemAsync(Form.Item, FormModes.Cancel).ConfigureAwait(true);
			}
			else
			{
				await Form.ResetChanges();
				await Click.InvokeAsync("Cancel").ConfigureAwait(true);
			}
		}
	}

	private async Task OnDeleteAsync(MouseEventArgs args)
	{
		if (Form?.Item != null)
		{
			await Form.EditItemAsync(Form.Item, FormModes.Delete).ConfigureAwait(true);
		}
	}

	private async Task OnNoAsync(MouseEventArgs args)
	{
		if (Form?.Item != null)
		{
			await Form.EditItemAsync(Form.Item, Form.PreviousMode, false).ConfigureAwait(true);
		}
	}

	private async Task OnSaveAsync(MouseEventArgs args)
	{
		if (Form?.Item != null && Form.DataProvider != null)
		{
			var success = await Form.SaveAsync().ConfigureAwait(true);
			if (!success)
			{
				return;
			}

			await Form.ResetChanges();
		}

		await Click.InvokeAsync("Save").ConfigureAwait(true);
	}

	private async Task OnYesAsync(MouseEventArgs args)
	{
		if (Form?.Item != null)
		{
			if (Form.Mode == FormModes.Delete)
			{
				if (Form.DataProvider != null)
				{
					var success = await Form.DeleteAsync().ConfigureAwait(true);
					if (!success)
					{
						return;
					}

					await Form.ResetChanges();
				}

				await Click.InvokeAsync("Delete").ConfigureAwait(true);
			}
			else if (Form.Mode == FormModes.Cancel)
			{
				await Form.ResetChanges();
				await Click.InvokeAsync("Cancel").ConfigureAwait(true);
			}
		}
	}
}
