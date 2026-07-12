namespace PanoramicData.Blazor.Services;

/// <summary>
/// See IBlockOverlayService for description.
/// The BlockOverlay component can then implement the behaviour as appropriate.
/// </summary>
public class BlockOverlayService : IBlockOverlayService
{
	/// <inheritdoc />
	public event Action<string?>? OnShow;

	/// <inheritdoc />
	public event Action? OnHide;

	/// <summary>
	/// Displays the block overlay without custom HTML content.
	/// </summary>
	public void Show()
		=> OnShow?.Invoke(null);

	/// <inheritdoc />
	public void Show(string? html)
		=> OnShow?.Invoke(html);

	/// <inheritdoc />
	public void Hide()
		=> OnHide?.Invoke();
}
