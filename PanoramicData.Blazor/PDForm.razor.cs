using PanoramicData.DeepCloner;

namespace PanoramicData.Blazor;

/// <summary>
/// Form component for create/edit/delete workflows with field tracking, validation, and delta updates.
/// </summary>
/// <typeparam name="TItem">Model type edited by the form.</typeparam>
public partial class PDForm<TItem> : IAsyncDisposable where TItem : class
{
	private bool _showHelp;
	private IJSObjectReference? _module;
	private readonly List<PDFormFieldEditor<TItem>> _fieldEditors = [];

	/// <summary>
	/// Raised when a reset is requested.
	/// </summary>
	public event System.EventHandler? ResetRequested;

	[Inject] private IJSRuntime JSRuntime { get; set; } = default!;

	[Inject] private INavigationCancelService NavigationCancelService { get; set; } = default!;

	/// <summary>
	/// Injected log service.
	/// </summary>
	[Inject] protected ILogger<PDForm<TItem>> Logger { get; set; } = new NullLogger<PDForm<TItem>>();

	/// <summary>
	/// Should edit deltas be automatically applied to the model?
	/// </summary>
	[Parameter] public bool AutoApplyDelta { get; set; }

	/// <summary>
	/// Gets or sets the child content that the drop zone wraps.
	/// </summary>
	[Parameter] public RenderFragment? ChildContent { get; set; }

	/// <summary>
	/// CSS classes to be added to the containing DIV element.
	/// </summary>
	[Parameter] public string CssClass { get; set; } = string.Empty;

	/// <summary>
	/// Should the user be prompted to confirm cancel when changes have been made?
	/// </summary>
	[Parameter] public bool ConfirmCancel { get; set; } = true;

	/// <summary>
	/// Should the user be prompted to confirm on page unload when changes have been made?
	/// </summary>
	[Parameter] public bool ConfirmOnUnload { get; set; } = true;

	/// <summary>
	/// Gets or sets the item being created / edited / deleted.
	/// </summary>
	[Parameter] public string Id { get; set; } = $"pd-form-{GenericTypeIds.NextFormId()}";

	/// <summary>
	/// Gets or sets the item being created / edited / deleted.
	/// </summary>
	[Parameter] public TItem? Item { get; set; }

	/// <summary>
	/// Gets or sets the IDataProviderService instance to use to save data.
	/// </summary>
	[Parameter] public IDataProviderService<TItem> DataProvider { get; set; } = null!;

	/// <summary>
	/// Event raised whenever the current item is successfully deleted.
	/// </summary>
	[Parameter] public EventCallback<TItem> Deleted { get; set; }

	/// <summary>
	/// Event raised when the current item has been successfully created.
	/// </summary>
	[Parameter] public EventCallback<TItem> Created { get; set; }

	/// <summary>
	/// Event raised when the current item has been successfully updated.
	/// </summary>
	[Parameter] public EventCallback<FieldUpdateArgs<TItem>> FieldUpdated { get; set; }

	/// <summary>
	/// Event raised when the current item has been successfully updated.
	/// </summary>
	[Parameter] public EventCallback<TItem> Updated { get; set; }

	/// <summary>
	/// Event raised whenever an error occurs.
	/// </summary>
	[Parameter] public EventCallback<string> Error { get; set; }

	/// <summary>
	/// Should the form be hidden after a Save operation?
	/// </summary>
	[Parameter] public bool HideForm { get; set; } = true;

	/// <summary>
	/// Sets the default mode of the form.
	/// </summary>
	[Parameter] public FormModes DefaultMode { get; set; }

	/// <summary>
	/// Sets how help text is displayed.
	/// </summary>
	[Parameter] public HelpTextMode HelpTextMode { get; set; } = HelpTextMode.Toggle;

	/// <summary>
	/// Gets or sets a delegate to be called for each field validated.
	/// </summary>
	[Parameter] public EventCallback<CustomValidateArgs<TItem>> CustomValidate { get; set; }

	/// <summary>
	/// Gets or sets a delegate to be called if an exception occurs.
	/// </summary>
	[Parameter] public EventCallback<Exception> ExceptionHandler { get; set; }

	/// <summary>
	/// Should any errors (i.e mandatory fields) be suppressed until the first edit occurs?
	/// </summary>
	[Parameter] public bool SuppressInitialErrors { get; set; } = true;

	/// <summary>
	/// Gets or sets the current form mode.
	/// </summary>
	public FormModes Mode { get; private set; }

