﻿using System.Security.Policy;

namespace PanoramicData.Blazor;

public partial class PDForm<TItem> : IAsyncDisposable where TItem : class
{
	private static int _seq;
	private bool _showHelp;
	private IJSObjectReference? _module;

	public event EventHandler? ErrorsChanged;

	[Inject] private IJSRuntime JSRuntime { get; set; } = default!;

	[Inject] private INavigationCancelService NavigationCancelService { get; set; } = default!;

	/// <summary>
	/// Injected log service.
	/// </summary>
	[Inject] protected ILogger<PDForm<TItem>> Logger { get; set; } = new NullLogger<PDForm<TItem>>();

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
	[Parameter] public string Id { get; set; } = $"pd-form-{++_seq}";

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
	public List<FormField<TItem>> Fields { get; } = new List<FormField<TItem>>();

	/// <summary>
	/// Gets a dictionary used to track uncommitted changes.
	/// </summary>
	public Dictionary<string, object?> Delta { get; } = new Dictionary<string, object?>();

	/// <summary>
	/// Gets a dictionary used to track validation errors.
	/// </summary>
	public Dictionary<string, List<string>> Errors { get; } = new Dictionary<string, List<string>>();

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

	protected override async Task OnInitializedAsync()
	{
		Mode = DefaultMode;
		NavigationCancelService.BeforeNavigate += NavigationService_BeforeNavigate;
		_module = await JSRuntime.InvokeAsync<IJSObjectReference>("import", "./_content/PanoramicData.Blazor/PDForm.razor.js").ConfigureAwait(true);
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
		Logger.LogError(ex, ex.Message);
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
	public void ResetChanges()
	{
		Delta.Clear();
		if (ConfirmOnUnload && _module != null)
		{
			_module.InvokeVoidAsync("setUnloadListener", Id, false);
		}

		if (Errors.Count > 0)
		{
			Errors.Clear();
			OnErrorsChanged(EventArgs.Empty);
		}
	}

	/// <summary>
	/// Sets the current mode of the form.
	/// </summary>
	/// <param name="mode">The new mode for the form.</param>
	[Obsolete("SetMode is deprecated, please use EditItemAsync instead.")]
	public void SetMode(FormModes mode, bool resetChanges = true)
	{
		PreviousMode = Mode;
		Mode = mode;
		if (resetChanges && (Mode == FormModes.Create || Mode == FormModes.Edit))
		{
			ResetChanges();
		}

		if (Errors.Count > 0)
		{
			Errors.Clear();
			OnErrorsChanged(EventArgs.Empty);
		}

		foreach (var field in Fields)
		{
			field.SuppressErrors = SuppressInitialErrors;
		}

		StateHasChanged();
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
		if (DataProvider != null && Item != null)
		{
			// validate all fields
			var errors = await ValidateFormAsync().ConfigureAwait(true);
			if (errors == 0)
			{
				if (Mode == FormModes.Create)
				{
					// apply delta to item
					await ApplyDelta(Item).ConfigureAwait(true);
					var response = await DataProvider.CreateAsync(Item, CancellationToken.None).ConfigureAwait(true);
					if (response.Success)
					{
						Mode = HideForm ? FormModes.Hidden : FormModes.Edit;
						await Created.InvokeAsync(Item).ConfigureAwait(true);
					}
					else
					{
						await Error.InvokeAsync(response.ErrorMessage).ConfigureAwait(true);
						return false;
					}
				}
				else if (Mode == FormModes.Edit)
				{
					var response = await DataProvider.UpdateAsync(Item, Delta, CancellationToken.None).ConfigureAwait(true);
					if (response.Success)
					{
						// update original item with delta
						await ApplyDelta(Item).ConfigureAwait(true);
						Mode = HideForm ? FormModes.Hidden : FormModes.Edit;
						await Updated.InvokeAsync(Item).ConfigureAwait(true);
					}
					else
					{
						await Error.InvokeAsync(response.ErrorMessage).ConfigureAwait(true);
						return false;
					}
				}
			}
			else
			{
				// 1 or more errors
				return false;
			}
		}

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
	/// Attempts to fetch the field with the given name.
	/// </summary>
	/// <param name="name">Name of the field to return.</param>
	/// <returns>A FormField instance if found, otherwise null.</returns>
	public FormField<TItem> GetField(string name) => Fields.First(x => x.Name == name);

	/// <summary>
	/// Attempts to get the requested fields current or original value and cast to the required type.
	/// </summary>
	/// <param name="fieldName">The name of the field whose value is to be fetched.</param>
	/// <param name="updatedValue">Should the current / updated value be returned or the original value?</param>
	/// <returns>The current or original field value cat to the appropriate type.</returns>
	/// <remarks>Use this method for Struct types only, use GetFieldStringValue() for String fields.</remarks>

	public object? GetFieldValue(string fieldName)
	{
		return GetFieldValue(fieldName, true);
	}

	public object? GetFieldValue(string fieldName, bool updatedValue)
	{
		return GetFieldValue(GetField(fieldName), updatedValue);
	}


	/// <summary>
	/// Attempts to get the requested fields current or original value and cast to the required type.
	/// </summary>
	/// <param name="field">The field whose value is to be fetched.</param>
	/// <param name="updatedValue">Should the current / updated value be returned or the original value?</param>
	/// <returns>The current or original field value cat to the appropriate type.</returns>
	/// <remarks>Use this method for Struct types only, use GetFieldStringValue() for String fields.</remarks>
	///

	public object? GetFieldValue(FormField<TItem> field)
		=> GetFieldValue(field, true);
	public object? GetFieldValue(FormField<TItem> field, bool updatedValue)
	{
		// point to relevant TItem instance
		if (Item is null)
		{
			return null;
		}

		// if original value required simply return
		object? value;
		var memberInfo = field.Field?.GetPropertyMemberInfo();
		if (memberInfo is PropertyInfo propInfo)
		{
			if (updatedValue && Delta.ContainsKey(memberInfo.Name))
			{
				value = Delta[memberInfo.Name];
			}
			else
			{
				value = propInfo.GetValue(Item);
			}
		}
		else
		{
			value = field.CompiledFieldFunc?.Invoke(Item);
		}

		return value;
	}

	/// <summary>
	/// Attempts to get the requested fields current or original value and cast to the required type.
	/// </summary>
	/// <param name="fieldName">The name of the field whose value is to be fetched.</param>
	/// <param name="updatedValue">Should the current / updated value be returned or the original value?</param>
	/// <returns>The current or original field value cat to the appropriate type.</returns>
	/// <remarks>Use this method for Struct types only, use GetFieldStringValue() for String fields.</remarks>

	public T GetFieldValue<T>(string fieldName) where T: struct
		=> GetFieldValue<T>(fieldName, true);

	public T GetFieldValue<T>(string fieldName, bool updatedValue) where T : struct
	{
		var field = GetField(fieldName);
		return GetFieldValue<T>(field, updatedValue);
	}

	/// <summary>
	/// Attempts to get the requested fields current or original value and cast to the required type.
	/// </summary>
	/// <param name="field">The field whose value is to be fetched.</param>
	/// <param name="updatedValue">Should the current / updated value be returned or the original value?</param>
	/// <returns>The current or original field value cat to the appropriate type.</returns>
	/// <remarks>Use this method for Struct types only, use GetFieldStringValue() for String fields.</remarks>

	public T GetFieldValue<T>(FormField<TItem> field) where T : struct
		=> GetFieldValue<T>(field, true);
	public T GetFieldValue<T>(FormField<TItem> field, bool updatedValue) where T : struct
	{
		// point to relevant TItem instance
		if (Item is null || field is null)
		{
			return default;
		}

		object? value = GetFieldValue(field, updatedValue);
		if (value is null)
		{
			return default;
		}

		if (value is T t)
		{
			return t;
		}

		try
		{
			return (T)Convert.ChangeType(value, typeof(T), CultureInfo.InvariantCulture);
		}
		catch
		{
			return default;
		}
	}

	/// <summary>
	/// Attempts to get the requested fields current or original value and cast to the required type.
	/// </summary>
	/// <param name="fieldName">The name of the field whose value is to be fetched.</param>
	/// <param name="updatedValue">Should the current / updated value be returned or the original value?</param>
	/// <returns>The current or original field value cat to the appropriate type.</returns>
	/// <remarks>Use this method for String fields only, use GetFieldValue<T>() for Struct values.</remarks>

	public string GetFieldStringValue(string fieldName)
		=> GetFieldStringValue(fieldName, true);

	public string GetFieldStringValue(string fieldName, bool updatedValue)
	{
		var field = GetField(fieldName);
		return field is null ? string.Empty : GetFieldStringValue(field, updatedValue);
	}

	public string GetFieldStringValue(IEnumerable<FormField<TItem>> fields)
		=> GetFieldStringValue(fields, true);

	public string GetFieldStringValue(IEnumerable<FormField<TItem>> fields, bool updatedValue)
	{
		var sb = new StringBuilder();
		foreach (var field in fields)
		{
			if (sb.Length > 0)
			{
				sb.Append('\t');
			}

			sb.Append(GetFieldStringValue(field, updatedValue));
		}

		return sb.ToString();
	}

	/// <summary>
	/// Attempts to get the requested fields current or original value and cast to the required type.
	/// </summary>
	/// <param name="field">The field whose value is to be fetched.</param>
	/// <param name="updatedValue">Should the current / updated value be returned or the original value?</param>
	/// <returns>The current or original field value cat to the appropriate type.</returns>
	/// <remarks>Use this method for String fields only, use GetFieldValue<T>() for Struct values.</remarks>

	public string GetFieldStringValue(FormField<TItem> field)
		=> GetFieldStringValue(field, true);

	public string GetFieldStringValue(FormField<TItem> field, bool updatedValue)
	{
		// point to relevant TItem instance
		if (Item is null)
		{
			return string.Empty;
		}

		// if original value required simply return
		object? value;
		var memberInfo = field.Field?.GetPropertyMemberInfo();
		if (memberInfo is PropertyInfo propInfo)
		{
			if (updatedValue && Delta.ContainsKey(memberInfo.Name))
			{
				value = Delta[memberInfo.Name];
			}
			else
			{
				value = propInfo.GetValue(Item);
			}
		}
		else
		{
			value = field.CompiledFieldFunc?.Invoke(Item);
		}

		if (value is null)
		{
			return string.Empty;
		}

		if (value is DateTimeOffset dto)
		{
			// return simple date time string
			return dto.DateTime.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture);
		}

		if (value is DateTime dt)
		{
			// return date time string
			return dt.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture);
		}

		return value.ToString() ?? string.Empty;
	}

