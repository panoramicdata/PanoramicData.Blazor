using PanoramicData.Blazor.Models;

namespace PanoramicData.Blazor.Arguments
{
	public class FieldUpdateArgs<TItem> where TItem : class
	{
		public FieldUpdateArgs(FormField<TItem> field, object? oldValue, object newValue)
		{
			Field = field;
			OldValue = oldValue;
			NewValue = newValue;
		}

		public FormField<TItem> Field { get; }

		public object? OldValue { get; }

		public object NewValue { get; set; }
	}
}
