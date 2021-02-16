using PanoramicData.Blazor.Services;

namespace PanoramicData.Blazor
{
	/// <summary>
	///The MenuItem class is used to hold details of a single menu entry.
	/// </summary>
	public class MenuItem
	{
		/// <summary>
		/// Gets or sets the unique identifier of the menu item.
		/// </summary>
		public string Key { get; set; } = string.Empty;

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

		/// <summary>
		/// Gets or sets whether this item is rendered as a separator.
		/// </summary>
		public bool IsSeparator { get; set; }

		/// <summary>
		/// Sets the short cut keys that will perform a click on this button.
		/// In format: 'ctrl-s', 'alt-ctrl-w' (case in-sensitive)
		/// </summary>
		public ShortcutKey ShortcutKey { get; set; } = new ShortcutKey();

		/// <summary>
		/// Gets the items Key if specified otherwise returns the items text.
		/// </summary>
		/// <returns></returns>
		public string GetKeyOrText()
		{
			if (string.IsNullOrWhiteSpace(Key))
			{
				return Text.Replace("&&", "");
			}
			return Key;
		}
	}
}
