namespace PanoramicData.Blazor;

/// <summary>
/// A Blazor component that represents a single item within a <see cref="PDToolbarDropdown"/> menu.
/// </summary>
public partial class PDMenuItem
{
	/// <summary>
	/// Gets or sets the parent toolbar dropdown this item belongs to.
	/// </summary>
	[CascadingParameter(Name = "ToolbarDropdown")]
	public PDToolbarDropdown ToolbarDropdown { get; set; } = null!;

	/// <summary>
	/// Gets or sets the unique identifier of the menu item.
	/// </summary>
	[Parameter]
	public string Key { get; set; } = string.Empty;

	/// <summary>
	/// Gets or sets the text to display on the menu item.
	/// </summary>
	[Parameter]
	public string Text { get; set; } = string.Empty;

	/// <summary>
	/// Gets or sets whether this item is displayed.
	/// </summary>
	[Parameter]
	public bool IsVisible { get; set; } = true;

	/// <summary>
	/// Gets or sets whether this item is displayed but disabled.
	/// </summary>
	[Parameter]
	public bool IsDisabled { get; set; }

	/// <summary>
	/// Gets or sets CSS classes to display an icon for the menu item.
	/// </summary>
	[Parameter]
	public string IconCssClass { get; set; } = string.Empty;

	/// <summary>
	/// Gets or sets custom markup to be displayed for the item.
	/// </summary>
	[Parameter]
	public string Content { get; set; } = string.Empty;

	/// <summary>
	/// Gets or sets whether this item is rendered as a separator.
	/// </summary>
	[Parameter]
	public bool IsSeparator { get; set; }

	/// <summary>
	/// Sets the short cut keys that will perform a click on this button.
	/// </summary>
	[Parameter]
	public ShortcutKey ShortcutKey { get; set; } = new ShortcutKey();

	/// <inheritdoc />
	protected override void OnInitialized() => ToolbarDropdown?.AddMenuItem(this);
}
