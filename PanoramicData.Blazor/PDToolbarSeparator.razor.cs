namespace PanoramicData.Blazor;

public partial class PDToolbarSeparator : IToolbarItem
{
	/// <summary>
	/// Gets or sets the unique identifier.
	/// </summary>
	[Parameter] public string Key { get; set; } = string.Empty;

	/// <summary>
	/// Gets or sets CSS classes for the separator.
	/// </summary>
	[Parameter] public string CssClass { get; set; } = "";

	/// <summary>
	/// Gets or sets CSS classes for the toolbar item.
	/// </summary>
	[Parameter] public string ItemCssClass { get; set; } = "";

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
}
