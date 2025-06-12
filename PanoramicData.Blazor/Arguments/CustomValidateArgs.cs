namespace PanoramicData.Blazor.Arguments;

/// <summary>
/// The CustomValidateArgs class holds details on a field to be validated and allows collection
/// of one or more error messages.
/// </summary>
public class CustomValidateArgs<TItem>(FormField<TItem> field, TItem? item) where TItem : class
{
	/// <summary>
	/// Gets the field being validated.
	/// </summary>
	public FormField<TItem> Field { get; } = field;

	/// <summary>
	/// Gets the TItem being validated.
	/// </summary>
	public TItem? Item { get; } = item;

	/// <summary>
	/// Gets one or more error messages keyed by field name, to be added.
	/// </summary>
	public Dictionary<string, string> AddErrorMessages { get; } = [];

	/// <summary>
	/// Gets one or more error messages keyed by field name to be cleared.
	/// </summary>
	public Dictionary<string, string> RemoveErrorMessages { get; } = [];
}
