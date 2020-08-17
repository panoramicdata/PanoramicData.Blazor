namespace PanoramicData.Blazor
{
	/// <summary>
	///  The ToolbarButton class is used to place a button onto a toolbar.
	/// </summary>
	public class ToolbarButton : ToolbarItem
	{
		/// <summary>
		/// Gets or sets the CSS class names used to place an icon on the button.
		/// </summary>
		public string IconCssClass { get; set; }

		/// <summary>
		/// Gets or sets the text displayed on the button.
		/// </summary>
		public string Text { get; set; }
	}
}
