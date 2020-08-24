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
		public string CancelReason { get; set; } = string.Empty;

		/// <summary>
		/// Gets or sets application defined state.
		/// </summary>
		public object? State { get; set; }
	}

	/// <summary>
	/// The DropZoneUploadEventArgs class provides information for PDDropZone upload events.
	/// </summary>
	public class DropZoneUploadEventArgs
	{
		/// <summary>
		/// Initializes a new instance of the DropZoneUploadEventArgs class.
		/// </summary>
		/// <param name="path">The path where the file is being uploaded.</param>
		/// <param name="name">The name of the file being uploaded.</param>
		/// <param name="size">The size of the file being uploaded.</param>
		public DropZoneUploadEventArgs(string path, string name, long size)
		{
			Path = path;
			Name = name;
			Size = size;
		}

		/// <summary>
		/// Gets or sets the path where the file is being uploaded to.
		/// </summary>
		public string Path { get; set; }

		/// <summary>
		/// Gets or sets the name of the file being uploaded.
		/// </summary>
		public string Name { get; set; }

		/// <summary>
		/// Gets or sets the size of the file being uploaded.
		/// </summary>
		public long Size { get; set; }
	}

	/// <summary>
	/// The DropZoneUploadProgressEventArgs class provides information for PDDropZone upload events.
	/// </summary>
	public class DropZoneUploadProgressEventArgs : DropZoneUploadEventArgs
	{
		/// <summary>
		/// Initializes a new instance of the DropZoneUploadEventArgs class.
		/// </summary>
		/// <param name="path">The path where the file is being uploaded.</param>
		/// <param name="name">The name of the file being uploaded.</param>
		/// <param name="size">The size of the file being uploaded.</param>
		/// <param name="size">The size of the file being uploaded.</param>
		public DropZoneUploadProgressEventArgs(string path, string name, long size, double progress)
			: base(path, name, size)
		{
			Progress = progress;
		}

		/// <summary>
		/// Gets or sets the
		/// </summary>
		public double Progress { get; set; }

		/// <summary>
		/// Gets or sets whether the operation should be canceled.
		/// </summary>
		public bool Cancel { get; set; }

		/// <summary>
		/// Optional string detailing why the operation is to be canceled.
		/// </summary>
		public string CancelReason { get; set; } = string.Empty;
	}
}
