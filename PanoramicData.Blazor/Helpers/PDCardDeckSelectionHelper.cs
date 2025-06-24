namespace PanoramicData.Blazor.Helpers
{
	/// <summary>
	/// Helper class for managing card selection in a card deck.
	/// </summary>
	internal class PDCardDeckSelectionHelper
	{
		/// <summary>
		/// Toggles the membership of a given card in the selection list.
		/// </summary>
		/// <typeparam name="TCard"></typeparam>
		/// <param name="card"></param>
		/// <param name="selection"></param>
		/// <returns></returns>
		internal static List<TCard> HandleIndividualAddRemove<TCard>(TCard card, List<TCard> selection) where TCard : ICard
		{
			// Add the card to the selection if it is not already selected
			if (!selection.Remove(card))
			{
				selection.Add(card);
			}

			return selection;
		}

		/// <summary>
		/// Adds a range of cards to the selection based on the Shift+Click action.
		/// </summary>
		/// <param name="currentCard"></param>
		internal static List<TCard> HandleAddRange<TCard>(List<TCard> selection, List<TCard> cards, TCard currentCard) where TCard : ICard
		{
			// For accessibility, on Shift+Click, select all cards from start to the current card
			int lastPosition = 0;

			if (selection.Count != 0)
			{
				// Check index of the last added card, selection will be calculated from this point
				lastPosition = cards.IndexOf(selection[^1]);
			}

			int currentPosition = cards.IndexOf(currentCard);

			// Cannot find Card, Illegal
			if (lastPosition == -1 || currentPosition == -1)
			{
				return selection;
			}

			// Find Smallest bound and largest bound
			var start = Math.Min(lastPosition, currentPosition);
			var end = Math.Max(lastPosition, currentPosition);

			List<TCard> currentSelection = [];

			for (int i = start; i <= end; i++)
			{
				var cardToSelect = cards.FirstOrDefault(c => cards.IndexOf(c) == i);

				// Cannot select a null card, so skip if it is null
				if (cardToSelect == null)
				{
					// TODO: Log a warning or handle the case where the card is not found
					return selection;
				}

				if (currentPosition > lastPosition)
				{
					// If the current position is greater than the last position, we are selecting backwards
					currentSelection.Insert(0, cardToSelect);
				}
				else
				{
					// Otherwise, we are selecting forwards
					currentSelection.Add(cardToSelect);
				}
			}

			// Replace the current selection with the new selection
			selection.Clear();
			selection.AddRange(currentSelection);

			return selection;
		}
	}
}
