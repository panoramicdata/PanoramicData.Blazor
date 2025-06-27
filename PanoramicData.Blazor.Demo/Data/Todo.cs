namespace PanoramicData.Blazor.Demo.Data;

public class Todo : ICard
{
	/// <inheritdoc/>
	public Guid Id { get; set; } = Guid.NewGuid();

	public Priority Priority { get; set; } = Priority.Major;

	public required string Title { get; set; } = string.Empty;

	public required string Description { get; set; } = string.Empty;

	public override string ToString() => Title;

}