	/// <summary>
	/// Updates the current value for the given field.
	/// </summary>
	/// <param name="field">The field to be updated.</param>
	/// <param name="value">The new value for the field.</param>
	/// <remarks>If valid, the new value is applied direct to the Item when in Create mode,
	/// otherwise tracked as a delta when in Edit mode.</remarks>
	public async Task SetFieldValueAsync(FormField<TItem> field, object? value)
	{
		var previousChanges = Delta.Count;
		if (Item != null && field.Field != null)
		{
			// stop suppressing errors after editing
			field.SuppressErrors = false;

			var memberInfo = field.Field.GetPropertyMemberInfo();
			if (memberInfo != null && memberInfo is PropertyInfo propInfo)
			{
				// add / replace value on delta object
				object? typedValue = value is null ? null
					: (propInfo.PropertyType == value.GetType() ? value : value.Cast(propInfo.PropertyType));

				// notify application of change - and allow for override
				var args = new FieldUpdateArgs<TItem>(field, GetFieldValue(field, false), typedValue);
				await FieldUpdated.InvokeAsync(args).ConfigureAwait(true);
				typedValue = args.NewValue;

				// update delta
				Delta[memberInfo.Name] = typedValue;

				// set on unload flag
				if (previousChanges == 0 && Delta.Count > 0 && ConfirmOnUnload && _module != null)
				{
					await _module.InvokeVoidAsync("setUnloadListener", Id, true).ConfigureAwait(true);
				}

				// validate field
				await ValidateFieldAsync(field, value).ConfigureAwait(true);


				StateHasChanged();
			}
		}
	}

