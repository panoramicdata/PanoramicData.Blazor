namespace PanoramicData.Blazor.Demo.Data;

public class Todo : ICard
{
	/// <inheritdoc/>
	public Guid Id { get; set; } = Guid.NewGuid();

	/// <summary>
	/// The priority of the todo item.
	/// </summary>
	public Priority Priority { get; set; } = Priority.Major;

	public TodoProgress Progress { get; set; } = TodoProgress.BeingDefined;

	/// <summary>
	/// The title of the todo item.
	/// </summary>
	public required string Title { get; set; } = string.Empty;

	/// <summary>
	/// The description of the todo item.
	/// </summary>
	public required string Description { get; set; } = string.Empty;

	/// <summary>
	/// Holds the position that this card is in within the deck.
	/// </summary>
	public int? DeckPosition { get; set; }

	/// <summary>
	/// Returns a string representation of the todo item, typically the title.
	/// </summary>
	/// <returns></returns>
	public override string ToString() => Title;

}
