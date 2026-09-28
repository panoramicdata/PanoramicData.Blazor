using AwesomeAssertions;
using Bunit;
using PanoramicData.Blazor.Arguments;
using PanoramicData.Blazor.Exceptions;
using PanoramicData.Blazor.Models;

namespace PanoramicData.Blazor.Test.Components;

/// <summary>
/// Table pane behaviour of <see cref="PDFileExplorer"/>: which rows are listed, cell rendering, double-click,
/// renaming, and the keyboard shortcuts.
/// </summary>
public partial class PDFileExplorerTests
{
	private static async Task<TableAfterEditEventArgs<FileExplorerItem>> AfterTableEditAsync(IRenderedComponent<PDFileExplorer> cut, string path, object? newName)
	{
		var table = Table(cut);
		var item = table.Instance.ItemsToDisplay.Single(x => x.Path == path);
		var args = new TableAfterEditEventArgs<FileExplorerItem>(item) { NewValues = new() { ["Name"] = newName } };
		await table.InvokeAsync(() => table.Instance.AfterEdit.InvokeAsync(args));
		return args;
	}

	private static async Task<TableBeforeEditEventArgs<FileExplorerItem>> BeforeTableEditAsync(IRenderedComponent<PDFileExplorer> cut, FileExplorerItem item)
	{
		var table = Table(cut);
		var args = new TableBeforeEditEventArgs<FileExplorerItem>(item);
		await table.InvokeAsync(() => table.Instance.BeforeEdit.InvokeAsync(args));
		return args;
	}

	/// <summary>A sub-folder lists a parent (..) entry first, then folders, then files.</summary>
	[Fact]
	public async Task SubFolder_ListsParentEntry_ThenFoldersThenFiles()
	{
		var cut = RenderExplorer();

		await NavigateAsync(cut, "/Docs");

		RowNames(cut).Should().Equal("..", "Sub", "a.docx", "b.xlsx");
		var parent = Table(cut).Instance.ItemsToDisplay[0];
		parent.Path.Should().Be("/");
		parent.IsReadOnly.Should().BeTrue();
		parent.CanCopyMove.Should().BeFalse();
	}

	/// <summary>With the parent entry and folder grouping turned off, rows keep the provider's order.</summary>
	[Fact]
	public async Task NoParentEntry_AndNoGrouping_KeepsProviderOrder()
	{
		var cut = RenderExplorer(p => p.Add(x => x.ShowParentFolder, false).Add(x => x.GroupFolders, false));

		await NavigateAsync(cut, "/Docs");

		RowNames(cut).Should().Equal("a.docx", "b.xlsx", "Sub");
	}

	/// <summary>Files can be hidden altogether, or filtered by a filename pattern.</summary>
	[Fact]
	public async Task FileFilters_HideOrFilterFiles()
	{
		var hidden = RenderExplorer(p => p.Add(x => x.ShowFiles, false));
		await NavigateAsync(hidden, "/Docs");
		RowNames(hidden).Should().Equal("..", "Sub");

		var filtered = RenderExplorer(p => p.Add(x => x.FilenamePattern, "*.xlsx"));
		await NavigateAsync(filtered, "/Docs");
		RowNames(filtered).Should().Equal("..", "Sub", "b.xlsx");
	}

	/// <summary>Cells show the type, a humanised size, and the dates in the configured format.</summary>
	[Fact]
	public async Task Cells_ShowTypeSizeAndDates()
	{
		var cut = RenderExplorer(p => p
			.Add(x => x.DateFormat, "yyyy")
			.Add(x => x.ColumnConfig, [new PDColumnConfig { Id = "Type" }, new PDColumnConfig { Id = "Size" }, new PDColumnConfig { Id = "Created" }, new PDColumnConfig { Id = "Modified" }]));
		await NavigateAsync(cut, "/Docs");

		var row = cut.Find("tr[id='/Docs/b.xlsx']");
		var cells = row.QuerySelectorAll("td").Select(x => x.TextContent.Trim()).ToList();

		cells[0].Should().Be("XLSX File");
		cells[1].Should().Be("2 KB");
		cells[2].Should().Be("2026");
		cells[3].Should().Be("2026");
	}

