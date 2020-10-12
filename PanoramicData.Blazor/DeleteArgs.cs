namespace PanoramicData.Blazor
{
	/// <summary>
	/// The DeleteArgs class provides arguments for a delete operation.
	/// </summary>
	public class DeleteArgs
	{
		/// <summary>
		/// Gets the file items to be deleted.
		/// </summary>
		public FileExplorerItem[] Items { get; set; } = new FileExplorerItem[0];

		/// <summary>
		/// Gets or sets the outcome of the delete.
		/// </summary>
		public DeleteResolutions Resolution { get; set; }

		/// <summary>
		/// Enumeration of possible delete resolutions.
		/// </summary>
		public enum DeleteResolutions
		{
			/// <summary>
			/// Prompt the user for decision.
			/// </summary>
			Prompt,
			/// <summary>
			/// Cancel the entire operation.
			/// </summary>
			Cancel,
			/// <summary>
			/// Proceed to delete all items.
			/// </summary>
			Delete
		}
	}
}
