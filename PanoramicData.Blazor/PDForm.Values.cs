using PanoramicData.DeepCloner;

namespace PanoramicData.Blazor;

/// <summary>
/// Reading and changing the values of the fields of a <see cref="PDForm{TItem}"/>.
/// </summary>
public partial class PDForm<TItem>
{
	/// <summary>
	/// Attempts to fetch the field with the given name.
	/// </summary>
	/// <param name="name">Name of the field to return.</param>
	/// <returns>A FormField instance if found, otherwise null.</returns>
	/// <remarks>
	/// The return type is not annotated as nullable, so that existing callers compiled with nullable warnings
	/// treated as errors are not broken; check the result for null when the name may be unknown.
	/// </remarks>
	public FormField<TItem> GetField(string name) => Fields.FirstOrDefault(x => x.Name == name)!;

	/// <summary>
	/// Attempts to get the requested fields current or original value and cast to the required type.
	/// </summary>
	/// <param name="fieldName">The name of the field whose value is to be fetched.</param>
	/// <returns>The current or original field value cat to the appropriate type.</returns>
	/// <remarks>Use this method for Struct types only, use GetFieldStringValue() for String fields.</remarks>
	public object? GetFieldValue(string fieldName)
	{
		return GetFieldValue(fieldName, true);
	}

	/// <summary>
	/// Attempts to get a field value by name.
	/// </summary>
	/// <param name="fieldName">Field name.</param>
	/// <param name="updatedValue">True to prefer current delta value; false for original value.</param>
	/// <returns>Field value or null.</returns>
	public object? GetFieldValue(string fieldName, bool updatedValue)
	{
		var field = GetField(fieldName);
		return field is null ? null : GetFieldValue(field, updatedValue);
	}

	/// <summary>
	/// Attempts to get the requested fields current or original value and cast to the required type.
	/// </summary>
	/// <param name="field">The field whose value is to be fetched.</param>
	/// <returns>The current or original field value cat to the appropriate type.</returns>
	/// <remarks>Use this method for Struct types only, use GetFieldStringValue() for String fields.</remarks>
	public object? GetFieldValue(FormField<TItem> field)
		=> GetFieldValue(field, true);

	/// <summary>
	/// Attempts to get a field value from a field definition.
	/// </summary>
	/// <param name="field">Field definition.</param>
	/// <param name="updatedValue">True to prefer current delta value; false for original value.</param>
	/// <returns>Field value or null.</returns>
	public object? GetFieldValue(FormField<TItem> field, bool updatedValue)
	{
		// point to relevant TItem instance
		if (Item is null)
		{
			return null;
		}

		if (field.Field?.GetPropertyMemberInfo() is not PropertyInfo propInfo)
		{
			return field.CompiledFieldFunc?.Invoke(Item);
		}

		// if original value required simply return
		return updatedValue && Delta.TryGetValue(propInfo.Name, out var value)
			? value
			: propInfo.GetValue(Item);
	}

	/// <summary>
	/// Attempts to get the requested fields current or original value and cast to the required type.
	/// </summary>
	/// <param name="fieldName">The name of the field whose value is to be fetched.</param>
	/// <returns>The current or original field value cat to the appropriate type.</returns>
	/// <remarks>Use this method for Struct types only, use GetFieldStringValue() for String fields.</remarks>
	public T GetFieldValue<T>(string fieldName) where T : struct
		=> GetFieldValue<T>(fieldName, true);

	/// <summary>
	/// Attempts to get and convert a field value by name.
	/// </summary>
	/// <typeparam name="T">Target value type.</typeparam>
	/// <param name="fieldName">Field name.</param>
	/// <param name="updatedValue">True to prefer current delta value; false for original value.</param>
	/// <returns>Converted value, or default when unavailable.</returns>
	public T GetFieldValue<T>(string fieldName, bool updatedValue) where T : struct
	{
		var field = GetField(fieldName);
		return GetFieldValue<T>(field, updatedValue);
	}

