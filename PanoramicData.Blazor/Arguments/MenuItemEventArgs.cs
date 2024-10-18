namespace PanoramicData.Blazor.Arguments;

/// <summary>
/// The MenuItemEventArgs class provides information about an event related to a particular MenuItem instance.
/// </summary>
/// <remarks>
/// Initializes a new instance of the MenuItemEventArgs class.
/// </remarks>
/// <param name="sender">The object that raised the event.</param>
/// <param name="item">The MenuItem the event relates to.</param>
public class MenuItemEventArgs(object sender, MenuItem item) : CancelEventArgs
{

	/// <summary>
	/// Gets the MenuItem the event relates to.
	/// </summary>
	public MenuItem MenuItem { get; } = item;

	/// <summary>
	/// Gets the object that raised the event.
	/// </summary>
	public object Sender { get; } = sender;
}
