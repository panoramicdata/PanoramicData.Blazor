namespace PanoramicData.Blazor.Arguments;

public class FieldUpdateArgs<TItem>(FormField<TItem> field, object? oldValue, object? newValue) where TItem : class
{
	public FormField<TItem> Field { get; } = field;

	public object? OldValue { get; } = oldValue;

	public object? NewValue { get; set; } = newValue;
}
