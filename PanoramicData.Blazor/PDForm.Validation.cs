namespace PanoramicData.Blazor;

/// <summary>
/// Validating the fields of a <see cref="PDForm{TItem}"/> and tracking their errors.
/// </summary>
public partial class PDForm<TItem>
{
	/// <summary>
	/// Raised when validation errors change.
	/// </summary>
	public event System.EventHandler? ErrorsChanged;

	/// <summary>
	/// Gets a dictionary used to track validation errors.
	/// </summary>
	public Dictionary<string, List<string>> Errors { get; } = [];

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
	/// Validates the given field, using the latest changed value for it, and returns the typed value.
	/// </summary>
	/// <param name="field">The field to be validated.</param>
	/// <returns>Value converted to appropriate data type, otherwise null if problems casting.</returns>
	public Task<object?> ValidateFieldAsync(FormField<TItem> field)
		=> ValidateFieldAsync(field, null, null);

	/// <summary>
	/// Validates the given field and returns the typed value.
	/// </summary>
	/// <param name="field">The field to be validated.</param>
	/// <param name="value">The value to be validated, if null then will use the latest changed value for the given field.</param>
	/// <returns>Value converted to appropriate data type, otherwise null if problems casting.</returns>
	public Task<object?> ValidateFieldAsync(FormField<TItem> field, object? value)
		=> ValidateFieldAsync(field, value, null);

	/// <summary>
	/// Validates the given field and returns the typed value.
	/// </summary>
	/// <param name="field">The field to be validated.</param>
	/// <param name="value">The value to be validated, if null then will use the latest changed value for the given field.</param>
	/// <param name="updatedItem">Optional item clone with updates applied, improves performance if supplied.</param>
	/// <returns>Value converted to appropriate data type, otherwise null if problems casting.</returns>
	public async Task<object?> ValidateFieldAsync(FormField<TItem> field, object? value, TItem? updatedItem)
	{
		if (Item == null || field.Field?.GetPropertyMemberInfo() is not PropertyInfo propInfo)
		{
			return null;
		}

		try
		{
			// cast value
			var typedValue = (value ?? GetFieldValue(field, true))?.Cast(propInfo.PropertyType);

			// run standard data annotation validation
			updatedItem = GetItemToValidate(updatedItem);
			ValidateAnnotations(propInfo.Name, typedValue, updatedItem);

			// validate numeric values
			ValidateRange(field, propInfo.Name, typedValue);

			// run custom validation
			await RunCustomValidationAsync(field, updatedItem).ConfigureAwait(true);

			return typedValue;
		}
		catch (Exception ex)
		{
			SetFieldErrors(propInfo.Name, ex.Message);
		}

		return null;
	}

	/// <summary>
	/// Returns the item with updates to validate: the one given, if any, though the form must still be able to
	/// produce one of its own.
	/// </summary>
	private TItem GetItemToValidate(TItem? updatedItem)
	{
		var itemWithUpdates = GetItemWithUpdates()
			?? throw new ArgumentException("Failed to get updated item instance");
		return updatedItem ?? itemWithUpdates;
	}

	private void ValidateAnnotations(string memberName, object? typedValue, TItem updatedItem)
	{
		var results = new List<ValidationResult>();
		var context = new ValidationContext(updatedItem)
		{
			MemberName = memberName
		};
		if (Validator.TryValidateProperty(typedValue, context, results))
		{
			ClearErrors(memberName);
		}
		else
		{
			SetFieldErrors(memberName, [.. results.Where(x => x?.ErrorMessage != null).Select(x => x.ErrorMessage!)]);
		}
	}

	private void ValidateRange(FormField<TItem> field, string memberName, object? typedValue)
	{
		if (field.MaxValue.HasValue && Convert.ToDouble(typedValue, CultureInfo.InvariantCulture) > field.MaxValue.Value)
		{
			SetFieldErrors(memberName, $"Value must be {field.MaxValue.Value} or less.");
		}

		if (field.MinValue.HasValue && Convert.ToDouble(typedValue, CultureInfo.InvariantCulture) < field.MinValue.Value)
		{
			SetFieldErrors(memberName, $"Value must be {field.MinValue.Value} or greater.");
		}
	}

	private async Task RunCustomValidationAsync(FormField<TItem> field, TItem updatedItem)
	{
		var args = new CustomValidateArgs<TItem>(field, updatedItem);
		await CustomValidate.InvokeAsync(args).ConfigureAwait(true);
		foreach (var kvp in args.RemoveErrorMessages)
		{
			RemoveFieldError(kvp.Key, kvp.Value);
		}

		foreach (var kvp in args.AddErrorMessages)
		{
			SetFieldErrors(kvp.Key, kvp.Value);
		}

		if (args.RemoveErrorMessages.Count > 0 && args.AddErrorMessages.Count == 0)
		{
			OnErrorsChanged(System.EventArgs.Empty);
		}
	}

	private void RemoveFieldError(string fieldName, string message)
	{
		var entry = Errors.FirstOrDefault(x => x.Key == fieldName && x.Value.Contains(message));
		if (entry.Key is null)
		{
			return;
		}

		Errors[entry.Key].Remove(message);
		if (Errors[entry.Key].Count == 0)
		{
			Errors.Remove(entry.Key);
		}
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
			Errors.Add(fieldName, []);
		}

		// avoid duplicate messages
		foreach (var message in messages)
		{
			if (!Errors[fieldName].Contains(message))
			{
				Errors[fieldName].Add(message);
			}
		}

		OnErrorsChanged(System.EventArgs.Empty);
	}

	/// <summary>
	/// Remove errors for the given field.
	/// </summary>
	/// <param name="fieldName">Name of the field.</param>
	public void ClearErrors(string fieldName)
	{
		Errors.Remove(fieldName);
		OnErrorsChanged(System.EventArgs.Empty);
	}

	/// <summary>
	/// Removes all errors, telling listeners when there were any.
	/// </summary>
	private void ClearAllErrors()
	{
		if (Errors.Count > 0)
		{
			Errors.Clear();
			OnErrorsChanged(System.EventArgs.Empty);
		}
	}

	/// <summary>
	/// Raises the <see cref="ErrorsChanged"/> event.
	/// </summary>
	/// <param name="e">Event args.</param>
	protected virtual void OnErrorsChanged(System.EventArgs e) => ErrorsChanged?.Invoke(this, e);
}