	/// <summary>
	/// Creates and returns a clone of the Item under edit with the current changes applied.
	/// </summary>
	/// <returns>A new TItem instance with changes applied.</returns>
	public TItem? GetItemWithUpdates()
	{
		if (Item is null)
		{
			return null;
		}

		var json = System.Text.Json.JsonSerializer.Serialize(Item);
		var clone = System.Text.Json.JsonSerializer.Deserialize<TItem>(json);
		if (clone != null)
		{
			foreach (var kvp in Delta)
			{
				var propInfo = clone.GetType().GetProperty(kvp.Key);
				propInfo?.SetValue(clone, kvp.Value);
			}
		}

		return clone;
	}

	/// <summary>
	/// Validate all form fields.
	/// </summary>
	/// <returns>Count of all validation errors identified.</returns>
	public async Task<int> ValidateFormAsync()
	{
		Errors.Clear();
		var updatedItem = GetItemWithUpdates();
		foreach (var field in Fields)
		{
			if ((Mode == FormModes.Create && field.ShowInCreate(updatedItem)) ||
				(Mode == FormModes.Edit && field.ShowInEdit(updatedItem)))
			{
				await ValidateFieldAsync(field, null, updatedItem).ConfigureAwait(true);
			}
		}

		return Errors.Count;
	}

