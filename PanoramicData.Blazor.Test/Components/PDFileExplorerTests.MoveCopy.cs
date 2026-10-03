using AwesomeAssertions;
using Bunit;
using PanoramicData.Blazor.Arguments;

namespace PanoramicData.Blazor.Test.Components;

/// <summary>
/// Move, copy and drop behaviour of <see cref="PDFileExplorer"/>: keyboard and tree clipboard, conflict detection
/// and resolution, custom move/copy, and dropping items on folders.
/// </summary>
public partial class PDFileExplorerTests
{
	private static void WaitForConflictPrompt(IRenderedComponent<PDFileExplorer> cut, string message)
		=> cut.WaitForAssertion(() => Modal(cut, "Move / Copy Conflict").Find(".modal-body").TextContent.Should().Contain(message));

	/// <summary>Ctrl+C then Ctrl+V in the table copies the selection into the current folder when no folder is selected.</summary>
	[Fact]
	public async Task TableKeys_CopyPaste_IntoCurrentFolder()
	{
		var cut = RenderExplorer();
		await NavigateAsync(cut, "/Docs");
		await SelectRowsAsync(cut, "/Docs/a.docx");
		await KeyDownTableAsync(cut, "KeyC", ctrl: true);
		await NavigateAsync(cut, "/Empty");

		await KeyDownTableAsync(cut, "KeyV", ctrl: true);

		_provider.Updates.Should().ContainSingle().Which.Delta.Should().Contain("Path", "/Empty/a.docx").And.Contain("Copy", true);
	}

	/// <summary>Ctrl+X then Ctrl+V into a single selected folder moves the items, and the clipboard is then empty.</summary>
	[Fact]
	public async Task TableKeys_CutPaste_IntoSelectedFolder_ClearsClipboard()
	{
		var cut = RenderExplorer();
		await NavigateAsync(cut, "/Docs");
		await SelectRowsAsync(cut, "/Docs/b.xlsx");
		await KeyDownTableAsync(cut, "KeyX", ctrl: true);
		await SelectRowsAsync(cut, "/Docs/Sub");

		await KeyDownTableAsync(cut, "KeyV", ctrl: true);
		await KeyDownTableAsync(cut, "KeyV", ctrl: true);

		_provider.Updates.Should().ContainSingle().Which.Delta.Should().Contain("Path", "/Docs/Sub/b.xlsx").And.Contain("Copy", false);
	}

	/// <summary>Table keys are ignored while a row is being edited.</summary>
	[Fact]
	public async Task TableKeys_WhileEditing_AreIgnored()
	{
		var deleteRequests = 0;
		var cut = RenderExplorer(p => p.Add(x => x.DeleteRequest, (DeleteArgs _) => deleteRequests++));
		await SelectRowsAsync(cut, "/Docs");
		await ClickMenuAsync(TableMenu(cut), "Rename");

		await KeyDownTableAsync(cut, "Delete");

		deleteRequests.Should().Be(0);
	}

	/// <summary>Tree Ctrl+C then Ctrl+V on another folder copies the folder there and refreshes the target node.</summary>
	[Fact]
	public async Task TreeKeys_CopyPaste_CopiesFolder()
	{
		var cut = RenderExplorer();
		await NavigateAsync(cut, "/Docs/Sub");
		await KeyDownTreeAsync(cut, "KeyC", ctrl: true);
		await NavigateAsync(cut, "/Empty");

		await KeyDownTreeAsync(cut, "KeyV", ctrl: true);

		_provider.Updates.Should().ContainSingle().Which.Delta.Should().Contain("Path", "/Empty/Sub").And.Contain("Copy", true);
		Tree(cut).Instance.RootNode.Find("/Empty/Sub").Should().NotBeNull();
	}

