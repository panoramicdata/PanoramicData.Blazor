namespace PanoramicData.Blazor;

/// <summary>
/// Generates the default ids of <see cref="PDCardDeck{TCard}"/> and <see cref="PDCardDeckGroup{TCard}"/>.
/// </summary>
/// <remarks>
/// Kept outside the generic components so that every deck shares one sequence whatever its card type: a static
/// counter in a generic type is a separate counter per card type, so two decks of different card types would
/// both be given <c>pd-carddeck-1</c> and the page would hold duplicate element ids.
/// </remarks>
internal static class PDCardDeckIdSequence
{
	private static int _deck;
	private static int _group;

	/// <summary>Returns a new, unique default id for a card deck.</summary>
	internal static string NextDeckId() => $"pd-carddeck-{Interlocked.Increment(ref _deck)}";

	/// <summary>Returns a new, unique default id for a card deck group.</summary>
	internal static string NextGroupId() => $"pd-carddeckgroup-{Interlocked.Increment(ref _group)}";
}
