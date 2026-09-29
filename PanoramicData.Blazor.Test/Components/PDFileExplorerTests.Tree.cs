using AwesomeAssertions;
using Bunit;
using PanoramicData.Blazor.Arguments;
using PanoramicData.Blazor.Exceptions;
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

	private static MenuItem MenuEntry(IRenderedComponent<PDContextMenu> menu, string text)
		=> menu.Instance.Items.First(x => x.Text == text);

	private static async Task UpdateMenuAsync(IRenderedComponent<PDContextMenu> menu, ElementInfo? source = null)
	{
		var args = new MenuItemsEventArgs(menu.Instance, menu.Instance.Items) { SourceElement = source };
		await menu.InvokeAsync(() => menu.Instance.UpdateState.InvokeAsync(args));
	}

	private static async Task ClickMenuAsync(IRenderedComponent<PDContextMenu> menu, string text)
	{
		var item = MenuEntry(menu, text);
		await menu.InvokeAsync(() => menu.Instance.ClickHandler(item));
	}

	private static string[] VisibleMenuTexts(IRenderedComponent<PDContextMenu> menu)
		=> [.. menu.Instance.Items.Where(x => x.IsVisible && !x.IsSeparator).Select(x => x.Text)];

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

	/// <summary>At the root, the tree context menu offers only New Folder, and the application is given the folder.</summary>
	[Fact]
	public async Task TreeMenu_AtRoot_OffersOnlyNewFolder()
	{
		object? context = null;
		var cut = RenderExplorer(p => p.Add(x => x.UpdateTreeContextState, (MenuItemsEventArgs a) => context = a.Context));
		var menu = TreeMenu(cut);

		await UpdateMenuAsync(menu);

		VisibleMenuTexts(menu).Should().Equal("New Folder");
		((FileExplorerItem)context!).Path.Should().Be("/");
	}

	/// <summary>On a sub-folder the tree context menu offers rename, copy, cut and delete.</summary>
	[Fact]
	public async Task TreeMenu_OnSubFolder_OffersEditActions()
	{
		var cut = RenderExplorer();
		await NavigateAsync(cut, "/Docs");
		var menu = TreeMenu(cut);

		await UpdateMenuAsync(menu);

		VisibleMenuTexts(menu).Should().Equal("New Folder", "Rename", "Copy", "Cut", "Delete");
	}

	/// <summary>On a read-only folder, the menu offers no New Folder and no Paste, even with something copied.</summary>
	[Fact]
	public async Task TreeMenu_OnReadOnlyFolder_OffersNoNewFolderOrPaste()
	{
		var cut = RenderExplorer();
		await NavigateAsync(cut, "/Docs");
		await KeyDownTreeAsync(cut, "KeyC", ctrl: true);
		await NavigateAsync(cut, "/Media");
		var menu = TreeMenu(cut);

		await UpdateMenuAsync(menu);

		VisibleMenuTexts(menu).Should().NotContain("New Folder").And.NotContain("Paste");
	}

	/// <summary>With something copied, Paste is offered on a folder that accepts items.</summary>
	[Fact]
	public async Task TreeMenu_WithCopiedItem_OffersPaste()
	{
		var cut = RenderExplorer();
		await NavigateAsync(cut, "/Docs");
		await KeyDownTreeAsync(cut, "KeyC", ctrl: true);
		await NavigateAsync(cut, "/Empty");
		var menu = TreeMenu(cut);

		await UpdateMenuAsync(menu);

		VisibleMenuTexts(menu).Should().Contain("Paste");
	}

	/// <summary>A tree menu click the application cancels does nothing.</summary>
	[Fact]
	public async Task TreeMenuClick_CancelledByApplication_DoesNothing()
	{
		var cut = RenderExplorer(p => p.Add(x => x.TreeContextMenuClick, (MenuItemEventArgs a) => a.Cancel = true));

		await ClickMenuAsync(TreeMenu(cut), "New Folder");

		_provider.Creates.Should().BeEmpty();
	}

	/// <summary>New Folder from the tree creates a uniquely named folder under the selected one and selects it.</summary>
	[Fact]
	public async Task TreeMenu_NewFolder_CreatesUniqueFolder_AndSelectsIt()
	{
		var cut = RenderExplorer();
		await NavigateAsync(cut, "/Docs");
		await ClickMenuAsync(TreeMenu(cut), "New Folder");
		await NavigateAsync(cut, "/Docs");

		await ClickMenuAsync(TreeMenu(cut), "New Folder");

		_provider.Creates.Should().Equal("/Docs/New Folder", "/Docs/New Folder (2)");
		cut.Instance.FolderPath.Should().Be("/Docs/New Folder (2)");
		Tree(cut).Instance.SelectedNode!.IsEditing.Should().BeTrue();
	}

	/// <summary>A folder the provider fails to create is not selected.</summary>
	[Fact]
	public async Task TreeMenu_NewFolder_WhenCreateFails_StaysOnFolder()
	{
		_provider.RefuseCreates();
		var cut = RenderExplorer();
		await NavigateAsync(cut, "/Docs");

		await ClickMenuAsync(TreeMenu(cut), "New Folder");

		cut.Instance.FolderPath.Should().Be("/Docs");
	}

	/// <summary>Rename from the tree puts a renamable sub-folder into edit mode.</summary>
	[Fact]
	public async Task TreeMenu_Rename_BeginsEditing()
	{
		var cut = RenderExplorer();
		await NavigateAsync(cut, "/Docs");

		await ClickMenuAsync(TreeMenu(cut), "Rename");

		Tree(cut).Instance.SelectedNode!.IsEditing.Should().BeTrue();
	}

	/// <summary>The root folder cannot be renamed.</summary>
	[Fact]
	public async Task TreeMenu_Rename_OnRoot_IsRefused()
	{
		var cut = RenderExplorer();

		await ClickMenuAsync(TreeMenu(cut), "Rename");

		Tree(cut).Instance.SelectedNode!.IsEditing.Should().BeFalse();
	}

	/// <summary>Renaming is refused when the application cancels it or renaming is turned off.</summary>
	/// <param name="allowRename">The AllowRename parameter.</param>
	/// <param name="cancel">Whether the BeforeRename handler cancels.</param>
	[Theory]
	[InlineData(true, true)]
	[InlineData(false, false)]
	public async Task TreeRename_IsRefused_WhenCancelledOrDisallowed(bool allowRename, bool cancel)
	{
		RenameArgs? seen = null;
		var cut = RenderExplorer(p => p
			.Add(x => x.AllowRename, allowRename)
			.Add(x => x.BeforeRename, (RenameArgs a) => { seen = a; a.Cancel = cancel; }));
		await NavigateAsync(cut, "/Docs");
		var tree = Tree(cut);
		var args = new TreeNodeBeforeEditEventArgs<FileExplorerItem>(tree.Instance.SelectedNode!);

		await tree.InvokeAsync(() => tree.Instance.BeforeEdit.InvokeAsync(args));

		args.Cancel.Should().BeTrue();
		seen!.Item!.Path.Should().Be("/Docs");
	}

	/// <summary>A folder whose CanRename is false cannot be renamed.</summary>
	[Fact]
	public async Task TreeRename_IsRefused_WhenItemCannotBeRenamed()
	{
		_provider.Get("/Docs").CanRename = false;
		var cut = RenderExplorer();
		await NavigateAsync(cut, "/Docs");
		var tree = Tree(cut);
		var args = new TreeNodeBeforeEditEventArgs<FileExplorerItem>(tree.Instance.SelectedNode!);

		await tree.InvokeAsync(() => tree.Instance.BeforeEdit.InvokeAsync(args));

		args.Cancel.Should().BeTrue();
	}

	/// <summary>A tree rename to a name beginning with a period is refused and reported.</summary>
	[Fact]
	public async Task TreeAfterEdit_LeadingPeriod_IsRefused()
	{
		var cut = RenderExplorer();
		await NavigateAsync(cut, "/Docs/Sub");

		var args = await AfterTreeEditAsync(cut, "Sub", ".Sub");

		args.Cancel.Should().BeTrue();
		_exceptions.Should().ContainSingle().Which.Should().BeOfType<PDFileExplorerException>();
		_provider.Updates.Should().BeEmpty();
	}

	/// <summary>A tree rename to the name of a sibling is refused and reported.</summary>
	[Fact]
	public async Task TreeAfterEdit_SiblingName_IsRefused()
	{
		var cut = RenderExplorer();
		await NavigateAsync(cut, "/Docs");

		var args = await AfterTreeEditAsync(cut, "Docs", "media");

		args.Cancel.Should().BeTrue();
		_exceptions.Should().ContainSingle().Which.Message.Should().Contain("already exists");
	}

	/// <summary>Committing the original name unchanged is refused silently.</summary>
	[Fact]
	public async Task TreeAfterEdit_OriginalName_IsRefusedSilently()
	{
		var cut = RenderExplorer();
		await NavigateAsync(cut, "/Docs");

		var args = await AfterTreeEditAsync(cut, "Docs", "Docs");

		args.Cancel.Should().BeTrue();
		_exceptions.Should().BeEmpty();
	}

	/// <summary>A valid tree rename is sent to the provider and the folder's nodes take the new path.</summary>
	[Fact]
	public async Task TreeAfterEdit_ValidName_RenamesFolderAndDescendants()
	{
		var cut = RenderExplorer();
		await NavigateAsync(cut, "/Docs/Sub");
		await NavigateAsync(cut, "/Docs");

		await AfterTreeEditAsync(cut, "Docs", "Papers");

		_provider.Updates.Should().ContainSingle().Which.Delta["Path"].Should().Be("/Papers");
		Tree(cut).Instance.RootNode.Find("/Papers").Should().NotBeNull();
		Tree(cut).Instance.RootNode.Find("/Papers/Sub")!.Data!.Path.Should().Be("/Papers/Sub");
		cut.Instance.FolderPath.Should().Be("/Papers");
	}

	/// <summary>A tree rename the provider refuses leaves the folder as it was.</summary>
	[Fact]
	public async Task TreeAfterEdit_ProviderRefuses_LeavesFolder()
	{
		_provider.RefuseUpdates();
		var cut = RenderExplorer();
		await NavigateAsync(cut, "/Docs");

		await AfterTreeEditAsync(cut, "Docs", "Papers");

		Tree(cut).Instance.RootNode.Find("/Docs").Should().NotBeNull();
		cut.Instance.FolderPath.Should().Be("/Docs");
	}

	private static async Task<TreeNodeAfterEditEventArgs<FileExplorerItem>> AfterTreeEditAsync(IRenderedComponent<PDFileExplorer> cut, string oldName, string newName)
	{
		var tree = Tree(cut);
		var args = new TreeNodeAfterEditEventArgs<FileExplorerItem>(tree.Instance.SelectedNode!, oldName, newName);
		await tree.InvokeAsync(() => tree.Instance.AfterEdit.InvokeAsync(args));
		return args;
	}
}