	/// <summary>
	/// Validates the given field and returns the typed value.
	/// </summary>
	/// <param name="field">The field to be validated.</param>
	/// <param name="value">The value to be validated, if omitted then will use the latest changed value for the given field.</param>
	/// <param name="updatedItem">Optional item clone with updates applied, improves performance if supplied.</param>
	/// <returns>Value converted to appropriate data type, otherwise null if problems casting.</returns>
	public async Task<object?> ValidateFieldAsync(FormField<TItem> field, object? value = null, TItem? updatedItem = null)
	{
		if (Item != null && field.Field != null)
		{
			var memberInfo = field.Field.GetPropertyMemberInfo();
			if (memberInfo != null && memberInfo is PropertyInfo propInfo)
			{
				try
				{
					// cast value
					object? typedValue = (value ?? GetFieldValue(field, true))?.Cast(propInfo.PropertyType);

					// run standard data annotation validation
					var results = new List<ValidationResult>();
					var itemWithUpdates = GetItemWithUpdates()
						?? throw new ArgumentException("Failed to get updated item instance");

					var context = new ValidationContext(updatedItem ??= itemWithUpdates)
					{
						MemberName = memberInfo.Name
					};
					if (Validator.TryValidateProperty(typedValue, context, results))
					{
						ClearErrors(memberInfo.Name);
					}
					else
					{
						SetFieldErrors(memberInfo.Name, results.Where(x => x?.ErrorMessage != null).Select(x => x.ErrorMessage!).ToArray());
					}

					// validate numeric values
					if (field.MaxValue.HasValue && Convert.ToDouble(typedValue, CultureInfo.InvariantCulture) > field.MaxValue.Value)
					{
						SetFieldErrors(memberInfo.Name, $"Value must be {field.MaxValue.Value} or less.");
					}

					if (field.MinValue.HasValue && Convert.ToDouble(typedValue, CultureInfo.InvariantCulture) < field.MinValue.Value)
					{
						SetFieldErrors(memberInfo.Name, $"Value must be {field.MinValue.Value} or greater.");
					}

					// run custom validation
					if (Item != null)
					{
						var args = new CustomValidateArgs<TItem>(field, updatedItem ??= GetItemWithUpdates());
						await CustomValidate.InvokeAsync(args).ConfigureAwait(true);
						foreach (var kvp in args.RemoveErrorMessages)
						{
							var entry = Errors.FirstOrDefault(x => x.Key == kvp.Key && x.Value.Contains(kvp.Value));
							if (entry.Key != null)
							{
								Errors[entry.Key].Remove(kvp.Value);
								if (Errors[entry.Key].Count == 0)
								{
									Errors.Remove(entry.Key);
								}
							}
						}

						foreach (var kvp in args.AddErrorMessages)
						{
							SetFieldErrors(kvp.Key, kvp.Value);
						}

						if (args.RemoveErrorMessages.Count > 0 && args.AddErrorMessages.Count == 0)
						{
							OnErrorsChanged(EventArgs.Empty);
						}
					}

					return typedValue;
				}
				catch (Exception ex)
				{
					SetFieldErrors(memberInfo.Name, ex.Message);
				}
			}
		}

		return null;
	}

