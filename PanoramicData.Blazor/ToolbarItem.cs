namespace PanoramicData.Blazor
{
	/// <summary>
	/// The ToolbarItem class is the base class for toolbar items.
	/// </summary>
	public abstract class ToolbarItem : IToolbarItem
	{
		/// <summary>
		/// Gets or sets the unique identifier of the toolbar item.
		/// </summary>
		public string Key { get; set; } = string.Empty;

		/// <summary>
		/// Gets or sets the tooltip for the toolbar item.
		/// </summary>
		public string ToolTip { get; set; } = string.Empty;

		/// <summary>
		/// Gets or sets whether the toolbar item is visible.
		/// </summary>
		public bool IsVisible { get; set; } = true;

		/// <summary>
		/// Gets or sets whether the toolbar item is enabled.
		/// </summary>
		public bool IsEnabled { get; set; } = true;

		/// <summary>
		/// Gets or sets whether the toolbar item is positioned further to the right of the previous toolbar item.
		/// </summary>
		public bool ShiftRight { get; set; }
	}
}
