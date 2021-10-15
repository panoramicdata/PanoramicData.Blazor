using System;
using System.Linq;

namespace PanoramicData.Blazor.Models
{
	public class ElementInfo
	{
		public string[] ClassList { get; set; } = Array.Empty<string>();

		public string Id { get; set; } = string.Empty;

		public ElementInfo? Parent { get; set; }

		public string Tag { get; set; } = string.Empty;

		/// <summary>
		/// Searches for and returns first matching ancestor.
		/// </summary>
		/// <param name="tag">The HTML Tag name to search for.</param>
		/// <param name="classes">Zero or more classes to match.</param>
		/// <returns>An ElementInfo instance of the matched element or null if no match.</returns>
		public ElementInfo? Find(string tag, params string[] classes)
		{
			var el = this.Parent;
			while (el != null)
			{
				if (el.Tag == tag && classes.Except(el.ClassList).Count() == 0)
				{
					return el;
				}
				el = el.Parent;
			}
			return null;
		}

		/// <summary>
		/// Searches ancestors for given tag that has any classes provided.
		/// </summary>
		/// <param name="tag">The HTML Tag name to search for.</param>
		/// <param name="classes">Zero or more classes to match.</param>
		/// <returns>true if any ancestor matches, otherwise false.</returns>
		public bool HasAncestor(string tag, params string[] classes)
		{
			return Find(tag, classes) != null;
		}
	}
}
