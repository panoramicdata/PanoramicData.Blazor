namespace PanoramicData.Blazor;

public partial class PDToolbarLinkButton
{
	/// <summary>
	/// Gets or sets the button sizes.
	/// </summary>
	[Parameter] public ButtonSizes? Size { get; set; }

	/// <summary>
	/// Gets or sets the unique identifier.
	/// </summary>
	[Parameter] public string Key { get; set; } = string.Empty;

	/// <summary>
	/// Gets or sets the text displayed on the button.
	/// </summary>
	[Parameter] public string Text { get; set; } = string.Empty;

	/// <summary>
	/// Gets or sets CSS classes for the button.
	/// </summary>
	[Parameter] public string CssClass { get; set; } = "btn-secondary";

	/// <summary>
	/// Gets or sets CSS classes for the toolbar item.
	/// </summary>
	[Parameter] public string ItemCssClass { get; set; } = "";

	/// <summary>
	/// Gets or sets CSS classes for an optional icon.
	/// </summary>
	[Parameter] public string IconCssClass { get; set; } = string.Empty;

	/// <summary>
	/// Gets or sets CSS classes for the text.
	/// </summary>
	[Parameter] public string TextCssClass { get; set; } = string.Empty;

	/// <summary>
	/// Gets or sets the tooltip for the toolbar item.
	/// </summary>
	[Parameter] public string ToolTip { get; set; } = string.Empty;

	/// <summary>
	/// Gets or sets whether the toolbar item is visible.
	/// </summary>
	[Parameter] public bool IsVisible { get; set; } = true;

	/// <summary>
	/// Gets or sets whether the toolbar item is enabled.
	/// </summary>
	[Parameter] public bool IsEnabled { get; set; } = true;

	/// <summary>
	/// Gets or sets whether the toolbar item is positioned further to the right of the previous toolbar item.
	/// </summary>
	[Parameter] public bool ShiftRight { get; set; } = false;

	/// <summary>
	/// Sets the short cut keys that will perform a click on this button.
	/// In format: 'ctrl-s', 'alt-ctrl-w' (case in-sensitive)
	/// </summary>
	[Parameter] public ShortcutKey ShortcutKey { get; set; } = new ShortcutKey();

	/// <summary>
	/// Sets where to display the linked URL, as the name for a browsing context (a tab, window, or <iframe>).
	/// The following keywords have special meanings for where to load the URL:
	/// _self: the current browsing context. (Default)
	/// _blank: usually a new tab, but users can configure browsers to open a new window instead.
	/// _parent: the parent browsing context of the current one. If no parent, behaves as _self.
	/// _top: the topmost browsing context (the "highest" context that’s an ancestor of the current one). If no ancestors, behaves as _self.
	/// </summary>
	[Parameter] public string Target { get; set; } = "_self";

	/// <summary>
	/// Sets the destination URL.
	/// </summary>
	[Parameter] public string Url { get; set; } = "#";

	private Dictionary<string, object> Attributes { get; set; } = new Dictionary<string, object>();

	protected override void OnParametersSet()
	{
		if (!string.IsNullOrEmpty(Key))
		{
			Attributes.Clear();
			Attributes.Add("Id", $"pd-tbr-btn-{Key}");
		}
	}
}
