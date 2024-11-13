namespace PanoramicData.Blazor.Demo.Data;

public class TreeItem : IWebLink
{
	public int Id { get; set; }
	public int? ParentId { get; set; }
	public int Order { get; set; }
	public string Name { get; set; } = string.Empty;
	public string IconCssClass { get; set; } = string.Empty;
	public bool IsGroup { get; set; }

	#region IWebLink members

	public string Target { get; set; } = string.Empty;

	public string Url { get; set; } = string.Empty;

	#endregion

	public override string ToString() => $"{Id} - {Name} (order {Order})";
}
