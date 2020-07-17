namespace PanoramicData.Blazor
{
	/// <summary>
	///The MenuItem class is used to hold details of a single menu entry.
	/// </summary>
	public class MenuItem
	{
		/// <summary>
		/// Gets or sets the text display on the menu item.
		/// </summary>
		public string Text { get; set; } = string.Empty;

		/// <summary>
		/// Gets or sets whether this item is displayed.
		/// </summary>
		public bool IsVisible { get; set; } = true;

		/// <summary>
		/// Gets or sets whether this item is displayed but disabled.
		/// </summary>
		public bool IsDisabled { get; set; }
	}
}
