using PanoramicData.Blazor.Models;
using System;

namespace PanoramicData.Blazor.Arguments
{
	public class UploadsReadyEventArgs
	{
		public bool Cancel { get; set; }

		public bool Overwrite { get; set; }

		public DropZoneFile[] Files { get; set; } = Array.Empty<DropZoneFile>();

		public DropZoneFile[] FilesToSkip { get; set; } = Array.Empty<DropZoneFile>();
	}
}
