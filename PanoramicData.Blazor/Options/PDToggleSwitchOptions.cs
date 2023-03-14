namespace PanoramicData.Blazor.Options;

public class PDToggleSwitchOptions
{
	/// <summary>
	/// Sets the colour of the border.
	/// </summary>
	public string BorderColour { get; set; } = "#1b6ec2";

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
	/// Sets the background colour of the inner non-toggle area when the switch is off.
	/// </summary>
	public string OffBackgroundColour { get; set; } = "silver";

	/// <summary>
	/// Sets the foreground colour of the inner non-toggle area when the switch is off.
	/// </summary>
	/// <remarks>Leave null to use toggle colour.</remarks>
	public string? OffForegroundColour { get; set; }

	/// <summary>
	/// Sets the text displayed when the switch is off.
	/// </summary>
	public string OffText { get; set; } = string.Empty;

	/// <summary>
	/// Sets the background colour of the inner non-toggle area when the switch is on.
	/// </summary>
	/// <remarks>Leave blank to use border colour.</remarks>
	public string OnBackgroundColour { get; set; } = string.Empty;

	/// <summary>
	/// Sets the foreground colour of the inner non-toggle area when the switch is on.
	/// </summary>
	/// <remarks>Leave null to use toggle colour.</remarks>
	public string? OnForegroundColour { get; set; }

	/// <summary>
	/// Sets the text displayed when the switch is on.
	/// </summary>
	public string OnText { get; set; } = string.Empty;

	/// <summary>
	/// Gets or sets whether the toggle switch has rounded ends.
	/// </summary>
	public bool Rounded { get; set; }

	/// <summary>
	/// Sets the colour of the toggle switch.
	/// </summary>
	public string ToggleColour { get; set; } = "white";

	/// <summary>
	/// Sets the width of the component.
	/// </summary>
	public int? Width { get; set; }

}
