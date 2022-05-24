namespace PanoramicData.Blazor.Demo.Data;

public class TreeItem
{
	public int Id { get; set; }
	public int? ParentId { get; set; }
	public int Order { get; set; }
	public string Name { get; set; } = string.Empty;
	public string IconCssClass { get; set; } = string.Empty;
	public bool IsGroup { get; set; }

	public override string ToString()
	{
		return $"{Id} - {Name} (order {Order})";
	}
}
