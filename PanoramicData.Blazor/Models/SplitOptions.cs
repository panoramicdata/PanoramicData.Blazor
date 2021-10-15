namespace PanoramicData.Blazor.Models
{
	public class SplitOptions
	{
		/// <summary>
		/// Gets or sets an array of panel sizes (in percentages).
		/// </summary>
		public int[]? Sizes { get; set; }

		/// <summary>
		/// Gets or sets an array of panel minimum sizes (in pixels).
		/// </summary>
		public int[]? MinSize { get; set; }

		/// <summary>
		/// Gets or sets whether to expand panels to their min size, possibly overriding the default percentage size.
		/// </summary>
		public bool ExpandToMin { get; set; }

		/// <summary>
		/// Sets the gutter size in pixels.
		/// </summary>
		public int GutterSize { get; set; }

		/// <summary>
		/// Gets or sets the gutter alignment between elements.
		/// </summary>
		public string GutterAlign { get; set; } = "center";

		/// <summary>
		/// Gets or sets the direction the contained panels are split.
		/// </summary>
		public string Direction { get; set; } = "horizontal";

		/// <summary>
		/// Sets the snap to minimum size offset in pixels.
		/// </summary>
		public int SnapOffset { get; set; } = 30;

		/// <summary>
		/// Sets the number of pixels to drag.
		/// </summary>
		public int DragInterval { get; set; } = 1;

		/// <summary>
		/// Gets or sets the cursor to display when dragging.
		/// </summary>
		public string Cursor { get; set; } = "col-resize";
	}
}
