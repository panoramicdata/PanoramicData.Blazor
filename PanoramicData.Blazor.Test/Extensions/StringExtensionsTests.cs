using AwesomeAssertions;
using PanoramicData.Blazor.Extensions;
using PanoramicData.Blazor.Models;

namespace PanoramicData.Blazor.Test.Extensions;

/// <summary>
/// Tests for the shortcut helpers of <see cref="PanoramicData.Blazor.Extensions.StringExtensions"/> that the original <c>StringExtensionsTests</c>
/// leave uncovered.
/// </summary>
public class StringExtensionsTests
{
	/// <summary>A value filter text would split, or read as a <c>#</c> enclosure, is quoted; any other is left as it is.</summary>
	[Theory]
	[InlineData("C#", "\"C#\"")]
	[InlineData("#Build", "\"#Build\"")]
	[InlineData("Ready Now", "\"Ready Now\"")]
	[InlineData("Plain", "Plain")]
	[InlineData("\"Ready Now\"", "\"Ready Now\"")]
	[InlineData("#2026-01-01 10:00#", "#2026-01-01 10:00#")]
	public void QuoteForFilter_QuotesWhatFilterTextWouldMisread(string text, string expected)
		=> text.QuoteForFilter().Should().Be(expected);

	/// <summary>The shortcut is appended in brackets, with the highlight marker removed from the text.</summary>
	[Fact]
	public void AppendShortcut_AppendsShortcutText()
	{
		var shortcut = new ShortcutKey { CtrlKey = true, Code = "KeyS" };

		"&&Save".AppendShortcut(shortcut).Should().Be("Save (Ctrl-S)");
	}

	/// <summary>Without a shortcut, or without text, the text is returned unchanged.</summary>
	[Fact]
	public void AppendShortcut_NoShortcutOrText_ReturnsText()
	{
		"&&Save".AppendShortcut(new ShortcutKey()).Should().Be("&&Save");
		string.Empty.AppendShortcut(new ShortcutKey { Key = "a" }).Should().BeEmpty();
	}

	/// <summary>Empty text produces empty markup.</summary>
	[Fact]
	public void GetShortcutMarkup_Empty_IsEmpty()
	{
		string.Empty.GetShortcutMarkup().Value.Should().BeEmpty();
	}
}
