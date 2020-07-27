using System.ComponentModel;

namespace PanoramicData.Blazor
{
	/// <summary>
	/// The MenuItemEventArgs class provides information about an event related to a particular MenuItem instance.
	/// </summary>
	public class MenuItemEventArgs : CancelEventArgs
	{
		/// <summary>
		/// Initializes a new instance of the MenuItemEventArgs class.
		/// </summary>
		/// <param name="item">The MenuItem the event relates to.</param>
		public MenuItemEventArgs(MenuItem item)
		{
			MenuItem = item;
		}

		/// <summary>
		/// Gets the MenuItem the event relates to.
		/// </summary>
		public MenuItem MenuItem { get; }
	}
}
