using AwesomeAssertions;
using PanoramicData.Blazor.Models;

namespace PanoramicData.Blazor.Test.Models;

/// <summary>
/// Tests for the few branches of <see cref="Filter"/>, <see cref="PageCriteria"/>, <see cref="ShortcutKey"/>,
/// <see cref="FileExplorerItem"/> and <see cref="PartialMarkdown"/> that their original test classes leave uncovered.
/// </summary>
public class SmallModelGapTests
{
	/// <summary>A filter of an undefined type has no text form.</summary>
	[Fact]
	public void Filter_ToString_UndefinedType_IsEmpty()
	{
		new Filter((FilterTypes)99, "Name", "x").ToString().Should().BeEmpty();
	}

	/// <summary>Setting the page size to its current value raises no change event.</summary>
	[Fact]
	public void PageCriteria_SamePageSize_RaisesNothing()
	{
		var criteria = new PageCriteria(1, 25, 100);
		var raised = 0;
		criteria.PageSizeChanged += (_, _) => raised++;

		criteria.PageSize = 25;

		raised.Should().Be(0);
		criteria.PageSize.Should().Be(25u);
	}

	/// <summary>Shortcut text lists each modifier in turn, then the key.</summary>
	[Fact]
	public void ShortcutKey_ToString_ListsAllModifiers()
	{
		new ShortcutKey { CtrlKey = true, ShiftKey = true, AltKey = true, Code = "KeyA" }.ToString().Should().Be("Ctrl-Shift-Alt-A");
	}

	/// <summary>A code that is neither a letter nor a digit is shown upper-cased as it is.</summary>
	[Fact]
	public void ShortcutKey_ToString_OtherCode_IsUpperCased()
	{
		new ShortcutKey { ShiftKey = true, Code = "Escape" }.ToString().Should().Be("Shift-ESCAPE");
	}

	/// <summary>A null path has no name.</summary>
	[Fact]
	public void FileExplorerItem_GetNameFromPath_Null_IsEmpty()
	{
		FileExplorerItem.GetNameFromPath(null).Should().BeEmpty();
	}

	/// <summary>Blank markdown renders to nothing, and markdown renders to HTML with the advanced extensions.</summary>
	[Fact]
	public void PartialMarkdown_ToHtml_RendersMarkdown()
	{
		PartialMarkdown.ToHtml(null).Should().BeEmpty();
		PartialMarkdown.ToHtml("  ").Should().BeEmpty();
		PartialMarkdown.ToHtml("**bold**").Should().Contain("<strong>bold</strong>");
		PartialMarkdown.ToHtml("| a |\n|---|\n| 1 |").Should().Contain("<table>");
	}
}
