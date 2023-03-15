namespace PanoramicData.Blazor.Options;

public class PDToggleSwitchOptions
{
	/// <summary>
	/// Sets the width of the border.
	/// </summary>
	public int BorderWidth { get; set; } = 2;

	/// <summary>
	/// Sets CSS classes for the component.
	/// </summary>
	public string CssClass { get; set; } = "";

	/// <summary>
	/// Sets the height of the component.
	/// </summary>
	public int? Height { get; set; }

	/// <summary>
	/// Sets the text displayed when the switch is off.
	/// </summary>
	public string OffText { get; set; } = string.Empty;

	/// <summary>
	/// Sets the text displayed when the switch is on.
	/// </summary>
	public string OnText { get; set; } = string.Empty;

	/// <summary>
	/// Gets or sets whether the toggle switch has rounded ends.
	/// </summary>
	public bool Rounded { get; set; }

	/// <summary>
	/// Gets or sets a standard size for the component.
	/// </summary>
	public ButtonSizes Size { get; set; } = ButtonSizes.Medium;

	/// <summary>
	/// Sets the width of the component.
	/// </summary>
	public int? Width { get; set; }
}