	/// <summary>
	/// Gets a full list of all fields.
	/// </summary>
	public List<FormField<TItem>> Fields { get; } = [];

	/// <summary>
	/// Gets a dictionary used to track uncommitted changes.
	/// </summary>
	public Dictionary<string, object?> Delta { get; } = [];

	/// <summary>
	/// Gets whether changes have been made.
	/// </summary>
	public bool HasChanges
	{
		get
		{
			return Delta.Count > 0;
		}
	}

	/// <summary>
	/// Gets or sets the mode that the form was in before the current mode.
	/// </summary>
	public FormModes PreviousMode { get; private set; }

	/// <summary>
	/// Initializes component state and navigation guard subscriptions.
	/// </summary>
	protected override void OnInitialized()
	{
		Mode = DefaultMode;
		NavigationCancelService.BeforeNavigate += NavigationService_BeforeNavigate;
	}

	/// <summary>
	/// Loads JavaScript resources after first render.
	/// </summary>
	/// <param name="firstRender">True on first render; otherwise false.</param>
	protected override async Task OnAfterRenderAsync(bool firstRender)
	{
		if (firstRender)
		{
			try
			{
				_module = await JSRuntime.InvokeAsync<IJSObjectReference>("import", "./_content/PanoramicData.Blazor/PDForm.razor.js").ConfigureAwait(true);
			}
			catch
			{
				// BC-40 - fast page switching in Server Side blazor can lead to OnAfterRender call after page / objects disposed
			}
		}
	}

	/// <summary>
	/// Adds the given field to the list of available fields.
	/// </summary>
	/// <param name="field">The PDColumn to be added.</param>
	public async Task AddFieldAsync(PDField<TItem> field)
	{
		try
		{
			Fields.Add(new FormField<TItem>
			{
				AutoComplete = field.AutoComplete,
				Id = field.Id,
				Description = field.Description,
				DescriptionFunc = field.DescriptionFunc,
				DisplayOptions = field.DisplayOptions,
				Field = field.Field,
				Group = field.Group,
				Helper = field.Helper,
				ReadOnlyInCreate = field.ReadOnlyInCreate,
				ReadOnlyInEdit = field.ReadOnlyInEdit,
				ShowCopyButton = field.ShowCopyButton,
				ShowInCreate = field.ShowInCreate,
				ShowInDelete = field.ShowInDelete,
				ShowInEdit = field.ShowInEdit,
				EditTemplate = field.EditTemplate,
				Title = field.GetTitle(),
				TitleFunc = field.TitleFunc,
				MaxLength = field.MaxLength,
				MaxValue = field.MaxValue,
				MinValue = field.MinValue,
				Label = field.Label,
				ShowValidationResult = field.ShowValidationResult,
				Options = field.Options,
				IsPassword = field.IsPassword,
				IsSensitive = field.IsSensitive,
				IsTextArea = field.IsTextArea,
				IsImage = field.IsImage,
				TextAreaRows = field.TextAreaRows,
				HelpUrl = field.HelpUrl
			});
			StateHasChanged();
		}
		catch (Exception ex)
		{
			await HandleExceptionAsync(ex).ConfigureAwait(false);
		}
	}

	/// <summary>
	/// Centralized method to process exceptions.
	/// </summary>
	/// <param name="ex">Exception that has been raised.</param>
	public async Task HandleExceptionAsync(Exception ex)
	{
		Logger.LogError(ex, "Exception occurred: {ErrorMessage}", ex.Message);
		await ExceptionHandler.InvokeAsync(ex).ConfigureAwait(true);
	}

	/// <summary>
	/// Sets the current item.
	/// </summary>
	/// <param name="item">The current item to be edited.</param>
	[Obsolete("SetItem is deprecated, please use EditItemAsync instead.")]
	public void SetItem(TItem item) => Item = item;

	/// <summary>
	/// Gets or sets whether help text should be displayed.
	/// </summary>
	public bool ShowHelp
	{
		get { return _showHelp; }
		set
		{
			if (value != _showHelp)
			{
				_showHelp = value;
				StateHasChanged();
			}
		}
	}

	/// <summary>
	/// Reset the current edit changes and errors.
	/// </summary>
	public async Task ResetChanges()
	{
		Delta.Clear();

		OnResetRequested(System.EventArgs.Empty);

		await SetUnloadListenerAsync(false).ConfigureAwait(true);

		ClearAllErrors();
	}

