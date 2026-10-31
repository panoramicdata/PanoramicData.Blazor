namespace PanoramicData.Blazor;

/// <summary>
/// Joins the toolbar items inside it into one group of buttons, edge to edge with rounded outer corners only, like a
/// Bootstrap button group, within a <see cref="PDToolbar"/>.
/// </summary>
public partial class PDToolbarGroup
{
	/// <summary>
	/// Gets or sets the toolbar items to group, such as <see cref="PDToolbarButton"/> and <see cref="PDToolbarDropdown"/>.
	/// </summary>
	[Parameter] public RenderFragment? ChildContent { get; set; }

	/// <summary>
	/// Gets or sets what the group is for, read out by screen readers.
	/// </summary>
	[Parameter] public string? Label { get; set; }

	/// <summary>
	/// Gets or sets whether the group is visible.
	/// </summary>
	[Parameter] public bool IsVisible { get; set; } = true;

	/// <summary>
	/// Gets or sets CSS classes for the group.
	/// </summary>
	[Parameter] public string ItemCssClass { get; set; } = "";

	/// <summary>
	/// Gets or sets whether the group is positioned further to the right of the previous toolbar item.
	/// </summary>
	[Parameter] public bool ShiftRight { get; set; }
}
