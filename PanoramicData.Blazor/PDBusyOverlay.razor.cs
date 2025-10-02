namespace PanoramicData.Blazor;

public partial class PDBusyOverlay
{
	/// <summary>
	/// Gets or sets the CSS class for the component.
	/// </summary>
	[Parameter]
	public string CssClass { get; set; } = string.Empty;

	/// <summary>
	/// Gets or sets whether the busy overlay is active.
	/// </summary>
	[Parameter]
	public bool IsBusy { get; set; }

	/// <summary>
	/// Gets or sets the child content of the component.
	/// </summary>
	[Parameter]
	public RenderFragment? ChildContent { get; set; }

	/// <summary>
	/// Gets or sets the CSS class for the overlay.
	/// </summary>
	[Parameter]
	public string OverlayCssClass { get; set; } = "fas fa-4x fa-spin fa-circle-notch";

	/// <summary>
	/// Gets or sets the content to be displayed in the overlay.
	/// </summary>
	[Parameter]
	public RenderFragment? OverlayContent { get; set; }
}