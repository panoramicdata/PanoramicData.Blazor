namespace PanoramicData.Blazor.Models;

/// <summary>
/// A basic implementation of <see cref="IDisplayItem"/> for use in list and selection controls.
/// </summary>
public class BasicItem : IDisplayItem
{
	/// <inheritdoc />
	public string IconCssClass { get; set; } = string.Empty;

	/// <inheritdoc />
	public string Id { get; set; } = string.Empty;

	/// <inheritdoc />
	public string Text { set; get; } = string.Empty;

}

/// <summary>
/// Extends <see cref="BasicItem"/> with <see cref="ISelectable"/> support for use in selection controls.
/// </summary>
public class SelectableItem : BasicItem, ISelectable
{
	/// <inheritdoc />
	public bool IsEnabled { set; get; } = true;

	/// <inheritdoc />
	public bool IsSelected { set; get; }
}
