namespace PanoramicData.Blazor.Extensions;

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
		if (path == oldPathPrefix || path.StartsWith(oldPathPrefix.TrimEnd('/') + '/', StringComparison.Ordinal))
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
	public static bool In(this string value, params string[] comparisonList) => comparisonList.Contains(value);

	/// <summary>
	/// Appends the shortcut keys to the given text.
	/// </summary>
	/// <param name="text">The text to be appended.</param>
	/// <param name="shortcutKey">The shortcut key combination.</param>
	/// <returns>A new string contain the given text with the shortcut text appended.</returns>
	public static string AppendShortcut(this string text, ShortcutKey shortcutKey)
	{
		if (string.IsNullOrEmpty(text) || !shortcutKey.HasValue)
		{
			return text;
		}

		return $"{text.Replace("&&", "")} ({shortcutKey})";
	}

	/// <summary>
	/// Returns a markup string that highlight (underline) the shortcut key.
	/// </summary>
	/// <param name="text">The text containing a double ampersand (&&) before the character to highlight.</param>
	/// <returns>A new MarkupString instance containing the markup text.</returns>
	public static MarkupString GetShortcutMarkup(this string text)
	{
		if (string.IsNullOrEmpty(text))
		{
			return (MarkupString)text;
		}

		var ampIdx = text.IndexOf("&&", StringComparison.Ordinal);
		if (ampIdx == -1)
		{
			return (MarkupString)text;
		}

		var sb = new StringBuilder();
		sb.Append("<span>")
			.Append(text[..ampIdx])
			.Append("<u>")
			.Append(text.AsSpan(ampIdx + 2, 1))
			.Append("</u>")
			.Append(text[(ampIdx + 3)..])
			.Append("</span>");
		return (MarkupString)sb.ToString();
	}

	public static string LowerFirstChar(this string text) => string.IsNullOrWhiteSpace(text)
		? text
		: text[0].ToString().ToLowerInvariant() + text[1..];

	public static string UpperFirstChar(this string text) => string.IsNullOrWhiteSpace(text)
		? text
		: text[0].ToString().ToUpperInvariant() + text[1..];

	public static string QuoteIfContainsWhitespace(this string text)
	{
		if (text.Length > 0)
		{
			if (!(text.StartsWith("\"", StringComparison.Ordinal) && text.StartsWith("\"", StringComparison.Ordinal)) && !(text.StartsWith("#", StringComparison.Ordinal) && text.StartsWith("#", StringComparison.Ordinal)))
			{
				if (text.Contains(' ') || text.Contains('\t') || text.Contains('\r') || text.Contains('\n'))
				{
					return $"\"{text}\"";
				}
			}
		}

		return text;
	}

	public static string RemoveQuotes(this string text)
	{
		if (text.StartsWith("\"", StringComparison.Ordinal) && text.EndsWith("\"", StringComparison.Ordinal))
		{
			return text[1..^1];
		}

		return text;
	}
}
