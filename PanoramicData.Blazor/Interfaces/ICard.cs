namespace PanoramicData.Blazor.Interfaces;

public interface ICard
{
	/// <summary>
	/// Unique identifier for this card.
	/// </summary>
	public Guid Id { get; set; }

	/// <summary>
	/// The current position of the card in the deck or list.
	/// </summary>
	public int CurrentPosition { get; set; }
}
