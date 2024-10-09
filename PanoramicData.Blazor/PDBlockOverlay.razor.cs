namespace PanoramicData.Blazor;

/// <summary>
/// This is the default concrete implementation of the behaviour
/// for the BlockOverlay - this can be customised as appropriate.
/// Users can implement their own one of these and bind to the actions
/// on the IBlockOverlayService once it's registers in services.
/// </summary>
public partial class PDBlockOverlay
{
	[Inject] protected IBlockOverlayService BlockOverlayService { get; set; } = null!;

	protected bool IsVisible { get; set; }

	protected string? Html { get; set; }

	protected string BlockOverlayClass
		=> IsVisible ? "blockoverlay_show" : "blockoverlay_hide";

	protected override void OnInitialized()
	{
		// Bind the actions
		BlockOverlayService.OnShow += Show;
		BlockOverlayService.OnHide += Hide;
	}

	public void Show() => Show(null);

	public void Show(string? html)
	{
		// Set the Html and then make it visible
		Html = html;
		IsVisible = true;
		StateHasChanged();
	}

	public void Hide()
	{
		// Hide the Html and then blank it out
		IsVisible = false;
		Html = string.Empty;
		StateHasChanged();
	}

	public void Dispose()
	{
		// Called when the component is removed
		// Unbind the actions
		BlockOverlayService.OnShow -= Show;
		BlockOverlayService.OnHide -= Hide;
	}
}