	/// <summary>
	/// Attempts to get the requested fields current or original value and cast to the required type.
	/// </summary>
	/// <param name="field">The field whose value is to be fetched.</param>
	/// <returns>The current or original field value cat to the appropriate type.</returns>
	/// <remarks>Use this method for Struct types only, use GetFieldStringValue() for String fields.</remarks>
	public T GetFieldValue<T>(FormField<TItem> field) where T : struct
		=> GetFieldValue<T>(field, true);

	/// <summary>
	/// Attempts to get and convert a field value from a field definition.
	/// </summary>
	/// <typeparam name="T">Target value type.</typeparam>
	/// <param name="field">Field definition.</param>
	/// <param name="updatedValue">True to prefer current delta value; false for original value.</param>
	/// <returns>Converted value, or default when unavailable.</returns>
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
	/// <returns>The current or original field value cat to the appropriate type.</returns>
	/// <remarks>Use this method for String fields only, use GetFieldValue&lt;T&gt;() for Struct values.</remarks>
	public string GetFieldStringValue(string fieldName)
		=> GetFieldStringValue(fieldName, true);

	/// <summary>
	/// Attempts to get a string field value by name.
	/// </summary>
	/// <param name="fieldName">Field name.</param>
	/// <param name="updatedValue">True to prefer current delta value; false for original value.</param>
	/// <returns>String value.</returns>
	public string GetFieldStringValue(string fieldName, bool updatedValue)
	{
		var field = GetField(fieldName);
		return field is null ? string.Empty : GetFieldStringValue(field, updatedValue);
	}

	/// <summary>
	/// Gets tab-delimited string values for the supplied fields.
	/// </summary>
	/// <param name="fields">Fields to read.</param>
	/// <returns>Tab-delimited string values.</returns>
	public string GetFieldStringValue(IEnumerable<FormField<TItem>> fields)
		=> GetFieldStringValue(fields, true);

	/// <summary>
	/// Gets tab-delimited string values for the supplied fields.
	/// </summary>
	/// <param name="fields">Fields to read.</param>
	/// <param name="updatedValue">True to prefer current delta values; false for original values.</param>
	/// <returns>Tab-delimited string values.</returns>
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
	/// <returns>The current or original field value cat to the appropriate type.</returns>
	/// <remarks>Use this method for String fields only, use GetFieldValue&lt;T&gt;() for Struct values.</remarks>
	public string GetFieldStringValue(FormField<TItem> field)
		=> GetFieldStringValue(field, true);

