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
