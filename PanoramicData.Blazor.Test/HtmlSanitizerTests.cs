using PanoramicData.Blazor.Extensions;
using Shouldly;

namespace PanoramicData.Blazor.Test;

/// <summary>Tests for the HtmlSanitizer class.</summary>
public class HtmlSanitizerTests
{
    /// <summary>Verifies that null input returns an empty string.</summary>
    [Fact]
    public void WhenNullInputThenReturnsEmpty()
    {
        HtmlSanitizer.Sanitize(null).ShouldBe(string.Empty);
    }

    /// <summary>Verifies that an empty string input returns an empty string.</summary>
    [Fact]
    public void WhenEmptyInputThenReturnsEmpty()
    {
        HtmlSanitizer.Sanitize("").ShouldBe(string.Empty);
    }

    /// <summary>Verifies that plain text with no HTML tags is returned unchanged.</summary>
    [Fact]
    public void WhenPlainTextThenReturnsUnchanged()
    {
        HtmlSanitizer.Sanitize("Hello world").ShouldBe("Hello world");
    }

    /// <summary>Verifies that script tags and their content are removed from the sanitized output.</summary>
    [Fact]
    public void WhenScriptTagThenRemovesIt()
    {
        var result = HtmlSanitizer.Sanitize("<p>Hello</p><script>alert('xss')</script>");

        result.ShouldNotContain("<script");
        result.ShouldNotContain("alert");
        result.ShouldContain("<p>Hello</p>");
    }

    /// <summary>Verifies that event handler attributes such as onclick are removed from the sanitized output.</summary>
    [Fact]
    public void WhenEventHandlerAttributeThenRemovesIt()
    {
        var result = HtmlSanitizer.Sanitize("<div onclick=\"alert('xss')\">content</div>");

        result.ShouldNotContain("onclick");
        result.ShouldNotContain("alert");
    }

    /// <summary>Verifies that javascript: URLs in href attributes are removed.</summary>
    [Fact]
    public void WhenJavascriptUrlThenRemovesIt()
    {
        var result = HtmlSanitizer.Sanitize("<a href=\"javascript:alert('xss')\">click</a>");

        result.ShouldNotContain("javascript:");
    }

    /// <summary>Verifies that safe HTML tags such as p, strong, and em are preserved in the sanitized output.</summary>
    [Fact]
    public void WhenAllowedTagsThenPreservesThem()
    {
        var html = "<p>Text <strong>bold</strong> <em>italic</em></p>";

        var result = HtmlSanitizer.Sanitize(html);

        result.ShouldContain("<p>");
        result.ShouldContain("<strong>");
        result.ShouldContain("<em>");
    }
}