	/// <summary>A small non-empty file is shown as at least one kilobyte; a folder shows no size and "File Folder".</summary>
	[Fact]
	public async Task Cells_SmallFileRoundsUp_AndFolderHasNoSize()
	{
		var cut = RenderExplorer(p => p.Add(x => x.ColumnConfig, [new PDColumnConfig { Id = "Type" }, new PDColumnConfig { Id = "Size" }]));
		await NavigateAsync(cut, "/Docs");

		cut.Find("tr[id='/Docs/a.docx']").QuerySelectorAll("td")[1].TextContent.Trim().Should().Be("1 KB");
		var folderCells = cut.Find("tr[id='/Docs/Sub']").QuerySelectorAll("td");
		folderCells[0].TextContent.Trim().Should().Be("File Folder");
		folderCells[1].TextContent.Trim().Should().BeEmpty();
	}

	/// <summary>Row cells carry hidden, system and read-only classes, unless the application supplies its own.</summary>
	[Fact]
	public async Task Cells_CarryStateClasses_OrApplicationClasses()
	{
		_provider.Get("/Docs/a.docx").IsHidden = true;
		_provider.Get("/Docs/b.xlsx").IsSystem = true;
		var cut = RenderExplorer();
		await NavigateAsync(cut, "/Docs");
		cut.Find("tr[id='/Docs/a.docx'] span.file-hidden").Should().NotBeNull();
		cut.Find("tr[id='/Docs/b.xlsx'] span.file-system").Should().NotBeNull();
		cut.Find("tr[id='/'] span.file-readonly").Should().NotBeNull();

		var custom = RenderExplorer(p => p.Add(x => x.GetItemCssClass, item => item.Name == "Docs" ? "mine" : null!));

		custom.Find("tr[id='/Docs'] span.mine").Should().NotBeNull();
		custom.FindAll("tr[id='/Media'] span.file-readonly").Should().NotBeEmpty();
	}

	/// <summary>Icons default by entry type; an application function can replace them or blank them out.</summary>
	[Fact]
	public void GetIconCssClass_DefaultsAndOverrides()
	{
		var cut = RenderExplorer(p => p.Add(x => x.GetItemIconCssClass, item => item.Name switch { "custom" => "fas fa-x", "blank" => string.Empty, _ => null! }));
		var explorer = cut.Instance;

		explorer.GetIconCssClass(null).Should().BeEmpty();
		explorer.GetIconCssClass(new FileExplorerItem { EntryType = FileExplorerItemType.File }).Should().Be("far fa-fw fa-file");
		explorer.GetIconCssClass(new FileExplorerItem { EntryType = FileExplorerItemType.Directory }).Should().Be("fa fa-fw fa-folder");
		explorer.GetIconCssClass(new FileExplorerItem { Name = "custom" }).Should().Be("fas fa-x");
		explorer.GetIconCssClass(new FileExplorerItem { Name = "blank" }).Should().Be("far fa-fw fa-hidden fa-file");
	}

	/// <summary>Display names and the read-only indicator rule, as used by both panes.</summary>
	[Fact]
	public void DisplayNameAndReadOnlyIndicator_Rules()
	{
		PDFileExplorer.GetItemDisplayName(null).Should().BeEmpty();
		PDFileExplorer.GetItemDisplayName(new FileExplorerItem { Name = "x" }).Should().Be("x");
		PDFileExplorer.ShouldShowReadOnlyIndicator(null).Should().BeFalse();
		PDFileExplorer.ShouldShowReadOnlyIndicator(new FileExplorerItem { Name = "..", IsReadOnly = true }).Should().BeFalse();
		PDFileExplorer.ShouldShowReadOnlyIndicator(new FileExplorerItem { Name = "x", IsReadOnly = true }).Should().BeTrue();
		PDFileExplorer.ShouldShowReadOnlyIndicator(new FileExplorerItem { Name = "x" }).Should().BeFalse();
	}

