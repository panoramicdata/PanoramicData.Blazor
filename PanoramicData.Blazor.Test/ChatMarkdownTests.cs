using AwesomeAssertions;
using PanoramicData.Blazor.Models;

namespace PanoramicData.Blazor.Test;

/// <summary>
/// Tests for the safe rendering of a chat message's markdown.
/// </summary>
public class ChatMarkdownTests
{
	/// <summary>Verifies which link addresses are safe to follow.</summary>
	[Theory]
	[InlineData("https://www.example.com/x", true)]
	[InlineData("http://www.example.com", true)]
	[InlineData("mailto:someone@example.com", true)]
	[InlineData("tel:+441234567890", true)]
	[InlineData("/relative/path", true)]
	[InlineData("page#section", true)]
	[InlineData("//www.example.com/x", true)]
	[InlineData("page?q=a:b", true)]
	[InlineData("", true)]
	[InlineData("javascript:alert(1)", false)]
	[InlineData("JavaScript:alert(1)", false)]
	[InlineData(" javascript:alert(1)", false)]
	[InlineData("java\tscript:alert(1)", false)]
	[InlineData("java\nscript:alert(1)", false)]
	[InlineData("vbscript:msgbox(1)", false)]
	[InlineData("data:text/html;base64,AAAA", false)]
	[InlineData("file:///c:/secret", false)]
	public void A_link_address_is_safe_only_when_relative_or_web_mail_or_phone(string address, bool expected)
		=> ChatMarkdown.IsSafeAddress(address).Should().Be(expected);

	/// <summary>Verifies that no content renders as nothing.</summary>
	[Fact]
	public void No_content_renders_as_nothing()
	{
		ChatMarkdown.ToHtml(null).Should().BeEmpty();
		ChatMarkdown.ToHtml("   ").Should().BeEmpty();
	}

	/// <summary>Verifies that an autolink to a script address is shown as text.</summary>
	[Fact]
	public void An_autolink_to_a_script_address_is_shown_as_text()
		=> ChatMarkdown.ToHtml("<javascript:alert(1)>").Should()
			.NotContain("href=")
			.And.Contain("javascript:alert(1)");

	/// <summary>Verifies that a script address written with a character reference is still refused.</summary>
	[Fact]
	public void An_encoded_script_address_is_shown_as_text()
		=> ChatMarkdown.ToHtml("[click](javascript&#58;alert(1))").Should().NotContain("href=");

	/// <summary>Verifies that an image with a script address is shown as text, address included.</summary>
	[Fact]
	public void An_image_with_a_script_address_is_shown_as_text()
		=> ChatMarkdown.ToHtml("![x](javascript:alert(1))").Should()
			.NotContain("href=")
			.And.NotContain("<img")
			.And.Contain("x (javascript:alert(1))");

	/// <summary>Verifies that an image with no text shows its address as the link text.</summary>
	[Fact]
	public void An_image_with_no_text_shows_its_address()
		=> ChatMarkdown.ToHtml("![](https://evil.example/n.png)").Should()
			.NotContain("<img")
			.And.Contain(">https://evil.example/n.png</a>");

	/// <summary>Verifies that an image held in the message itself is not loaded either.</summary>
	[Fact]
	public void A_data_image_is_not_loaded()
		=> ChatMarkdown.ToHtml("![x](data:image/png;base64,AAAA)").Should()
			.NotContain("<img")
			.And.NotContain("href=\"data:");

	/// <summary>Verifies that an image shown as a link keeps no image styling.</summary>
	[Fact]
	public void An_image_shown_as_a_link_has_no_image_class()
		=> ChatMarkdown.ToHtml("![x](https://evil.example/c.png)").Should().NotContain("img-fluid");

	/// <summary>Verifies that a reply still being streamed never loads an image.</summary>
	[Fact]
	public void A_half_streamed_reply_never_loads_an_image()
	{
		var partial = PartialMarkdown.Trim("Here it is:\n\n![x](https://evil.example/s.png)\n\n| a |\n|---");

		PartialMarkdown.ToHtml(partial.Content).Should().NotContain("<img");
	}
}
