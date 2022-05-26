namespace PanoramicData.Blazor.Arguments;

/// <summary>
/// The MenuItemEventArgs class provides information about an event related to a particular MenuItem instance.
/// </summary>
public class MenuItemEventArgs : CancelEventArgs
{
	/// <summary>
	/// Initializes a new instance of the MenuItemEventArgs class.
	/// </summary>
	/// <param name="sender">The object that raised the event.</param>
	/// <param name="item">The MenuItem the event relates to.</param>
	public MenuItemEventArgs(object sender, MenuItem item)
	{
		MenuItem = item;
		Sender = sender;
	}

	/// <summary>
	/// Gets the MenuItem the event relates to.
	/// </summary>
	public MenuItem MenuItem { get; }

	/// <summary>
	/// Gets the object that raised the event.
	/// </summary>
	public object Sender { get; }
}