	/// <summary>Read-only rows show the indicator in the name cell, as an icon when an icon class is set.</summary>
	[Fact]
	public void NameCell_ReadOnlyIndicator_TextOrIcon()
	{
		var text = RenderExplorer();
		text.Find("tr[id='/Media'] .pdfe-readonly-text.pdfe-readonly-after").TextContent.Should().Be("(ro)");

		var icon = RenderExplorer(p => p.Add(x => x.ReadOnlyIconClass, "fa fa-lock").Add(x => x.ReadOnlyIndicatorPosition, ReadOnlyIndicatorPosition.Before));
		icon.Find("tr[id='/Media'] i.pdfe-readonly-icon.pdfe-readonly-before").Should().NotBeNull();
	}

	/// <summary>Double-clicking a folder opens it; double-clicking a file raises ItemDoubleClick.</summary>
	[Fact]
	public async Task DoubleClick_OpensFolders_AndReportsFiles()
	{
		FileExplorerItem? opened = null;
		var cut = RenderExplorer(p => p.Add(x => x.ItemDoubleClick, (FileExplorerItem i) => opened = i));
		var table = Table(cut);

		await table.InvokeAsync(() => table.Instance.DoubleClick.InvokeAsync(table.Instance.ItemsToDisplay.Single(x => x.Name == "Docs")));
		cut.Instance.FolderPath.Should().Be("/Docs");

		await table.InvokeAsync(() => table.Instance.DoubleClick.InvokeAsync(table.Instance.ItemsToDisplay.Single(x => x.Name == "a.docx")));
		opened!.Path.Should().Be("/Docs/a.docx");
	}

	/// <summary>While a row is being edited a double-click does nothing.</summary>
	[Fact]
	public async Task DoubleClick_WhileEditing_DoesNothing()
	{
		var cut = RenderExplorer();
		await SelectRowsAsync(cut, "/Docs");
		await ClickMenuAsync(TableMenu(cut), "Rename");
		var table = Table(cut);

		await table.InvokeAsync(() => table.Instance.DoubleClick.InvokeAsync(table.Instance.ItemsToDisplay.Single(x => x.Name == "Docs")));

		table.Instance.IsEditing.Should().BeTrue();
		cut.Instance.FolderPath.Should().Be("/");
	}

	/// <summary>Beginning a row edit selects only the name part of a file name.</summary>
	[Fact]
	public async Task BeforeEdit_SelectsNameWithoutExtension()
	{
		var cut = RenderExplorer();
		await NavigateAsync(cut, "/Docs");

		var args = await BeforeTableEditAsync(cut, Table(cut).Instance.ItemsToDisplay.Single(x => x.Name == "b.xlsx"));

		args.Cancel.Should().BeFalse();
		args.SelectionEnd.Should().Be(1);
	}

	/// <summary>Row edits are refused for the parent entry, uploads, read-only items and unrenamable items.</summary>
	[Fact]
	public async Task BeforeEdit_RefusesItemsThatCannotBeRenamed()
	{
		var cut = RenderExplorer();

		(await BeforeTableEditAsync(cut, new FileExplorerItem { Name = ".." })).Cancel.Should().BeTrue();
		(await BeforeTableEditAsync(cut, new FileExplorerItem { Name = "u", IsUploading = true })).Cancel.Should().BeTrue();
		(await BeforeTableEditAsync(cut, new FileExplorerItem { Name = "r", IsReadOnly = true })).Cancel.Should().BeTrue();
		(await BeforeTableEditAsync(cut, new FileExplorerItem { Name = "n", CanRename = false })).Cancel.Should().BeTrue();
	}

	/// <summary>Row edits are refused when the application cancels them or renaming is off.</summary>
	[Fact]
	public async Task BeforeEdit_RefusedByApplicationOrSetting()
	{
		var cancelling = RenderExplorer(p => p.Add(x => x.BeforeRename, (RenameArgs a) => a.Cancel = true));
		(await BeforeTableEditAsync(cancelling, new FileExplorerItem { Name = "x" })).Cancel.Should().BeTrue();

		var disallowed = RenderExplorer(p => p.Add(x => x.AllowRename, false));
		(await BeforeTableEditAsync(disallowed, new FileExplorerItem { Name = "x" })).Cancel.Should().BeTrue();
	}

