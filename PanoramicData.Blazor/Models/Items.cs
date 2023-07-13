namespace PanoramicData.Blazor.Models;

public class BasicItem : IDisplayItem
{
	public BasicItem(string id)
	{
		Id = id;
	}

	public BasicItem(string id, string text)
	{
		Id = id;
		Text = text;
	}

	public BasicItem(string id, string text, string iconCssClass)
	{
		Id = id;
		Text = text;
		IconCssClass = iconCssClass;
	}

	public string Id { get; private set; } = string.Empty;

	public string Text { set; get; } = string.Empty;

	public string IconCssClass { get; set; } = string.Empty;
}

public class SelectableItem : BasicItem, ISelectable
{
	public SelectableItem(string id)
		: base(id)
	{
	}

	public SelectableItem(string id, string text)
		: base(id, text)
	{
	}

	public SelectableItem(string id, string text, string iconCssClass)
		: base(id, text, iconCssClass)
	{
	}

	public bool IsEnabled { set; get; } = true;

	public bool IsSelected { set; get; }
}
