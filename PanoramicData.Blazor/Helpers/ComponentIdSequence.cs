namespace PanoramicData.Blazor.Helpers;

/// <summary>
/// Supplies the numbers used to build default element ids for generic components.
/// </summary>
/// <remarks>
/// A static field declared in a generic type is separate for every closed type, so two components
/// of different item types (for example <c>PDTable&lt;Customer&gt;</c> and <c>PDTable&lt;Order&gt;</c>)
/// would otherwise both produce the same default id. Drawing from this single, thread-safe sequence
/// keeps every default id unique within the process.
/// </remarks>
internal static class ComponentIdSequence
{
	private static int _value;

	/// <summary>
	/// Returns the next number in the sequence.
	/// </summary>
	/// <returns>A number that has not been returned before.</returns>
	public static int Next() => Interlocked.Increment(ref _value);
}