	/// <summary>
	/// Gets whether the form is currently valid.
	/// </summary>
	public bool IsValid() => Errors.Count == 0;

	/// <summary>
	/// Adds one or more error messages for the given field.
	/// </summary>
	/// <param name="fieldName">Name of the field being validated.</param>
	/// <param name="messages">One or more error messages.</param>
	public void SetFieldErrors(string fieldName, params string[] messages)
	{
		if (!Errors.ContainsKey(fieldName))
		{
			Errors.Add(fieldName, new List<string>());
		}
		// avoid duplicate messages
		foreach (var message in messages)
		{
			if (!Errors[fieldName].Contains(message))
			{
				Errors[fieldName].Add(message);
			}
		}

		OnErrorsChanged(EventArgs.Empty);
	}

	/// <summary>
	/// Remove errors for the given field.
	/// </summary>
	/// <param name="fieldName">Name of the field.</param>
	public void ClearErrors(string fieldName)
	{
		Errors.Remove(fieldName);
		OnErrorsChanged(EventArgs.Empty);
	}

	protected virtual void OnErrorsChanged(EventArgs e) => ErrorsChanged?.Invoke(this, e);

	private void NavigationService_BeforeNavigate(object? sender, BeforeNavigateEventArgs e)
	{
		if (HasChanges && ConfirmOnUnload)
		{
			e.Cancel = true;
		}
	}

	/// <summary>
	/// Sets the current edit item and display mode.
	/// </summary>
	/// <param name="item">Item to edit.</param>
	/// <param name="mode">Display mode of edit.</param>
	/// <param name="resetChanges">Should any current changes be reset?</param>
	/// <param name="validate">Should the item be validated? Null value will lead to Validation being called only when mode is set to Create.</param>
	public async Task EditItemAsync(TItem? item, FormModes mode, bool resetChanges = true, bool? validate = null)
	{
		Item = item;
		PreviousMode = Mode;
		Mode = mode;
		if (resetChanges && (Mode == FormModes.Create || Mode == FormModes.Edit))
		{
			ResetChanges();
		}

		if (Errors.Count > 0)
		{
			Errors.Clear();
			OnErrorsChanged(EventArgs.Empty);
		}

		foreach (var field in Fields)
		{
			field.SuppressErrors = SuppressInitialErrors;
		}

		validate ??= mode == FormModes.Create;

		if (validate == true)
		{
			await ValidateFormAsync().ConfigureAwait(true);
		}

		StateHasChanged();
	}

	public async ValueTask DisposeAsync()
	{
		try
		{
			GC.SuppressFinalize(this);
			NavigationCancelService.BeforeNavigate -= NavigationService_BeforeNavigate;
			if (_module != null)
			{
				await _module.DisposeAsync().ConfigureAwait(true);
			}
		}
		catch
		{
		}
	}
}
