namespace PanoramicData.Blazor.Models;

public class BasicItem : IDisplayItem
{
	public string IconCssClass { get; set; } = string.Empty;

	public string Id { get; set; } = string.Empty;

	public string Text { set; get; } = string.Empty;

}

public class SelectableItem : BasicItem, ISelectable
{
	public bool IsEnabled { set; get; } = true;

	public bool IsSelected { set; get; }
}
