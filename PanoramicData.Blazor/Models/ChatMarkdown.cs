using Markdig;
using Markdig.Renderers.Html;
using Markdig.Syntax;
using Markdig.Syntax.Inlines;
using MarkdownHtmlRenderer = Markdig.Renderers.HtmlRenderer;

namespace PanoramicData.Blazor.Models;

/// <summary>
/// Renders a chat message's markdown as HTML that cannot load content, run script or embed another page.
/// </summary>
/// <remarks>
/// Raw HTML is shown as text, attribute blocks and media embeds are not enabled, an image is shown as a link to
/// its address rather than loaded, and a link whose address is not http, https, mailto or tel is shown as text.
/// </remarks>
public static class ChatMarkdown
{
	private static readonly string[] _safeSchemes = ["http", "https", "mailto", "tel"];

	// The advanced extensions, less generic attributes and media links, plus the presentation extensions chat uses.
	private static readonly MarkdownPipeline _pipeline = new MarkdownPipelineBuilder()
		.ConfigureNewLine("\n")
		.DisableHtml()
		.UseAbbreviations()
		.UseAlertBlocks()
		.UseAutoIdentifiers()
		.UseAutoLinks()
		.UseBootstrap()
		.UseCitations()
		.UseCustomContainers()
		.UseDefinitionLists()
		.UseDiagrams()
		.UseEmojiAndSmiley(true)
		.UseEmphasisExtras()
		.UseFigures()
		.UseFooters()
		.UseFootnotes()
		.UseGridTables()
		.UseListExtras()
		.UseMathematics()
		.UsePipeTables()
		.UseReferralLinks("nofollow")
		.UseTaskLists()
		.Build();

	/// <summary>
	/// Renders <paramref name="markdown"/> as safe HTML; empty for no content.
	/// </summary>
	public static string ToHtml(string? markdown)
	{
		if (string.IsNullOrWhiteSpace(markdown))
		{
			return string.Empty;
		}

		var document = Markdown.Parse(markdown, _pipeline);
		MakeSafe(document);

		using var writer = new StringWriter();
		var renderer = new MarkdownHtmlRenderer(writer);
		_pipeline.Setup(renderer);
		renderer.Render(document);
		writer.Flush();
		return writer.ToString();
	}

	/// <summary>
	/// Whether a link to <paramref name="address"/> is safe to follow: relative, or http, https, mailto or tel.
	/// </summary>
	internal static bool IsSafeAddress(string? address)
	{
		// Browsers drop whitespace and control characters inside a scheme, so "java\tscript:" is still script.
		var compact = new string([.. (address ?? string.Empty).Where(character => !char.IsWhiteSpace(character) && !char.IsControl(character))]);
		var colon = compact.IndexOf(':', StringComparison.Ordinal);
		var pathStart = compact.IndexOfAny(['/', '?', '#']);

		return colon < 0
			|| (pathStart >= 0 && pathStart < colon)
			|| _safeSchemes.Contains(compact[..colon], StringComparer.OrdinalIgnoreCase);
	}

	private static void MakeSafe(MarkdownDocument document)
	{
		foreach (var image in document.Descendants<LinkInline>().Where(link => link.IsImage).ToList())
		{
			ShowAsLink(image);
		}

		foreach (var link in document.Descendants<LinkInline>().Where(link => !IsSafeAddress(link.Url)).ToList())
		{
			Unwrap(link);
		}

		foreach (var autolink in document.Descendants<AutolinkInline>().Where(autolink => !IsSafeAddress(autolink.Url)).ToList())
		{
			autolink.ReplaceBy(new LiteralInline(autolink.Url));
		}
	}

	private static void ShowAsLink(LinkInline image)
	{
		var address = image.Url ?? string.Empty;
		image.IsImage = false;
		image.TryGetAttributes()?.Classes?.Remove("img-fluid");

		if (image.FirstChild is null)
		{
			image.AppendChild(new LiteralInline(address));
		}
		else
		{
			image.InsertAfter(new LiteralInline($" ({address})"));
		}
	}

	private static void Unwrap(LinkInline link)
	{
		if (link.FirstChild is null)
		{
			link.InsertBefore(new LiteralInline(link.Url ?? string.Empty));
		}

		var child = link.FirstChild;
		while (child is not null)
		{
			var next = child.NextSibling;
			child.Remove();
			link.InsertBefore(child);
			child = next;
		}

		link.Remove();
	}
}
