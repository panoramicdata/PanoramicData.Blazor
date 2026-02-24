using System.Net;
using System.Text.RegularExpressions;

namespace PanoramicData.Blazor.Extensions;

/// <summary>
/// Provides basic HTML sanitization to prevent XSS attacks.
/// </summary>
public static partial class HtmlSanitizer
{
	private static readonly HashSet<string> _allowedTags =
	[
		"a", "abbr", "b", "blockquote", "br", "caption", "cite", "code",
		"col", "colgroup", "dd", "del", "details", "dfn", "div", "dl", "dt",
		"em", "figcaption", "figure", "h1", "h2", "h3", "h4", "h5", "h6",
		"hr", "i", "img", "ins", "kbd", "li", "mark", "ol", "p", "pre",
		"q", "rp", "rt", "ruby", "s", "samp", "small", "span", "strong",
		"sub", "summary", "sup", "table", "tbody", "td", "tfoot", "th",
		"thead", "time", "tr", "u", "ul", "var", "wbr"
	];

	private static readonly HashSet<string> _allowedAttributes =
	[
		"href", "src", "alt", "title", "class", "style", "width", "height",
		"colspan", "rowspan", "target", "rel", "id", "name", "datetime"
	];

	/// <summary>
	/// Sanitizes HTML content by removing potentially dangerous tags and attributes.
	/// </summary>
	/// <param name="html">The HTML string to sanitize.</param>
	/// <returns>Sanitized HTML string.</returns>
	public static string Sanitize(string? html)
	{
		if (string.IsNullOrWhiteSpace(html))
		{
			return string.Empty;
		}

		// Remove script tags and their content
		html = ScriptTagRegex().Replace(html, string.Empty);

		// Remove event handler attributes
		html = EventHandlerRegex().Replace(html, "$1");

		// Remove javascript: URLs
		html = JavaScriptUrlRegex().Replace(html, "$1\"\"");

		// Remove disallowed tags but keep their content
		html = DisallowedTagRegex().Replace(html, match =>
		{
			var tagName = match.Groups[1].Value.ToLowerInvariant();
			return _allowedTags.Contains(tagName) ? match.Value : string.Empty;
		});

		return html;
	}

	[GeneratedRegex(@"<script\b[^<]*(?:(?!</script>)<[^<]*)*</script>", RegexOptions.IgnoreCase | RegexOptions.Singleline)]
	private static partial Regex ScriptTagRegex();

	[GeneratedRegex(@"(<[^>]*)\s+on\w+\s*=\s*([""'][^""']*[""']|[^\s>]+)", RegexOptions.IgnoreCase)]
	private static partial Regex EventHandlerRegex();

	[GeneratedRegex(@"(<[^>]*(?:href|src)\s*=\s*)([""'])javascript:[^""']*\2", RegexOptions.IgnoreCase)]
	private static partial Regex JavaScriptUrlRegex();

	[GeneratedRegex(@"</?(\w+)[^>]*>", RegexOptions.IgnoreCase)]
	private static partial Regex DisallowedTagRegex();
}
