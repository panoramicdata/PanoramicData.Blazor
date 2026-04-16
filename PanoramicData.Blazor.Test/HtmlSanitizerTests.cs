using PanoramicData.Blazor.Extensions;
using Shouldly;

namespace PanoramicData.Blazor.Test;

public class HtmlSanitizerTests
{
    [Fact]
    public void WhenNullInputThenReturnsEmpty()
    {
        HtmlSanitizer.Sanitize(null).ShouldBe(string.Empty);
    }

    [Fact]
    public void WhenEmptyInputThenReturnsEmpty()
    {
        HtmlSanitizer.Sanitize("").ShouldBe(string.Empty);
    }

    [Fact]
    public void WhenPlainTextThenReturnsUnchanged()
    {
        HtmlSanitizer.Sanitize("Hello world").ShouldBe("Hello world");
    }

    [Fact]
    public void WhenScriptTagThenRemovesIt()
    {
        var result = HtmlSanitizer.Sanitize("<p>Hello</p><script>alert('xss')</script>");

        result.ShouldNotContain("<script");
        result.ShouldNotContain("alert");
        result.ShouldContain("<p>Hello</p>");
    }

    [Fact]
    public void WhenEventHandlerAttributeThenRemovesIt()
    {
        var result = HtmlSanitizer.Sanitize("<div onclick=\"alert('xss')\">content</div>");

        result.ShouldNotContain("onclick");
        result.ShouldNotContain("alert");
    }

    [Fact]
    public void WhenJavascriptUrlThenRemovesIt()
    {
        var result = HtmlSanitizer.Sanitize("<a href=\"javascript:alert('xss')\">click</a>");

        result.ShouldNotContain("javascript:");
    }

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
