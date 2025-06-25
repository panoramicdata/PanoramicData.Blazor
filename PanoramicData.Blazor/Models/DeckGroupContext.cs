namespace PanoramicData.Blazor.Models
{
	/// <summary>
	/// Holds information and event handlers for a Deck that is a member of a group
	/// </summary>
	/// <typeparam name="TCard"></typeparam>
	public class DeckGroupContext<TCard>(bool isMember) where TCard : ICard
	{
		/// <summary>
		/// Flag to indicate to parent objects that this deck is part of a deck group (e.g. in PDCardDeckGroup)
		/// </summary>
		internal bool IsMemberOfGroup { get; private set; } = isMember;

		#region Deck Interaction Events

		/// <summary>
		/// Occurrs on user's mouse up event (when within the bounds of this PDCardDeck)
		/// </summary>
		internal Func<PDCardDeck<TCard>, Task>? OnSelect { get; set; }

		/// <summary>
		/// Occurrs when a user drags an item inside the bounds of this deck
		/// </summary>
		internal Func<PDCardDeck<TCard>, Task>? OnDragEntered { get; set; }

		#endregion
	}
}