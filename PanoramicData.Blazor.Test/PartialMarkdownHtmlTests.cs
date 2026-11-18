using AwesomeAssertions;
using PanoramicData.Blazor.Models;

namespace PanoramicData.Blazor.Test;

/// <summary>
/// What a streamed chat reply puts into the page once its markdown is rendered as HTML.
/// </summary>
public class PartialMarkdownHtmlTests
{
	/// <summary>Verifies that an image is shown as a link to its address and never loaded.</summary>
	[Fact]
	public void An_image_is_shown_as_a_link_and_not_loaded()
		=> PartialMarkdown.ToHtml("![x](https://evil.example/c?d=1)").Should()
			.NotContain("<img")
			.And.Contain("<a href=\"https://evil.example/c?d=1\"")
			.And.Contain("(https://evil.example/c?d=1)");

	/// <summary>Verifies that raw HTML is shown as text.</summary>
	[Fact]
	public void Raw_html_is_shown_as_text()
		=> PartialMarkdown.ToHtml("Hello <img src=x onerror=\"alert(1)\"> there").Should()
			.NotContain("<img")
			.And.Contain("&lt;img src=x onerror=");

	/// <summary>Verifies that an attribute block adds no attribute.</summary>
	[Fact]
	public void An_attribute_block_adds_no_attribute()
		=> PartialMarkdown.ToHtml("![x](https://evil.example/a.png){onerror=alert(1)}").Should()
			.NotContain("<img")
			.And.NotContain("onerror=\"");

	/// <summary>Verifies that an iframe is shown as text.</summary>
	[Fact]
	public void An_iframe_is_shown_as_text()
		=> PartialMarkdown.ToHtml("<iframe src=\"https://evil.example/\"></iframe>").Should()
			.NotContain("<iframe")
			.And.Contain("&lt;iframe");

	/// <summary>Verifies that a video address is shown as a link, not embedded.</summary>
	[Fact]
	public void A_video_address_is_shown_as_a_link()
		=> PartialMarkdown.ToHtml("![video](https://www.youtube.com/watch?v=abc)").Should()
			.NotContain("<iframe")
			.And.Contain("<a href=\"https://www.youtube.com/watch?v=abc\"");

	/// <summary>Verifies that a link to a script address keeps its text and loses its address.</summary>
	[Fact]
	public void A_link_to_a_script_address_is_shown_as_text()
		=> PartialMarkdown.ToHtml("[click](javascript:alert(1))").Should()
			.NotContain("href=")
			.And.Contain("click");

	/// <summary>Verifies that a reference-style image is shown as a link.</summary>
	[Fact]
	public void A_reference_style_image_is_shown_as_a_link()
		=> PartialMarkdown.ToHtml("![x][r]\n\n[r]: https://evil.example/r.png").Should()
			.NotContain("<img")
			.And.Contain("<a href=\"https://evil.example/r.png\"");

	/// <summary>Verifies that a protocol-relative image is shown as a link.</summary>
	[Fact]
	public void A_protocol_relative_image_is_shown_as_a_link()
		=> PartialMarkdown.ToHtml("![x](//evil.example/p.png)").Should()
			.NotContain("<img")
			.And.Contain("<a href=\"//evil.example/p.png\"");

	/// <summary>Verifies that an image in a table cell is shown as a link.</summary>
	[Fact]
	public void An_image_in_a_table_cell_is_shown_as_a_link()
		=> PartialMarkdown.ToHtml("| a |\n|---|\n| ![x](https://evil.example/t.png) |").Should()
			.NotContain("<img")
			.And.Contain("<td><a href=\"https://evil.example/t.png\"");

	/// <summary>Verifies that ordinary links and bare addresses still become links.</summary>
	[Fact]
	public void Ordinary_links_keep_their_address()
	{
		PartialMarkdown.ToHtml("Visit https://www.example.com/page").Should().Contain("<a href=\"https://www.example.com/page\"");
		PartialMarkdown.ToHtml("<https://www.example.com/auto>").Should().Contain("<a href=\"https://www.example.com/auto\"");
		PartialMarkdown.ToHtml("[the help](https://www.example.com/help)").Should().Contain("<a href=\"https://www.example.com/help\"");
	}

	/// <summary>Verifies that ordinary markdown still renders.</summary>
	[Fact]
	public void Ordinary_markdown_still_renders()
	{
		var html = PartialMarkdown.ToHtml("**Bold**, `code` and a table:\n\n| a | b |\n|---|---|\n| 1 | 2 |");

		html.Should().Contain("<strong>Bold</strong>").And.Contain("<code>code</code>").And.Contain("<table");
	}
}
