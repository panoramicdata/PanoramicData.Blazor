namespace PanoramicData.Blazor.Models;

/// <summary>
/// Issues the numbers in the default ids of generic types. A static field in a generic type is separate for every
/// closed type, so two forms of different item types would both be <c>pd-form-1</c>; held here, each sequence is
/// shared by all of them and an id is never handed out twice.
/// </summary>
internal static class GenericTypeIds
{
	private static int _lastTreeNodeId;
	private static int _lastFormId;
	private static int _lastFormFieldEditorId;

	/// <summary>Returns the next <see cref="TreeNode{T}"/> id.</summary>
	internal static int NextTreeNodeId() => Interlocked.Increment(ref _lastTreeNodeId);

	/// <summary>Returns the number for the next <see cref="PDForm{TItem}"/> default id.</summary>
	internal static int NextFormId() => Interlocked.Increment(ref _lastFormId);

	/// <summary>Returns the number for the next <see cref="PDFormFieldEditor{TItem}"/> default id.</summary>
	internal static int NextFormFieldEditorId() => Interlocked.Increment(ref _lastFormFieldEditorId);
}
