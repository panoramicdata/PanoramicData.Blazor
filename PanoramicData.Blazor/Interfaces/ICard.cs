namespace PanoramicData.Blazor.Interfaces;

public interface ICard
{
	/// <summary>
	/// The current position of the card in the deck or list.
	/// </summary>
	public int CurrentPosition { get; set; }
}
