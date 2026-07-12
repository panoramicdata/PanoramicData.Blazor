namespace PanoramicData.Blazor.Interfaces;

/// <summary>
/// Defines the minimum contract for items that can be displayed inside a card deck component.
/// </summary>
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
