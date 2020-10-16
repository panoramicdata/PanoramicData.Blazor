namespace PanoramicData.Blazor
{
	/// <summary>
	///  The ToolbarButton class is used to place a button onto a toolbar.
	/// </summary>
	public class ToolbarButton : ToolbarItem
	{
		private string? _key;

		/// <summary>
		/// Override the original implementation to support omitting Key property
		/// and simply having key returned as the current Text value.
		/// </summary>
		public override string Key
		{
			get
			{
				return _key ?? Text;
			}
			set
			{
				_key = value;
			}
		}

		/// <summary>
		/// Gets or sets the CSS class names used to style the button.
		/// </summary>
		public string CssClass { get; set; } = "btn-light";

		/// <summary>
		/// Gets or sets the CSS class names used to place an icon on the button.
		/// </summary>
		public string IconCssClass { get; set; } = string.Empty;

		/// <summary>
		/// Gets or sets the text displayed on the button.
		/// </summary>
		public string Text { get; set; } = string.Empty;

		/// <summary>
		/// Gets or sets the CSS class names used to adjust the text.
		/// </summary>
		public string TextCssClass { get; set; } = string.Empty;
	}
}
