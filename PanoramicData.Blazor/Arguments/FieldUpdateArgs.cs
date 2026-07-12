namespace PanoramicData.Blazor.Arguments;

/// <summary>
/// Provides arguments raised when a form field value is updated, carrying both the previous and new values.
/// </summary>
/// <typeparam name="TItem">The type of the model that owns the field.</typeparam>
/// <param name="field">The form field definition that was updated.</param>
/// <param name="oldValue">The field's value before the update.</param>
/// <param name="newValue">The field's new value after the update.</param>
public class FieldUpdateArgs<TItem>(FormField<TItem> field, object? oldValue, object? newValue) where TItem : class
{
	/// <summary>
	/// Gets the form field definition that was updated.
	/// </summary>
	public FormField<TItem> Field { get; } = field;

	/// <summary>
	/// Gets the field value before the update.
	/// </summary>
	public object? OldValue { get; } = oldValue;

	/// <summary>
	/// Gets or sets the field value after the update. Handlers may replace this value to override what is committed.
	/// </summary>
	public object? NewValue { get; set; } = newValue;
}
