using PanoramicData.Blazor.Models;
using System.Collections.Generic;

namespace PanoramicData.Blazor.Arguments
{
	/// <summary>
	/// The MoveCopyArgs class contains information on a file copy or move operation.
	/// </summary>
	public class MoveCopyArgs
	{
		/// <summary>
		/// Gets or sets a list of file items to be moved or copied.
		/// </summary>
		public List<FileExplorerItem> Payload { get; set; } = new List<FileExplorerItem>();

		/// <summary>
		/// Gets or sets the target path for the file items to be moved to copied to.
		/// </summary>
		public string TargetPath { get; set; } = string.Empty;

		/// <summary>
		/// Gets or sets a list of items in the target path.
		/// </summary>
		public List<FileExplorerItem> TargetItems { get; set; } = new List<FileExplorerItem>();

		/// <summary>
		/// Gets or sets whether this is a copy operation (true) or move (false).
		/// </summary>
		public bool IsCopy { get; set; }

		/// <summary>
		/// Gets or sets a list of file items that already exist at the destination.
		/// </summary>
		public List<FileExplorerItem> Conflicts { get; set; } = new List<FileExplorerItem>();

		/// <summary>
		/// Gets or sets how any conflicts should be handled.
		/// </summary>
		public ConflictResolutions ConflictResolution { get; set; } = ConflictResolutions.Skip;
	}
}
