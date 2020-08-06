namespace PanoramicData.Blazor
{
	/// <summary>
	/// The DropZoneEventArgs class provides information for PDDropZone drop events.
	/// </summary>
	public class DropZoneEventArgs
	{
		/// <summary>
		/// Initializes a new instance of the MenuItemEventArgs class.
		/// </summary>
		/// <param name="sender">The object that raised the event.</param>
		/// <param name="files">Files dropped onto the zone.</param>
		public DropZoneEventArgs(object sender, DropZoneFile[] files)
		{
			Sender = sender;
			Files = files;
		}

		/// <summary>
		/// Gets the object that raised the event.
		/// </summary>
		public object Sender { get; }

		/// <summary>
		/// Gets the files dropped onto the zone.
		/// </summary>
		public DropZoneFile[] Files { get; }

		/// <summary>
		/// Gets or sets whether the operation should be canceled.
		/// </summary>
		public bool Cancel { get; set; }

		/// <summary>
		/// Optional string detailing why the operation is to be canceled.
		/// </summary>
		public string CancelReason { get; set; }

		/// <summary>
		/// Gets or sets application defined state.
		/// </summary>
		public object State { get; set; }
	}
}
