using AwesomeAssertions;
using PanoramicData.Blazor.Arguments;
using PanoramicData.Blazor.Models;

namespace PanoramicData.Blazor.Test.Components;

/// <summary>
/// Folder tree context menu tests for <see cref="PDFileExplorer"/>.
/// </summary>
public partial class PDFileExplorerTests
{
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
}
