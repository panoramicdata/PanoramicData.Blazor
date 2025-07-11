namespace PanoramicData.Blazor.Interfaces;

public interface ICard
{
	/// <summary>
	/// Unique identifier for this card.
	/// </summary>
	public Guid Id { get; set; }

	/// <summary>
	/// Holds the position that this card is in within the deck.
	/// </summary>
	public int? DeckPosition { get; set; }
}
