namespace PanoramicData.Blazor.Extensions;

/// <summary>
/// Extension methods for <see cref="string"/> values: menu shortcut text.
/// </summary>
public static partial class StringExtensions
{
	// Marks the character to highlight as a shortcut, e.g. "&&File"
	private const string ShortcutMarker = "&&";

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

		return $"{text.Replace(ShortcutMarker, "")} ({shortcutKey})";
	}

	/// <summary>
	/// Returns a markup string that highlight (underline) the shortcut key.
	/// </summary>
	/// <param name="text">The text containing a double ampersand (&amp;&amp;) before the character to highlight.</param>
	/// <returns>A new MarkupString instance containing the markup text.</returns>
	public static MarkupString GetShortcutMarkup(this string text)
	{
		if (string.IsNullOrEmpty(text))
		{
			return (MarkupString)text;
		}

		var ampIdx = text.IndexOf(ShortcutMarker, StringComparison.Ordinal);
		if (ampIdx == -1)
		{
			return (MarkupString)text;
		}

		var sb = new StringBuilder();
		sb.Append("<span>")
			.Append(text[..ampIdx])
			.Append("<u>")
			.Append(text.AsSpan(ampIdx + ShortcutMarker.Length, 1))
			.Append("</u>")
			.Append(text[(ampIdx + ShortcutMarker.Length + 1)..])
			.Append("</span>");
		return (MarkupString)sb.ToString();
	}
}
