using AwesomeAssertions;
using Bunit;
using PanoramicData.Blazor.Models;

namespace PanoramicData.Blazor.Test.Components;

/// <summary>
/// Tests of where pasting from the file table context menu of <see cref="PDFileExplorer"/> puts the items.
/// </summary>
public partial class PDFileExplorerTests
{
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
