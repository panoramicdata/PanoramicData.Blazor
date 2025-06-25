
namespace PanoramicData.Blazor
{
	/// <summary>
	/// Holds information and event handlers for a Deck that is a member of a group
	/// </summary>
	/// <typeparam name="TCard"></typeparam>
	internal class DeckGroupContext<TCard>(bool isMember) where TCard : ICard
	{
		/// <summary>
		/// Flag to indicate to parent objects that this deck is part of a deck group (e.g. in PDCardDeckGroup)
		/// </summary>
		internal bool IsMemberOfGroup { get; private set; } = isMember;

		#region Deck Interaction Events

		/// <summary>
		/// Occurrs on user's mouse up event (when within the bounds of this PDCardDeck)
		/// </summary>
		internal Func<PDCardDeck<TCard>, Task>? OnDeckSelected { get; set; }

		/// <summary>
		/// Occurrs on user's drag event (when within the bounds of this PDCardDeck)
		/// </summary>
		internal Func<PDCardDeck<TCard>, Task>? OnDeckDragStarted { get; set; }

		/// <summary>
		/// Occurrs when a user is dragging and they enter the bounds of this PDCardDeck
		/// </summary>
		internal Func<PDCardDeck<TCard>, Task>? OnDeckDragEntered { get; set; }

		/// <summary>
		/// Occurrs when a user drags content from this deck outside of the bounds of this deck
		/// </summary>
		internal Func<PDCardDeck<TCard>, Task>? OnDeckDragLeft { get; set; }

		#endregion
	}
}