	/// <summary>
	/// Arms or disarms the prompt shown when leaving the page with unsaved changes, if the form asks for one.
	/// </summary>
	private async Task SetUnloadListenerAsync(bool enabled)
	{
		if (ConfirmOnUnload && _module != null)
		{
			await _module.InvokeVoidAsync("setUnloadListener", Id, enabled).ConfigureAwait(true);
		}
	}

	/// <summary>
	/// Sets the current mode of the form, resetting pending changes when entering Create or Edit mode.
	/// </summary>
	/// <param name="mode">The new mode for the form.</param>
	[Obsolete("SetMode is deprecated, please use EditItemAsync instead.")]
	public void SetMode(FormModes mode)
		=> SetMode(mode, true);

	/// <summary>
	/// Sets the current mode of the form.
	/// </summary>
	/// <param name="mode">The new mode for the form.</param>
	/// <param name="resetChanges">When true, pending changes are reset when entering Create or Edit mode.</param>
	[Obsolete("SetMode is deprecated, please use EditItemAsync instead.")]
	public void SetMode(FormModes mode, bool resetChanges)
	{
		PreviousMode = Mode;
		Mode = mode;
		if (resetChanges && (Mode == FormModes.Create || Mode == FormModes.Edit))
		{
#pragma warning disable CS4014 // Because this call is not awaited, execution of the current method continues before the call is completed
			ResetChanges();
#pragma warning restore CS4014 // Because this call is not awaited, execution of the current method continues before the call is completed
		}

		ClearAllErrors();

		foreach (var field in Fields)
		{
			field.SuppressErrors = SuppressInitialErrors;
		}

		StateHasChanged();
		_ = ResetAllEditorCssAsync();
	}

	/// <summary>
	/// Send request to the DataProvider to delete the item.
	/// </summary>
	public async Task<bool> DeleteAsync()
	{
		if (DataProvider != null && Item != null)
		{
			var response = await DataProvider.DeleteAsync(Item, CancellationToken.None).ConfigureAwait(true);
			if (response.Success)
			{
				Mode = HideForm ? FormModes.Hidden : FormModes.Create;
				await Deleted.InvokeAsync(Item).ConfigureAwait(true);
			}
			else
			{
				await Error.InvokeAsync(response.ErrorMessage).ConfigureAwait(true);
				return false;
			}
		}

		return true;
	}

	/// <summary>
	/// Send request to the DataProvider to create or update the item.
	/// </summary>
	public async Task<bool> SaveAsync()
	{
		if (DataProvider == null || Item == null)
		{
			return true;
		}

		// validate all fields - 1 or more errors prevents the save
		var errors = await ValidateFormAsync().ConfigureAwait(true);
		if (errors > 0)
		{
			return false;
		}

		return Mode switch
		{
			FormModes.Create => await CreateItemAsync(Item).ConfigureAwait(true),
			FormModes.Edit => await UpdateItemAsync(Item).ConfigureAwait(true),
			_ => true
		};
	}

	private async Task<bool> CreateItemAsync(TItem item)
	{
		// apply delta to item
		await ApplyDelta(item).ConfigureAwait(true);
		var response = await DataProvider.CreateAsync(item, CancellationToken.None).ConfigureAwait(true);
		if (!response.Success)
		{
			await Error.InvokeAsync(response.ErrorMessage).ConfigureAwait(true);
			return false;
		}

		Mode = HideForm ? FormModes.Hidden : FormModes.Edit;
		await Created.InvokeAsync(item).ConfigureAwait(true);
		return true;
	}

	private async Task<bool> UpdateItemAsync(TItem item)
	{
		var response = await DataProvider.UpdateAsync(item, Delta, CancellationToken.None).ConfigureAwait(true);
		if (!response.Success)
		{
			await Error.InvokeAsync(response.ErrorMessage).ConfigureAwait(true);
			return false;
		}

		// update original item with delta
		await ApplyDelta(item).ConfigureAwait(true);
		Mode = HideForm ? FormModes.Hidden : FormModes.Edit;
		await Updated.InvokeAsync(item).ConfigureAwait(true);
		return true;
	}

	private async Task ApplyDelta(TItem item)
	{
		var itemType = item.GetType();
		foreach (var change in Delta)
		{
			try
			{
				var propInfo = itemType.GetProperty(change.Key);
				propInfo?.SetValue(item, change.Value);
			}
			catch (Exception ex)
			{
				await Error.InvokeAsync($"Error applying delta to {change.Key}: {ex.Message}").ConfigureAwait(true);
			}
		}
	}

	/// <summary>
	/// Raises the <see cref="ResetRequested"/> event.
	/// </summary>
	/// <param name="e">Event args.</param>
	protected virtual void OnResetRequested(System.EventArgs e) => ResetRequested?.Invoke(this, e);

