using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace PanoramicData.Blazor.Extensions
{
	public static class StringExtensions
	{
		/// <summary>
		/// Checks if the given path starts with the given old prefix, if it does than it replaces the old prefix
		/// with the given new prefix. This function is specifically for updating relative and absolute path strings.
		/// </summary>
		/// <param name="path">The path to be checked and updated</param>
		/// <param name="oldPathPrefix">The prefix to be checked for.</param>
		/// <param name="newPathPrefix">The new prefix that is substituted for the old prefix.</param>
		/// <returns>A new string containing either the new path if modified, otherwise the original path.</returns>
		public static string ReplacePathPrefix(this string path, string oldPathPrefix, string newPathPrefix)
		{
			if (path == oldPathPrefix || path.StartsWith(oldPathPrefix.TrimEnd('/') + '/'))
			{
				return path.Replace(oldPathPrefix, newPathPrefix);
			}
			return path;
		}

		/// <summary>
		/// Determines if the current string is contained within the given list of strings.
		/// </summary>
		/// <param name="value">The current string to check for.</param>
		/// <param name="comparisonList">A list of one or more comparison strings.</param>
		/// <returns>true if the given string is contained within the given list, otherwise false.</returns>
		public static bool In(this string value, params string[] comparisonList)
		{
			return comparisonList.Contains(value);
		}
	}
}
