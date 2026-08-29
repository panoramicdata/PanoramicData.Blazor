namespace PanoramicData.Blazor.Models;

/// <summary>
/// How a <see cref="ChatConversationQuery"/> is to be matched against a user's conversations.
/// </summary>
/// <remarks>
/// The two modes answer different questions and neither subsumes the other, which is why this is a choice the
/// user makes rather than something the host infers. Keyword finds the conversation in which somebody used a
/// particular word - a hostname, a ticket key - and is the only way to find a term the model has never seen.
/// Semantic finds the conversation that was <i>about</i> something, and is the only way to find a conversation
/// whose subject the user can describe but whose wording they cannot remember.
/// </remarks>
public enum ChatConversationSearchMode
{
	/// <summary>
	/// Matches the literal text against conversation titles and message bodies.
	/// </summary>
	/// <remarks>
	/// The default, and the mode every implementer can support: it needs nothing but the store the
	/// conversations are already in.
	/// </remarks>
	Keyword = 0,

	/// <summary>
	/// Matches on meaning rather than wording, using whatever embedding the host has available.
	/// </summary>
	/// <remarks>
	/// Optional. A host without an embedding model should report the mode as unsupported so that the
	/// component can omit the choice, rather than accepting a query it will fail.
	/// </remarks>
	Semantic = 1
}
