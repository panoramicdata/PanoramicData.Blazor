using AwesomeAssertions;
using Bunit;
using PanoramicData.Blazor.Arguments;
using PanoramicData.Blazor.Models;

namespace PanoramicData.Blazor.Test.Components;

/// <summary>
/// Table context menu behaviour of <see cref="PDFileExplorer"/>: which entries are offered for a selection,
/// where Paste would go, and what each entry does.
/// </summary>
public partial class PDFileExplorerTests
{
	private static ElementInfo Cell(string rowId, params string[] rowClasses)
		=> new() { Tag = "TD", Parent = new ElementInfo { Tag = "TR", Id = rowId, ClassList = rowClasses } };

	/// <summary>With nothing selected, the table menu offers New Folder only, and the application sees the empty selection.</summary>
	[Fact]
	public async Task TableMenu_NoSelection_OffersNewFolder()
	{
		object? context = null;
		var cut = RenderExplorer(p => p.Add(x => x.UpdateTableContextState, (MenuItemsEventArgs a) => context = a.Context));
		var menu = TableMenu(cut);

		await UpdateMenuAsync(menu);

		VisibleMenuTexts(menu).Should().Equal("New Folder");
		((FileExplorerItem[])context!).Should().BeEmpty();
	}

	/// <summary>With an upload URL and nothing selected, Upload Files is offered too.</summary>
	[Fact]
	public async Task TableMenu_NoSelection_WithUploadUrl_OffersUpload()
	{
		var cut = RenderExplorer(p => p.Add(x => x.UploadUrl, "https://example.com/upload"));
		var menu = TableMenu(cut);

		await UpdateMenuAsync(menu);

		VisibleMenuTexts(menu).Should().Equal("Upload Files", "New Folder");
	}

	/// <summary>A single selected folder offers Open, Rename, Copy, Cut and Delete, with separators between groups.</summary>
	[Fact]
	public async Task TableMenu_SingleFolder_OffersFolderActions()
	{
		var cut = RenderExplorer();
		await SelectRowsAsync(cut, "/Docs");
		var menu = TableMenu(cut);

		await UpdateMenuAsync(menu);

		VisibleMenuTexts(menu).Should().Equal("Open", "Rename", "Copy", "Cut", "Delete");
		menu.Instance.Items.Where(x => x.IsSeparator).Count(x => x.IsVisible).Should().Be(3);
	}

	/// <summary>Where hiding an entry leaves two separators together, only the first is shown.</summary>
	[Fact]
	public async Task TableMenu_AdjacentSeparators_AreCollapsed()
	{
		_provider.Get("/Docs").CanRename = false;
		var cut = RenderExplorer();
		await SelectRowsAsync(cut, "/Docs");
		var menu = TableMenu(cut);

		await UpdateMenuAsync(menu);

		VisibleMenuTexts(menu).Should().Equal("Open", "Copy", "Cut", "Delete");
		menu.Instance.Items.Where(x => x.IsSeparator).Count(x => x.IsVisible).Should().Be(2);
	}

	/// <summary>A selection of files offers Download, and no Open or Rename for several.</summary>
	[Fact]
	public async Task TableMenu_SeveralFiles_OffersDownload()
	{
		var cut = RenderExplorer();
		await NavigateAsync(cut, "/Docs");
		await SelectRowsAsync(cut, "/Docs/a.docx", "/Docs/b.xlsx");
		var menu = TableMenu(cut);

		await UpdateMenuAsync(menu);

		VisibleMenuTexts(menu).Should().Equal("Download", "Copy", "Cut", "Delete");
	}

	/// <summary>The parent entry cannot be renamed, copied, cut or deleted.</summary>
	[Fact]
	public async Task TableMenu_ParentEntry_OffersOnlyOpen()
	{
		var cut = RenderExplorer();
		await NavigateAsync(cut, "/Docs");
		await SelectRowsAsync(cut, "/");
		var menu = TableMenu(cut);

		await UpdateMenuAsync(menu);

		VisibleMenuTexts(menu).Should().Equal("Open");
	}