	private void NavigationService_BeforeNavigate(object? sender, BeforeNavigateEventArgs e)
	{
		if (HasChanges && ConfirmOnUnload)
		{
			e.Cancel = true;
		}
	}

	/// <summary>
	/// Sets the current edit item and display mode, resetting any current changes and validating the item
	/// when the mode is Create or Edit.
	/// </summary>
	/// <param name="item">Item to edit.</param>
	/// <param name="mode">Display mode of edit.</param>
	public Task EditItemAsync(TItem? item, FormModes mode)
		=> EditItemAsync(item, mode, true, null);

	/// <summary>
	/// Sets the current edit item and display mode, validating the item when the mode is Create or Edit.
	/// </summary>
	/// <param name="item">Item to edit.</param>
	/// <param name="mode">Display mode of edit.</param>
	/// <param name="resetChanges">Should any current changes be reset?</param>
	public Task EditItemAsync(TItem? item, FormModes mode, bool resetChanges)
		=> EditItemAsync(item, mode, resetChanges, null);

	/// <summary>
	/// Sets the current edit item and display mode, resetting any current changes.
	/// </summary>
	/// <param name="item">Item to edit.</param>
	/// <param name="mode">Display mode of edit.</param>
	/// <param name="validate">Should the item be validated? Null value will lead to Validation being called only when mode is set to Create or Edit.</param>
	public Task EditItemAsync(TItem? item, FormModes mode, bool? validate)
		=> EditItemAsync(item, mode, true, validate);

	/// <summary>
	/// Sets the current edit item and display mode.
	/// </summary>
	/// <param name="item">Item to edit.</param>
	/// <param name="mode">Display mode of edit.</param>
	/// <param name="resetChanges">Should any current changes be reset?</param>
	/// <param name="validate">Should the item be validated? Null value will lead to Validation being called only when mode is set to Create or Edit.</param>
	public async Task EditItemAsync(TItem? item, FormModes mode, bool resetChanges, bool? validate)
	{
		Item = item;
		PreviousMode = Mode;
		Mode = mode;
		var isCreateOrEdit = mode is FormModes.Create or FormModes.Edit;
		if (resetChanges && isCreateOrEdit)
		{
			await ResetChanges();
		}

		ClearAllErrors();

		if (Mode == FormModes.Create)
		{
			foreach (var field in Fields)
			{
				field.SuppressErrors = SuppressInitialErrors;
			}
		}

		if (validate ?? isCreateOrEdit)
		{
			await ValidateFormAsync().ConfigureAwait(true);
		}

		StateHasChanged();
		await ResetAllEditorCssAsync();
	}

	/// <summary>
	/// Registers a field editor instance for coordinated form operations.
	/// </summary>
	/// <param name="editor">Editor to register.</param>
	public void RegisterFieldEditor(PDFormFieldEditor<TItem> editor)
	{
		if (!_fieldEditors.Contains(editor))
		{
			_fieldEditors.Add(editor);
		}
	}

	/// <summary>
	/// Unregisters a field editor instance.
	/// </summary>
	/// <param name="editor">Editor to unregister.</param>
	public void UnregisterFieldEditor(PDFormFieldEditor<TItem> editor)
	{
		_fieldEditors.Remove(editor);
	}

	private async Task ResetAllEditorCssAsync()
	{
		foreach (var editor in _fieldEditors.ToList())
		{
			await editor.ResetEditorCssAsync();
		}
	}

	/// <summary>
	/// Disposes JavaScript resources and unsubscribes navigation handlers.
	/// </summary>
	public async ValueTask DisposeAsync()
	{
		try
		{
			GC.SuppressFinalize(this);
			NavigationCancelService.BeforeNavigate -= NavigationService_BeforeNavigate;
			if (_module != null)
			{
				if (ConfirmOnUnload)
				{
					try
					{
						// The beforeunload listener is shared, module-level state keyed by form id and
						// outlives this component - without this disarm, a dirty form disposed by
						// navigation leaves the "Exit and lose changes?" prompt armed for the whole tab.
						await _module.InvokeVoidAsync("setUnloadListener", Id, false).ConfigureAwait(true);
					}
					catch (JSDisconnectedException)
					{
						// The Blazor circuit has already disconnected. The page and its listener are gone - benign; ignore.
					}
				}

				await _module.DisposeAsync().ConfigureAwait(true);
			}
		}
		catch
		{
			// the module may already be gone with the circuit, and there is nothing left to release
		}
	}
}
