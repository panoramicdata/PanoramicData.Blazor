namespace PanoramicData.Blazor;

public partial class PDToolbarButton : IToolbarItem
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
	/// Event raised whenever user clicks on the button.
	/// </summary>
	[Parameter] public EventCallback<KeyedEventArgs<MouseEventArgs>> Click { get; set; }

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
	[Parameter] public bool ShiftRight { get; set; }

	/// <summary>
	/// Sets the short cut keys that will perform a click on this button.
	/// In format: 'ctrl-s', 'alt-ctrl-w' (case in-sensitive)
	/// </summary>
	[Parameter] public ShortcutKey ShortcutKey { get; set; } = new ShortcutKey();

	/// <summary>
	/// Target where URL content should be opened.
	/// </summary>
	[Parameter] public string Target { get; set; } = "_self";

	/// <summary>
	/// Target URL. If set forces the button to be rendered as an Anchor element.
	/// </summary>
	[Parameter] public string Url { get; set; } = string.Empty;

	private Dictionary<string, object> Attributes { get; set; } = [];

	protected override void OnParametersSet()
	{
		if (!string.IsNullOrEmpty(Key))
		{
			Attributes.Clear();
			Attributes.Add("Id", $"pd-tbr-btn-{Key}");
		}
	}

	private async Task OnClick(MouseEventArgs args) => await Click.InvokeAsync(new KeyedEventArgs<MouseEventArgs>(Key, args)).ConfigureAwait(true);
}