	/// <summary>A row still uploading makes the selection invalid, so no edit actions are offered.</summary>
	[Fact]
	public async Task TableMenu_UploadingRow_OffersNoEditActions()
	{
		var cut = RenderExplorer();
		await NavigateAsync(cut, "/Docs");
		Table(cut).Instance.ItemsToDisplay.Single(x => x.Name == "a.docx").IsUploading = true;
		await SelectRowsAsync(cut, "/Docs/a.docx");
		var menu = TableMenu(cut);

		await UpdateMenuAsync(menu);

		VisibleMenuTexts(menu).Should().BeEmpty();
	}

	/// <summary>A table menu click the application cancels does nothing.</summary>
	[Fact]
	public async Task TableMenuClick_Cancelled_DoesNothing()
	{
		var cut = RenderExplorer(p => p.Add(x => x.TableContextMenuClick, (MenuItemEventArgs a) => a.Cancel = true));
		await SelectRowsAsync(cut, "/Docs");

		await ClickMenuAsync(TableMenu(cut), "Open");

		cut.Instance.FolderPath.Should().Be("/");
	}

	/// <summary>Open navigates into the single selected folder.</summary>
	[Fact]
	public async Task TableMenu_Open_NavigatesIntoFolder()
	{
		var cut = RenderExplorer();
		await SelectRowsAsync(cut, "/Docs");

		await ClickMenuAsync(TableMenu(cut), "Open");

		cut.Instance.FolderPath.Should().Be("/Docs");
	}

	/// <summary>Download raises the download request with the selected files.</summary>
	[Fact]
	public async Task TableMenu_Download_RaisesDownloadRequest()
	{
		TableSelectionEventArgs<FileExplorerItem>? request = null;
		var cut = RenderExplorer(p => p.Add(x => x.TableDownloadRequest, (TableSelectionEventArgs<FileExplorerItem> a) => request = a));
		await NavigateAsync(cut, "/Docs");
		await SelectRowsAsync(cut, "/Docs/a.docx");

		await ClickMenuAsync(TableMenu(cut), "Download");

		request!.Items.Should().ContainSingle().Which.Path.Should().Be("/Docs/a.docx");
	}

	/// <summary>Rename puts the selected row into edit mode.</summary>
	[Fact]
	public async Task TableMenu_Rename_BeginsEditing()
	{
		var cut = RenderExplorer();
		await SelectRowsAsync(cut, "/Docs");

		await ClickMenuAsync(TableMenu(cut), "Rename");

		Table(cut).Instance.IsEditing.Should().BeTrue();
	}

	/// <summary>New Folder from the table picks a name not already listed, creates it and starts editing its row.</summary>
	[Fact]
	public async Task TableMenu_NewFolder_PicksUnlistedName_AndEditsIt()
	{
		_provider.Items.Add(FileProvider.Dir("/New Folder"));
		var cut = RenderExplorer();

		await ClickMenuAsync(TableMenu(cut), "New Folder");

		_provider.Creates.Should().Equal("/New Folder (2)");
		Table(cut).Instance.Selection.Should().Equal("/New Folder (2)");
		Table(cut).Instance.IsEditing.Should().BeTrue();
	}

	/// <summary>The create-folder toolbar button creates a folder in the current folder.</summary>
	[Fact]
	public async Task CreateFolderButton_CreatesFolderInCurrentFolder()
	{
		var cut = RenderExplorer(p => p.Add(x => x.NewFolderName, "Fresh"));
		await NavigateAsync(cut, "/Docs");

		await ClickToolbarAsync(cut, "create-folder");

		_provider.Creates.Should().Equal("/Docs/Fresh");
	}

