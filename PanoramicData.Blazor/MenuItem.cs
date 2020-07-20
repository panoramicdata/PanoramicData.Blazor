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

		/// <summary>
		/// Gets or sets CSS classes to display an icon for the menu item.
		/// </summary>
		public string IconCssClass { get; set; } = string.Empty;

		/// <summary>
		/// Gets or sets custom markup to be displayed for the item.
		/// </summary>
		public string Content { get; set; } = string.Empty;
	}
}
