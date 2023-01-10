namespace PanoramicData.Blazor;

public partial class PDFormFooter<TItem> : IDisposable where TItem : class
{
	private bool _formSet;

	/// <summary>
	/// Form the component belongs to.
	/// </summary>
	[CascadingParameter] public PDForm<TItem>? Form { get; set; }

	/// <summary>
	/// Event raised whenever the user clicks on a button.
	/// </summary>
	[Parameter] public EventCallback<string> Click { get; set; }

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
	/// Should the Delete button be shown (only applicable when in Edit mode)?
	/// </summary>
	[Parameter] public bool ShowDelete { get; set; } = true;

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
	[Parameter] public string SaveButtonIconCssClass { get; set; } = "fas fa-save";

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
	[Parameter] public string CancelButtonIconCssClass { get; set; } = "fas fa-times";

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
	[Parameter] public string DeleteButtonIconCssClass { get; set; } = "fas fa-trash-alt";

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
	[Parameter] public string YesButtonIconCssClass { get; set; } = "fas fa-check";

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
	[Parameter] public string NoButtonIconCssClass { get; set; } = "fas fa-times";

	/// <summary>
	/// Gets or sets the buttons displayed in the form footer.
	/// </summary>
	public List<ToolbarItem> Buttons { get; set; } = new List<ToolbarItem>();

	private void SetVisibility(ToolbarItem? item, bool shown)
	{
		if (item != null)
		{
			item.IsVisible = shown;
		}
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

				// create default buttons
				Buttons.Add(new ToolbarButton { Key = "Yes", Text = YesButtonText, CssClass = YesButtonCssClass, IconCssClass = YesButtonIconCssClass, ShiftRight = true, Size = Size });
				Buttons.Add(new ToolbarButton { Key = "No", Text = NoButtonText, CssClass = NoButtonCssClass, IconCssClass = NoButtonIconCssClass, Size = Size });
				Buttons.Add(new ToolbarButton { Key = "Delete", Text = DeleteButtonText, CssClass = DeleteButtonCssClass, IconCssClass = DeleteButtonIconCssClass, Size = Size });
				Buttons.Add(new ToolbarButton { Key = "Save", Text = SaveButtonText, CssClass = SaveButtonCssClass, IconCssClass = SaveButtonIconCssClass, ShiftRight = true, Size = Size });
				Buttons.Add(new ToolbarButton { Key = "Cancel", Text = CancelButtonText, CssClass = CancelButtonCssClass, IconCssClass = CancelButtonIconCssClass, Size = Size });
			}

			SetVisibility(Buttons.Find(x => x.Key == "Yes"), Form.Mode == FormModes.Delete || Form.Mode == FormModes.Cancel);
			SetVisibility(Buttons.Find(x => x.Key == "No"), Form.Mode == FormModes.Delete || Form.Mode == FormModes.Cancel);
			SetVisibility(Buttons.Find(x => x.Key == "Delete"), ShowDelete && Form.Mode == FormModes.Edit);
			SetVisibility(Buttons.Find(x => x.Key == "Save"), ShowSave && (Form.Mode == FormModes.Create || Form.Mode == FormModes.Edit));
			SetVisibility(Buttons.Find(x => x.Key == "Cancel"), ShowCancel && (Form.Mode == FormModes.Create || Form.Mode == FormModes.Edit));
		}
	}

	private void Form_ErrorsChanged(object? sender, EventArgs e)
	{
		InvokeAsync(() =>
		{
			var saveButton = Buttons.Find(x => x.Key == "Save");
			if (saveButton != null)
			{
				var isValid = Form!.Errors.Count == 0;
				saveButton.IsEnabled = isValid;
				StateHasChanged();
			}
		}).ConfigureAwait(true);
	}

	private async Task OnButtonClick(KeyedEventArgs<MouseEventArgs> args)
	{
		var key = args.Key;

		if (Form?.Item != null)
		{
			if (key == "Delete")
			{
				await Form.EditItemAsync(Form.Item, FormModes.Delete).ConfigureAwait(true);
			}
			else if (key == "Save" && Form.DataProvider != null)
			{
				var success = await Form.SaveAsync().ConfigureAwait(true);
				if (!success)
				{
					return;
				}
				Form.ResetChanges();
			}
			else if (key == "Cancel" && Form.ConfirmCancel && Form.Delta.Count > 0)
			{
				await Form.EditItemAsync(Form.Item, FormModes.Cancel).ConfigureAwait(true);
			}
			else if (key == "Yes" && Form.Mode == FormModes.Delete && Form.DataProvider != null)
			{
				var success = await Form.DeleteAsync().ConfigureAwait(true);
				if (!success)
				{
					return;
				}
				Form.ResetChanges();
			}
			else if (key == "Yes" && Form.Mode == FormModes.Cancel)
			{
				await Click.InvokeAsync("Cancel").ConfigureAwait(true);
				Form.ResetChanges();
			}
			//else if (key == "No" && Form.Mode == FormModes.Delete)
			else if (key == "No")
			{
				await Form.EditItemAsync(Form.Item, Form.PreviousMode, false).ConfigureAwait(true);
			}
		}

		if (!(key == "Cancel" && Form?.ConfirmCancel == true && Form.Delta.Count > 0))
		{
			await Click.InvokeAsync(key).ConfigureAwait(true);
		}
	}

	public void Dispose()
	{
		if (Form != null)
		{
			Form.ErrorsChanged -= Form_ErrorsChanged;
		}
	}
}