	/// <summary>Copy then Paste from the table menu copies the selection into the folder right-clicked on.</summary>
	[Fact]
	public async Task TableMenu_CopyPaste_IntoRightClickedFolder()
	{
		var cut = RenderExplorer();
		await NavigateAsync(cut, "/Docs");
		await SelectRowsAsync(cut, "/Docs/a.docx");
		await ClickMenuAsync(TableMenu(cut), "Copy");
		await SelectRowsAsync(cut, "/Docs/Sub");

		await UpdateMenuAsync(TableMenu(cut), Cell("/Docs/Sub", "selected"));
		VisibleMenuTexts(TableMenu(cut)).Should().Contain("Paste");
		await ClickMenuAsync(TableMenu(cut), "Paste");

		var update = _provider.Updates.Should().ContainSingle().Subject;
		update.Path.Should().Be("/Docs/a.docx");
		update.Delta["Path"].Should().Be("/Docs/Sub/a.docx");
		update.Delta["Copy"].Should().Be(true);
	}

	/// <summary>Paste is not offered when the selected row right-clicked on is a file.</summary>
	[Fact]
	public async Task TableMenu_PasteOntoSelectedFile_IsNotOffered()
	{
		var cut = RenderExplorer();
		await NavigateAsync(cut, "/Docs");
		await SelectRowsAsync(cut, "/Docs/a.docx");
		await ClickMenuAsync(TableMenu(cut), "Copy");

		await UpdateMenuAsync(TableMenu(cut), Cell("/Docs/a.docx", "selected"));

		VisibleMenuTexts(TableMenu(cut)).Should().NotContain("Paste");
	}

	/// <summary>Right-clicking empty space pastes into the current folder.</summary>
	[Fact]
	public async Task TableMenu_PasteOnWhitespace_TargetsCurrentFolder()
	{
		var cut = RenderExplorer();
		await NavigateAsync(cut, "/Docs");
		await SelectRowsAsync(cut, "/Docs/a.docx");
		await ClickMenuAsync(TableMenu(cut), "Cut");
		await NavigateAsync(cut, "/Empty");

		await UpdateMenuAsync(TableMenu(cut), new ElementInfo { Tag = "DIV" });
		await ClickMenuAsync(TableMenu(cut), "Paste");

		_provider.Updates.Should().ContainSingle().Which.Delta.Should().Contain("Path", "/Empty/a.docx").And.Contain("Copy", false);
	}

	/// <summary>Right-clicking inside an unselected folder row pastes into that folder.</summary>
	[Fact]
	public async Task TableMenu_PasteInsideUnselectedFolderRow_TargetsThatFolder()
	{
		var cut = RenderExplorer();
		await NavigateAsync(cut, "/Docs");
		await SelectRowsAsync(cut, "/Docs/a.docx");
		await ClickMenuAsync(TableMenu(cut), "Copy");
		await Table(cut).InvokeAsync(() => Table(cut).Instance.ClearSelectionAsync());

		await UpdateMenuAsync(TableMenu(cut), new ElementInfo { Tag = "SPAN", Parent = Cell("/Docs/Sub") });
		await ClickMenuAsync(TableMenu(cut), "Paste");

		_provider.Updates.Should().ContainSingle().Which.Delta["Path"].Should().Be("/Docs/Sub/a.docx");
	}

	/// <summary>Right-clicking inside a file row that is not selected pastes into the current folder.</summary>
	[Fact]
	public async Task TableMenu_PasteInsideUnselectedFileRow_TargetsCurrentFolder()
	{
		var cut = RenderExplorer();
		await NavigateAsync(cut, "/Docs");
		await SelectRowsAsync(cut, "/Docs/Sub");
		await ClickMenuAsync(TableMenu(cut), "Copy");
		await NavigateAsync(cut, "/Empty");
		await NavigateAsync(cut, "/Docs");

		await UpdateMenuAsync(TableMenu(cut), new ElementInfo { Tag = "SPAN", Parent = Cell("/Docs/b.xlsx") });

		VisibleMenuTexts(TableMenu(cut)).Should().Contain("Paste");
	}
}
