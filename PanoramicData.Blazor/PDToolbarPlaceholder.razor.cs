namespace PanoramicData.Blazor;

public partial class PDToolbarPlaceholder
{
	/// <summary>
	/// Gets or sets the child content that the drop zone wraps.
	/// </summary>
	[Parameter] public RenderFragment? ChildContent { get; set; }

	/// <summary>
	/// Gets or sets whether the toolbar item is visible.
	/// </summary>
	[Parameter] public bool IsVisible { get; set; } = true;

	/// <summary>
	/// Gets or sets CSS classes for the toolbar item.
	/// </summary>
	[Parameter] public string ItemCssClass { get; set; } = "";

	/// <summary>
	/// Gets or sets whether the toolbar item is positioned further to the right of the previous toolbar item.
	/// </summary>
	[Parameter] public bool ShiftRight { get; set; }
}
