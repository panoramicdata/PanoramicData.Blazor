using AwesomeAssertions;
using Bunit;
using PanoramicData.Blazor.Arguments;
using PanoramicData.Blazor.Exceptions;
using PanoramicData.Blazor.Models;

namespace PanoramicData.Blazor.Test.Components;

/// <summary>
/// File table rename tests for <see cref="PDFileExplorer"/>.
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
		_provider.RefuseUpdates();
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
