namespace PanoramicData.Blazor;

/// <summary>
/// This is the default concrete implementation of the behaviour
/// for the BlockOverlay - this can be customised as appropriate.
/// Users can implement their own one of these and bind to the actions
/// on the IBlockOverlayService once it's registers in services.
/// </summary>
public partial class PDBlockOverlay
{
	/// <summary>
	/// Gets or sets the service that controls overlay visibility requests.
	/// </summary>
	[Inject] protected IBlockOverlayService BlockOverlayService { get; set; } = null!;

	/// <summary>
	/// Gets a value indicating whether the overlay is visible.
	/// </summary>
	protected bool IsVisible { get; set; }

	/// <summary>
	/// Gets the optional HTML content rendered inside the overlay.
	/// </summary>
	protected string? Html { get; set; }

	/// <summary>
	/// Gets the CSS class for current overlay visibility state.
	/// </summary>
	protected string BlockOverlayClass
		=> IsVisible ? "blockoverlay_show" : "blockoverlay_hide";

	/// <summary>
	/// Subscribes to overlay show/hide service events.
	/// </summary>
	protected override void OnInitialized()
	{
		// Bind the actions
		BlockOverlayService.OnShow += Show;
		BlockOverlayService.OnHide += Hide;
	}

	/// <summary>
	/// Shows the overlay without custom content.
	/// </summary>
	public void Show() => Show(null);

	/// <summary>
	/// Shows the overlay with optional HTML content.
	/// </summary>
	/// <param name="html">Optional HTML content.</param>
	public void Show(string? html)
	{
		// Set the Html and then make it visible
		Html = html;
		IsVisible = true;
		StateHasChanged();
	}

	/// <summary>
	/// Hides the overlay and clears content.
	/// </summary>
	public void Hide()
	{
		// Hide the Html and then blank it out
		IsVisible = false;
		Html = string.Empty;
		StateHasChanged();
	}

	/// <summary>
	/// Unsubscribes service events and disposes component resources.
	/// </summary>
	public void Dispose()
	{
		// Called when the component is removed
		// Unbind the actions
		BlockOverlayService.OnShow -= Show;
		BlockOverlayService.OnHide -= Hide;
		GC.SuppressFinalize(this);
	}
}
