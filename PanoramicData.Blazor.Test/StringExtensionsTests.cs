using PanoramicData.Blazor.Extensions;
using Shouldly;

namespace PanoramicData.Blazor.Test;

/// <summary>Tests for the StringExtensions extension methods.</summary>
public class StringExtensionsTests
{
    /// <summary>Verifies that ReplacePathPrefix substitutes the old prefix with the new prefix in the path.</summary>
    [Theory]
    [InlineData("/folder1/file.txt", "/folder1", "/folder2", "/folder2/file.txt")]
    [InlineData("/folder1", "/folder1", "/folder2", "/folder2")]
    [InlineData("/other/file.txt", "/folder1", "/folder2", "/other/file.txt")]
    [InlineData("/folder1/sub/file.txt", "/folder1", "/newroot", "/newroot/sub/file.txt")]
    public void WhenReplacingPathPrefixThenReturnsExpected(string path, string oldPrefix, string newPrefix, string expected)
    {
        path.ReplacePathPrefix(oldPrefix, newPrefix).ShouldBe(expected);
    }

    /// <summary>Verifies that In returns true when the value is in the list and false when it is not.</summary>
    [Theory]
    [InlineData("hello", new[] { "hello", "world" }, true)]
    [InlineData("missing", new[] { "hello", "world" }, false)]
    public void WhenCheckingInThenReturnsCorrectResult(string value, string[] list, bool expected)
    {
        value.In(list).ShouldBe(expected);
    }

    /// <summary>Verifies that LowerFirstChar converts the first character of the string to lowercase.</summary>
    [Fact]
    public void WhenLowerFirstCharThenFirstCharIsLowered()
    {
        "Hello".LowerFirstChar().ShouldBe("hello");
    }

    /// <summary>Verifies that UpperFirstChar converts the first character of the string to uppercase.</summary>
    [Fact]
    public void WhenUpperFirstCharThenFirstCharIsUppered()
    {
        "hello".UpperFirstChar().ShouldBe("Hello");
    }

    /// <summary>Verifies that QuoteIfContainsWhitespace wraps the string in double quotes when it contains whitespace.</summary>
    [Theory]
    [InlineData("", "")]
    [InlineData("nowhitespace", "nowhitespace")]
    [InlineData("has spaces", "\"has spaces\"")]
    [InlineData("has\ttab", "\"has\ttab\"")]
    public void WhenQuoteIfContainsWhitespaceThenReturnsExpected(string input, string expected)
    {
        input.QuoteIfContainsWhitespace().ShouldBe(expected);
    }

    /// <summary>Verifies that RemoveQuotes strips surrounding double quotes from a quoted string.</summary>
    [Theory]
    [InlineData("\"quoted\"", "quoted")]
    [InlineData("unquoted", "unquoted")]
    [InlineData("\"\"", "")]
    public void WhenRemoveQuotesThenReturnsExpected(string input, string expected)
    {
        input.RemoveQuotes().ShouldBe(expected);
    }

    /// <summary>Verifies that ExtractAlphanumericChars removes all non-alphanumeric characters from the string.</summary>
    [Theory]
    [InlineData("abc123!@#def456", "abc123def456")]
    [InlineData("", "")]
    [InlineData("   ", "   ")] // IsNullOrWhiteSpace returns as-is
    public void WhenExtractAlphanumericCharsThenReturnsOnlyAlphanumeric(string input, string expected)
    {
        input.ExtractAlphanumericChars().ShouldBe(expected);
    }

    /// <summary>Verifies that GetShortcutMarkup wraps the character after a double ampersand in an underline HTML tag.</summary>
    [Fact]
    public void WhenGetShortcutMarkupWithDoubleAmpersandThenUnderlines()
    {
        var result = "&&File".GetShortcutMarkup();

        result.Value.ShouldContain("<u>F</u>");
    }

    /// <summary>Verifies that GetShortcutMarkup returns the original string unchanged when no double ampersand is present.</summary>
    [Fact]
    public void WhenGetShortcutMarkupWithNoAmpersandThenReturnsOriginal()
    {
        var result = "File".GetShortcutMarkup();

        result.Value.ShouldBe("File");
    }
}
