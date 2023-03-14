namespace PanoramicData.Blazor;

public class PDComponentBase : ComponentBase
{
	/// <summary>
	/// Gets or sets CSS classes for the component.
	/// </summary>
	[Parameter] public string? CssClass { get; set; }

	/// <summary>
	/// Gets or sets whether the component is enabled.
	/// </summary>
	[Parameter] public bool IsEnabled { get; set; } = true;

	/// <summary>
	/// Gets or sets the component size.
	/// </summary>
	[Parameter] public ButtonSizes? Size { get; set; }

	/// <summary>
	/// Gets or sets the tooltip for the component.
	/// </summary>
	[Parameter] public string ToolTip { get; set; } = string.Empty;

	/// <summary>
	/// Gets or sets whether the component is visible.
	/// </summary>
	[Parameter] public bool IsVisible { get; set; } = true;
}
