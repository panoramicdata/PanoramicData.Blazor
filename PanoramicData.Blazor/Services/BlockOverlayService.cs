namespace PanoramicData.Blazor.Services;

/// <summary>
/// See IBlockOverlayService for description.
/// The BlockOverlay component can then implement the behavior as appropriate.
/// </summary>
public class BlockOverlayService : IBlockOverlayService
{
	public event Action<string?>? OnShow;

	public event Action? OnHide;

	public void Show()
		=> OnShow?.Invoke(null);

	public void Show(string? html)
		=> OnShow?.Invoke(html);

	public void Hide()
		=> OnHide?.Invoke();
}