	/// <summary>
	/// Attempts to get the requested fields current or original value and cast to the required type.
	/// </summary>
	/// <param name="field">The field whose value is to be fetched.</param>
	/// <param name="updatedValue">Should the current / updated value be returned or the original value?</param>
	/// <returns>The current or original field value cat to the appropriate type.</returns>
	/// <remarks>Use this method for String fields only, use GetFieldValue&lt;T&gt;() for Struct values.</remarks>
	public string GetFieldStringValue(FormField<TItem> field, bool updatedValue) => GetFieldValue(field, updatedValue) switch
	{
		null => string.Empty,

		// return simple date time string
		DateTimeOffset dto => dto.DateTime.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture),
		DateTime dt => dt.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture),
		var value => value.ToString() ?? string.Empty
	};

	/// <summary>
	/// Updates the value for a given field, manages the Delta dictionary, and notifies listeners, including the
	/// field itself (currently only for Monaco).
	/// </summary>
	/// <param name="field">The field to be updated.</param>
	/// <param name="value">The new value for the field.</param>
	/// <remarks>If valid, the new value is applied direct to the Item when in Create mode,
	/// otherwise tracked as a delta when in Edit mode.</remarks>
	public Task SetFieldValueAsync(FormField<TItem> field, object? value)
		=> SetFieldValueAsync(field, value, true);

	/// <summary>
	/// Updates the value for a given field, manages the Delta dictionary, and notifies listeners.
	/// </summary>
	/// <param name="field">The field to be updated.</param>
	/// <param name="value">The new value for the field.</param>
	/// <param name="notifyField">Whether to notify the field (currently only for Monaco) of the value change.</param>
	/// <remarks>If valid, the new value is applied direct to the Item when in Create mode,
	/// otherwise tracked as a delta when in Edit mode.</remarks>
	public async Task SetFieldValueAsync(FormField<TItem> field, object? value, bool notifyField)
	{
		// Exit if no item or field is set
		if (Item == null || field.Field == null)
		{
			return;
		}

		// Stop suppressing errors after editing
		field.SuppressErrors = false;

		// Get property info for the field
		if (field.Field.GetPropertyMemberInfo() is not PropertyInfo propInfo)
		{
			return;
		}

		// Get the original value from the item
		var originalValue = propInfo.GetValue(Item);

		// Get the current value (from Delta if present, else from the item)
		var currentValue = Delta.TryGetValue(propInfo.Name, out var deltaValue)
			? deltaValue
			: originalValue;

		// If the value hasn't changed, do nothing
		if (ValuesEqual(currentValue, value))
		{
			return;
		}

		// Check if the new value is the same as the original (revert)
		await UpdateDeltaAsync(propInfo, value, ValuesEqual(originalValue, value)).ConfigureAwait(true);

		await OnFieldValueChangedAsync(field, currentValue, value, notifyField).ConfigureAwait(true);

		// If in Edit mode and auto-apply is enabled, apply the delta to the item
		if (Mode == FormModes.Edit && AutoApplyDelta)
		{
			await ApplyDelta(Item).ConfigureAwait(true);
		}

		// Trigger UI update
		StateHasChanged();
	}

	/// <summary>
	/// Tells listeners of a field's new value, and validates it.
	/// </summary>
	private async Task OnFieldValueChangedAsync(FormField<TItem> field, object? previousValue, object? value, bool notifyField)
	{
		// Notify listeners of the field update
		var args = new FieldUpdateArgs<TItem>(field, previousValue, value);
		await FieldUpdated.InvokeAsync(args).ConfigureAwait(true);

		// Validate the field
		await ValidateFieldAsync(field, value).ConfigureAwait(true);

		// Notify the field of the value change
		if (notifyField)
		{
			field.OnValueChanged(value);
		}
	}

	/// <summary>
	/// Records a field's new value as a change, or forgets the change when the value is back to the original,
	/// arming or disarming the unload prompt as the form gains its first change or loses its last.
	/// </summary>
	private async Task UpdateDeltaAsync(PropertyInfo propInfo, object? value, bool revertedToOriginal)
	{
		if (revertedToOriginal)
		{
			// Remove from Delta if present - if no more changes, update unload listener
			if (Delta.Remove(propInfo.Name) && Delta.Count == 0)
			{
				await SetUnloadListenerAsync(false).ConfigureAwait(true);
			}

			return;
		}

		// Convert value to the correct type if needed and add/update Delta
		var wasUnchanged = Delta.Count == 0;
		Delta[propInfo.Name] = value is null || propInfo.PropertyType == value.GetType()
			? value
			: value.Cast(propInfo.PropertyType);

		// If this is the first change, update unload listener
		if (wasUnchanged)
		{
			await SetUnloadListenerAsync(true).ConfigureAwait(true);
		}
	}

	/// <summary>
	/// Compares two values, normalizing line endings for strings.
	/// </summary>
	private static bool ValuesEqual(object? a, object? b)
	{
		if (a is string sa && b is string sb)
		{
			return NormalizeLineEndings(sa) == NormalizeLineEndings(sb);
		}

		return Equals(a, b);
	}

	// Normalize all CRLF and CR to LF, then back to CRLF for consistency
	private static string NormalizeLineEndings(string text)
		=> text.Replace("\r\n", "\n").Replace("\r", "\n").Replace("\n", Environment.NewLine);

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

		try
		{
			var clone = Item.DeepClone();
			foreach (var kvp in Delta)
			{
				var propInfo = clone.GetType().GetProperty(kvp.Key);
				propInfo?.SetValue(clone, kvp.Value);
			}

			return clone;
		}
		catch
		{
			// an item that cannot be cloned, or a change the clone will not take, leaves no item with updates
			return null;
		}
	}
}
