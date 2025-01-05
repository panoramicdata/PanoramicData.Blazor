namespace PanoramicData.Blazor.Arguments;

/// <summary>
/// The DropZoneEventArgs class provides information for PDDropZone drop events.
/// </summary>
/// <remarks>
/// Initializes a new instance of the MenuItemEventArgs class.
/// </remarks>
/// <param name="sender">The object that raised the event.</param>
/// <param name="files">Files dropped onto the zone.</param>
public class DropZoneEventArgs(object sender, DropZoneFile[] files)
{

	/// <summary>
	/// Gets the object that raised the event.
	/// </summary>
	public object Sender { get; } = sender;

	/// <summary>
	/// Gets the files dropped onto the zone.
	/// </summary>
	public DropZoneFile[] Files { get; } = files;

	/// <summary>
	/// Gets or sets whether the operation should be cancelled.
	/// </summary>
	public bool Cancel { get; set; }

	/// <summary>
	/// Optional string detailing why the operation is to be cancelled.
	/// </summary>
	public string CancelReason { get; set; } = string.Empty;

	/// <summary>
	/// Gets or sets the base folder.
	/// </summary>
	public string BaseFolder { get; set; } = string.Empty;

	/// <summary>
	/// Gets or sets application defined state.
	/// </summary>
	public object? State { get; set; }
}