	/// <summary>An unchanged, blank or period-prefixed row name is refused, the latter two with an explanation.</summary>
	[Fact]
	public async Task AfterEdit_RefusesUnchangedBlankAndPeriodNames()
	{
		var cut = RenderExplorer();
		await NavigateAsync(cut, "/Docs");

		(await AfterTableEditAsync(cut, "/Docs/a.docx", "a.docx")).Cancel.Should().BeTrue();
		(await AfterTableEditAsync(cut, "/Docs/a.docx", " ")).Cancel.Should().BeTrue();
		(await AfterTableEditAsync(cut, "/Docs/a.docx", ".a")).Cancel.Should().BeTrue();

		_exceptions.Should().HaveCount(2).And.AllBeOfType<PDFileExplorerException>();
		_provider.Updates.Should().BeEmpty();
	}

	/// <summary>Renaming a row to another row's name (ignoring case) is refused and reported.</summary>
	[Fact]
	public async Task AfterEdit_DuplicateName_IsRefused()
	{
		var cut = RenderExplorer();
		await NavigateAsync(cut, "/Docs");

		var args = await AfterTableEditAsync(cut, "/Docs/a.docx", "B.XLSX");

		args.Cancel.Should().BeTrue();
		_exceptions.Should().ContainSingle().Which.Message.Should().Contain("already exists");
	}

	/// <summary>A file rename goes to the provider, updates the row and selects it by its new path.</summary>
	[Fact]
	public async Task AfterEdit_FileRename_UpdatesRowAndSelection()
	{
		var cut = RenderExplorer();
		await NavigateAsync(cut, "/Docs");

		var args = await AfterTableEditAsync(cut, "/Docs/a.docx", "c.docx");

		args.Cancel.Should().BeFalse();
		args.Item.Path.Should().Be("/Docs/c.docx");
		args.Item.Name.Should().Be("c.docx");
		Table(cut).Instance.Selection.Should().Equal("/Docs/c.docx");
	}

	/// <summary>Changing only the case of a name is allowed.</summary>
	[Fact]
	public async Task AfterEdit_CaseOnlyChange_IsAllowed()
	{
		var cut = RenderExplorer();
		await NavigateAsync(cut, "/Docs");

		var args = await AfterTableEditAsync(cut, "/Docs/a.docx", "A.docx");

		args.Cancel.Should().BeFalse();
		_provider.Updates.Should().ContainSingle().Which.Delta["Path"].Should().Be("/Docs/A.docx");
	}

	/// <summary>A folder renamed in the root folder gets a single leading slash, and its tree node follows.</summary>
	[Fact]
	public async Task AfterEdit_FolderRenameInRoot_RenamesTreeNode()
	{
		var cut = RenderExplorer();

		await AfterTableEditAsync(cut, "/Empty", "Blank");

		_provider.Updates.Should().ContainSingle().Which.Delta["Path"].Should().Be("/Blank");
		Tree(cut).Instance.RootNode.Find("/Blank").Should().NotBeNull();
	}

	/// <summary>A rename the provider refuses is cancelled.</summary>
	[Fact]
	public async Task AfterEdit_ProviderRefuses_IsCancelled()
	{
		_provider.FailUpdates = true;
		var cut = RenderExplorer();
		await NavigateAsync(cut, "/Docs");

		var args = await AfterTableEditAsync(cut, "/Docs/a.docx", "c.docx");

		args.Cancel.Should().BeTrue();
		args.Item.Name.Should().Be("a.docx");
	}

	/// <summary>An edit that does not include the name changes nothing.</summary>
	[Fact]
	public async Task AfterEdit_WithoutName_DoesNothing()
	{
		var cut = RenderExplorer();
		var table = Table(cut);
		var args = new TableAfterEditEventArgs<FileExplorerItem>(table.Instance.ItemsToDisplay[0]);

		await table.InvokeAsync(() => table.Instance.AfterEdit.InvokeAsync(args));

		args.Cancel.Should().BeFalse();
		_provider.Updates.Should().BeEmpty();
	}
}
