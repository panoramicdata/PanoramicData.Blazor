namespace PanoramicData.Blazor.Demo.Data;

public class Todo : ICard
{
	public Todo()
	{
	}

	public Todo(string title)
	{
		Title = title;
	}

	public Guid Id { get; set; } = Guid.NewGuid();

	public string Title { get; set; } = string.Empty;

	public override string ToString() => Title;

	public int CardSequence { get; set; }
}
