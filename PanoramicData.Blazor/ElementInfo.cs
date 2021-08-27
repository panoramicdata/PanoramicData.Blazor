using System;

namespace PanoramicData.Blazor
{
	public class ElementInfo
	{
		public string[] ClassList { get; set; } = Array.Empty<string>();

		public ElementInfo? Parent { get; set; }

		public string Tag { get; set; } = string.Empty;
	}
}
