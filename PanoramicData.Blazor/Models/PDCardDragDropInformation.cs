namespace PanoramicData.Blazor.Models
{
	/// <summary>
	/// Information about the drag-and-drop state of cards in a card deck.
	/// </summary>
	public class PDCardDragDropInformation
	{
		/// <summary>
		/// Whether a drag event has been detected
		/// </summary>
		public bool IsDragging { get; set; }

		/// <summary>
		/// The index that the current selection will be dragged to when the user drops it.
		/// </summary>
		public int TargetIndex { get; set; }
	}
}
