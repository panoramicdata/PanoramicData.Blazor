namespace PanoramicData.Blazor.Demo.Data;

public class Car
{
	public int FromYear { get; set; }

	public int Id { get; set; }

	public string Make { get; set; } = string.Empty;

	public string Model { get; set; } = string.Empty;

	public int? ToYear { get; set; }

	public override string ToString() => $"{Make} {Model}";
}
