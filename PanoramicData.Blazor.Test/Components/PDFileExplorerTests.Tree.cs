using AwesomeAssertions;
using Bunit;
using PanoramicData.Blazor.Models;

namespace PanoramicData.Blazor.Test.Components;

/// <summary>
/// Tree pane behaviour of <see cref="PDFileExplorer"/>: filtering, node rendering, the tree context menu,
/// keyboard shortcuts, renaming and folder deletion.
/// </summary>
public partial class PDFileExplorerTests
{
	private static TreeNode<FileExplorerItem> Node(IRenderedComponent<PDFileExplorer> cut, string path)
		=> Tree(cut).Instance.RootNode.Find(path) ?? throw new InvalidOperationException($"No node '{path}'.");

	/// <summary>The tree lists folders only, and excluded paths are left out of both panes.</summary>
	[Fact]
	public void Tree_ListsFoldersOnly_AndHonoursExcludedPaths()
	{
		var cut = RenderExplorer(p => p.Add(x => x.ExcludedPaths, ["/Empty"]));

		Node(cut, "/").Nodes!.Select(x => x.Key).Should().Equal("/Docs", "/Media");
		RowNames(cut).Should().Equal("Docs", "Media");
	}

	/// <summary>Sub-folders are sorted by the supplied sort function when one is given.</summary>
	[Fact]
	public void TreeSort_IsUsedForSiblings()
	{
		var cut = RenderExplorer(p => p.Add(x => x.TreeSort, (a, b) => string.Compare(b.Name, a.Name, StringComparison.Ordinal)));

		Node(cut, "/").Nodes!.Select(x => x.Key).Should().Equal("/Media", "/Empty", "/Docs");
	}

	/// <summary>A drop is only valid onto a folder that accepts new items.</summary>
	[Fact]
	public void IsDropValid_RequiresAFolderThatAcceptsItems()
	{
		var cut = RenderExplorer();
		var isDropValid = Tree(cut).Instance.IsDropValid!;

		isDropValid(_provider.Get("/Docs"), null).Should().BeTrue();
		isDropValid(_provider.Get("/Media"), null).Should().BeFalse();
	}

	/// <summary>Read-only folders show the text postfix after the name by default.</summary>
	[Fact]
	public void TreeNode_ReadOnly_ShowsPostfixAfterName()
	{
		var cut = RenderExplorer();

		var media = cut.FindAll("span.item-text").Single(x => x.TextContent.Contains("Media", StringComparison.Ordinal));
		media.QuerySelector(".pdfe-readonly-text.pdfe-readonly-after")!.TextContent.Should().Be("(ro)");
	}

	/// <summary>A read-only icon class replaces the postfix, and can be placed before the name.</summary>
	[Fact]
	public void TreeNode_ReadOnlyIcon_CanBePlacedBefore()
	{
		var cut = RenderExplorer(p => p
			.Add(x => x.ReadOnlyIconClass, "fa fa-lock")
			.Add(x => x.ReadOnlyIndicatorPosition, ReadOnlyIndicatorPosition.Before));

		var media = cut.FindAll("span.item-text").Single(x => x.TextContent.Contains("Media", StringComparison.Ordinal));
		media.QuerySelector("i.pdfe-readonly-icon.pdfe-readonly-before.fa-lock").Should().NotBeNull();
		cut.Instance.UseReadOnlyIcon().Should().BeTrue();
	}

	/// <summary>With the text postfix placed before the name, it precedes the name.</summary>
	[Fact]
	public void TreeNode_ReadOnlyText_CanBePlacedBefore()
	{
		var cut = RenderExplorer(p => p
			.Add(x => x.ReadOnlyPostfix, "[locked]")
			.Add(x => x.ReadOnlyIndicatorPosition, ReadOnlyIndicatorPosition.Before));

		cut.FindAll(".pdfe-readonly-text.pdfe-readonly-before").Select(x => x.TextContent).Should().Contain("[locked]");
	}

	/// <summary>A badge function adds a badge icon with its tooltip to tree nodes and table rows.</summary>
	[Fact]
	public void BadgeFunction_AddsBadges()
	{
		var cut = RenderExplorer(p => p.Add(x => x.GetItemBadgeCssClass, item => item.Name == "Docs" ? new IconInfo { CssCls = "fas fa-star", ToolTip = "Starred" } : null));

		var badges = cut.FindAll("i.pd-badge.fa-star");
		badges.Should().HaveCount(2);
		badges[0].GetAttribute("title").Should().Be("Starred");
	}
}
