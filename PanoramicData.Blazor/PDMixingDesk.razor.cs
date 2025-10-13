namespace PanoramicData.Blazor;

public partial class PDMixingDesk
{
	/// <summary>
	/// Gets or sets the child content (typically PDAudioChannel components).
	/// </summary>
	[Parameter]
	public RenderFragment? ChildContent { get; set; }

	/// <summary>
	/// Gets or sets additional CSS classes.
	/// </summary>
	[Parameter]
	public string CssClass { get; set; } = string.Empty;

	/// <summary>
	/// Gets or sets the minimum height of the mixing desk.
	/// </summary>
	[Parameter]
	public string MinHeight { get; set; } = "600px";
}
