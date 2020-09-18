using System.Collections.Generic;

namespace PanoramicData.Blazor
{
	/// <summary>
	/// The CustomValidateArgs class holds details on a field to be validated and allows collection
	/// of one or more error messages.
	/// </summary>
	public class CustomValidateArgs<TItem> where TItem : class
	{
		public CustomValidateArgs(FormField<TItem> field, TItem? item)
		{
			Field = field;
			Item = item;

		}

		/// <summary>
		/// Gets the field being validated.
		/// </summary>
		public FormField<TItem> Field { get; }

		/// <summary>
		/// Gets the TItem being validated.
		/// </summary>
		public TItem? Item { get; }

		/// <summary>
		/// Gets one or more error messages keyed by field name, to be added.
		/// </summary>
		public Dictionary<string, string> AddErrorMessages { get; } = new Dictionary<string, string>();

		/// <summary>
		/// Gets one or more error messages keyed by field name to be cleared.
		/// </summary>
		public Dictionary<string, string> RemoveErrorMessages { get; } = new Dictionary<string, string>();
	}
}
