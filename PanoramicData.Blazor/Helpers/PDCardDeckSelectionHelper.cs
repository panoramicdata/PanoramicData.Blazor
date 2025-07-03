namespace PanoramicData.Blazor.Helpers
{
	/// <summary>
	/// Helper class for managing card selection in a card deck.
	/// </summary>
	internal class PDCardDeckSelectionHelper<TCard> where TCard : ICard
	{
		/// <summary>
		/// The Card at which multiple selection will pivot around when Shift+Click is used.
		/// </summary>
		private TCard? _multiSelectionPivot;


		/// <summary>
		/// Toggles the membership of a given card in the selection list.
		/// </summary>
		/// <typeparam name="TCard"></typeparam>
		/// <param name="card"></param>
		/// <param name="selection"></param>
		/// <returns></returns>
		internal List<TCard> HandleIndividualAddRemove(List<TCard> selection, List<TCard> cards, TCard card)
		{
			// Add the card to the selection if it is not already selected
			if (!selection.Remove(card))
			{
				selection.Add(card);
				_multiSelectionPivot = card; // Set the pivot to the newly added card
			}

			return [.. selection];
		}

		/// <summary>
		/// Adds a range of cards to the selection based on the Shift+Click action.
		/// </summary>
		/// <param name="currentCard"></param>
		internal List<TCard> HandleAddRange(List<TCard> selection, List<TCard> cards, TCard currentCard)
		{
			// For accessibility, on Shift+Click, select all cards from start to the current card if there is no multi-selection pivot set.
			int lastPosition = 0;

			if (_multiSelectionPivot != null)
			{
				// Check index of the pivot card, selection will be calculated from this point
				lastPosition = cards.IndexOf(_multiSelectionPivot);

				// If the pivot card is not found, reset it
				if (lastPosition == -1)
				{
					_multiSelectionPivot = default;
					return selection;
				}
			}

			int currentPosition = cards.IndexOf(currentCard);

			// Cannot find Card, Illegal
			if (currentPosition == -1)
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

				// Add Card to selection
				currentSelection.Add(cardToSelect);

			}

			// Replace the current selection with the new selection
			selection.Clear();
			selection.AddRange(currentSelection);

			return [.. selection];
		}

		internal List<TCard> HandleSingleSelect(List<TCard> selection, List<TCard> cards, TCard card)
		{
			// If no modifier keys are pressed, clear the selection and add the clicked card
			selection.Clear();
			selection.Add(card);
			_multiSelectionPivot = card; // Set the pivot to the newly added card

			return [.. selection];
		}
	}
}
