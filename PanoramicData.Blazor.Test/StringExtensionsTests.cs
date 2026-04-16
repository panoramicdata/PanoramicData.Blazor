using PanoramicData.Blazor.Extensions;
using Shouldly;

namespace PanoramicData.Blazor.Test;

public class StringExtensionsTests
{
    [Theory]
    [InlineData("/folder1/file.txt", "/folder1", "/folder2", "/folder2/file.txt")]
    [InlineData("/folder1", "/folder1", "/folder2", "/folder2")]
    [InlineData("/other/file.txt", "/folder1", "/folder2", "/other/file.txt")]
    [InlineData("/folder1/sub/file.txt", "/folder1", "/newroot", "/newroot/sub/file.txt")]
    public void WhenReplacingPathPrefixThenReturnsExpected(string path, string oldPrefix, string newPrefix, string expected)
    {
        path.ReplacePathPrefix(oldPrefix, newPrefix).ShouldBe(expected);
    }

    [Theory]
    [InlineData("hello", new[] { "hello", "world" }, true)]
    [InlineData("missing", new[] { "hello", "world" }, false)]
    public void WhenCheckingInThenReturnsCorrectResult(string value, string[] list, bool expected)
    {
        value.In(list).ShouldBe(expected);
    }

    [Fact]
    public void WhenLowerFirstCharThenFirstCharIsLowered()
    {
        "Hello".LowerFirstChar().ShouldBe("hello");
    }

    [Fact]
    public void WhenUpperFirstCharThenFirstCharIsUppered()
    {
        "hello".UpperFirstChar().ShouldBe("Hello");
    }

    [Theory]
    [InlineData("", "")]
    [InlineData("nowhitespace", "nowhitespace")]
    [InlineData("has spaces", "\"has spaces\"")]
    [InlineData("has\ttab", "\"has\ttab\"")]
    public void WhenQuoteIfContainsWhitespaceThenReturnsExpected(string input, string expected)
    {
        input.QuoteIfContainsWhitespace().ShouldBe(expected);
    }

    [Theory]
    [InlineData("\"quoted\"", "quoted")]
    [InlineData("unquoted", "unquoted")]
    [InlineData("\"\"", "")]
    public void WhenRemoveQuotesThenReturnsExpected(string input, string expected)
    {
        input.RemoveQuotes().ShouldBe(expected);
    }

    [Theory]
    [InlineData("abc123!@#def456", "abc123def456")]
    [InlineData("", "")]
    [InlineData("   ", "   ")] // IsNullOrWhiteSpace returns as-is
    public void WhenExtractAlphanumericCharsThenReturnsOnlyAlphanumeric(string input, string expected)
    {
        input.ExtractAlphanumericChars().ShouldBe(expected);
    }

    [Fact]
    public void WhenGetShortcutMarkupWithDoubleAmpersandThenUnderlines()
    {
        var result = "&&File".GetShortcutMarkup();

        result.Value.ShouldContain("<u>F</u>");
    }

    [Fact]
    public void WhenGetShortcutMarkupWithNoAmpersandThenReturnsOriginal()
    {
        var result = "File".GetShortcutMarkup();

        result.Value.ShouldBe("File");
    }
}
