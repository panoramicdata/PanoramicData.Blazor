namespace PanoramicData.Blazor.Arguments;

/// <summary>
/// The MenuItemsEventArgs class provides information about an event related to a set of MenuItem instances.
/// </summary>
/// <remarks>
/// Initializes a new instance of the MenuItemsEventArgs class.
/// </remarks>
/// <param name="sender">The object that raised the event.</param>
/// <param name="items">The MenuItems the event relates to.</param>
public class MenuItemsEventArgs(object sender, List<MenuItem> items) : CancelEventArgs
{

	/// <summary>
	/// Gets or sets an application specific context.
	/// </summary>
	public object? Context { get; set; }

	/// <summary>
	/// Gets or sets detail son the DOM element that was clicked on.
	/// </summary>
	public ElementInfo? SourceElement { get; set; }

	/// <summary>
	/// Gets the MenuItem the event relates to.
	/// </summary>
	public List<MenuItem> MenuItems { get; } = items;

	/// <summary>
	/// Gets the object that raised the event.
	/// </summary>
	public object Sender { get; } = sender;
}