	/// <summary>Tree Ctrl+X then Ctrl+V moves the folder and empties the clipboard; an undeletable folder cannot be cut.</summary>
	[Fact]
	public async Task TreeKeys_CutPaste_MovesFolder_UnlessUndeletable()
	{
		_provider.Get("/Empty").CanDelete = false;
		var cut = RenderExplorer();
		await NavigateAsync(cut, "/Empty");
		await KeyDownTreeAsync(cut, "KeyX", ctrl: true);
		await NavigateAsync(cut, "/Docs");
		await KeyDownTreeAsync(cut, "KeyV", ctrl: true);
		_provider.Updates.Should().BeEmpty();

		await NavigateAsync(cut, "/Docs/Sub");
		await KeyDownTreeAsync(cut, "KeyX", ctrl: true);
		await NavigateAsync(cut, "/Empty");
		await KeyDownTreeAsync(cut, "KeyV", ctrl: true);
		await KeyDownTreeAsync(cut, "KeyV", ctrl: true);

		_provider.Updates.Should().ContainSingle().Which.Delta.Should().Contain("Path", "/Empty/Sub").And.Contain("Copy", false);
	}

	/// <summary>Tree keys are ignored while the selected node is being edited.</summary>
	[Fact]
	public async Task TreeKeys_WhileEditing_AreIgnored()
	{
		var deleteRequests = 0;
		var cut = RenderExplorer(p => p.Add(x => x.DeleteRequest, (DeleteArgs _) => deleteRequests++));
		await NavigateAsync(cut, "/Docs");
		await ClickMenuAsync(TreeMenu(cut), "Rename");

		await KeyDownTreeAsync(cut, "Delete");

		deleteRequests.Should().Be(0);
	}

	/// <summary>Tree Copy and Cut menu entries fill the clipboard, and Paste moves a cut folder into the selected one.</summary>
	[Fact]
	public async Task TreeMenu_CutPaste_MovesFolder_AndSelectsTarget()
	{
		var cut = RenderExplorer();
		await NavigateAsync(cut, "/Docs/Sub");
		await ClickMenuAsync(TreeMenu(cut), "Copy");
		await ClickMenuAsync(TreeMenu(cut), "Cut");
		await NavigateAsync(cut, "/Empty");

		await ClickMenuAsync(TreeMenu(cut), "Paste");

		_provider.Updates.Should().ContainSingle().Which.Delta.Should().Contain("Path", "/Empty/Sub").And.Contain("Copy", false);
		cut.Instance.FolderPath.Should().Be("/Empty");
	}

	/// <summary>An application performing the move/copy itself can stop the default behaviour.</summary>
	[Fact]
	public async Task CustomMoveCopy_CancelDefault_SkipsProvider()
	{
		CustomMoveCopyArgs? seen = null;
		var cut = RenderExplorer(p => p.Add(x => x.CustomMoveCopy, (CustomMoveCopyArgs a) => { seen = a; a.CancelDefault = true; }));
		await NavigateAsync(cut, "/Docs");
		await SelectRowsAsync(cut, "/Docs/a.docx");
		await KeyDownTableAsync(cut, "KeyC", ctrl: true);
		await NavigateAsync(cut, "/Empty");

		await KeyDownTableAsync(cut, "KeyV", ctrl: true);

		seen!.TargetPath.Should().Be("/Empty");
		seen.IsCopy.Should().BeTrue();
		seen.Payload.Should().ContainSingle();
		_provider.Updates.Should().BeEmpty();
	}

	/// <summary>An application observing the move/copy without cancelling lets the default go ahead.</summary>
	[Fact]
	public async Task CustomMoveCopy_WithoutCancel_StillMoves()
	{
		var calls = 0;
		var cut = RenderExplorer(p => p.Add(x => x.CustomMoveCopy, (CustomMoveCopyArgs _) => calls++));
		await NavigateAsync(cut, "/Docs");
		await SelectRowsAsync(cut, "/Docs/a.docx");
		await KeyDownTableAsync(cut, "KeyC", ctrl: true);
		await NavigateAsync(cut, "/Empty");

		await KeyDownTableAsync(cut, "KeyV", ctrl: true);

		calls.Should().Be(1);
		_provider.Updates.Should().ContainSingle();
	}
}
