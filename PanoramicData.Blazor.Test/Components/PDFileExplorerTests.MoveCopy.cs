using AwesomeAssertions;
using Bunit;
using PanoramicData.Blazor.Arguments;
using PanoramicData.Blazor.Models;

namespace PanoramicData.Blazor.Test.Components;

/// <summary>
/// Move, copy and drop behaviour of <see cref="PDFileExplorer"/>: keyboard and tree clipboard, conflict detection
/// and resolution, custom move/copy, and dropping items on folders.
/// </summary>
public partial class PDFileExplorerTests
{
	private static void WaitForConflictPrompt(IRenderedComponent<PDFileExplorer> cut, string message)
		=> cut.WaitForAssertion(() => Modal(cut, "Move / Copy Conflict").Find(".modal-body").TextContent.Should().Contain(message));

	private static async Task DropAsync(IRenderedComponent<PDFileExplorer> cut, object? target, object? payload, bool ctrl)
	{
		var table = Table(cut);
		await table.InvokeAsync(() => table.Instance.Drop.InvokeAsync(new DropEventArgs(target, payload, ctrl)));
	}

	private async Task<IRenderedComponent<PDFileExplorer>> CopyDocIntoEmptyWithConflictAsync(Action<ComponentParameterCollectionBuilder<PDFileExplorer>>? configure = null)
	{
		_provider.Items.Add(FileProvider.File("/Empty/a.docx", 1));
		var cut = RenderExplorer(configure);
		await NavigateAsync(cut, "/Docs");
		await SelectRowsAsync(cut, "/Docs/a.docx");
		await KeyDownTableAsync(cut, "KeyC", ctrl: true);
		await NavigateAsync(cut, "/Empty");
		return cut;
	}

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

	/// <summary>A conflict in another folder prompts with the conflicting names; Overwrite deletes the target then moves.</summary>
	[Fact]
	public async Task Conflict_Overwrite_DeletesTargetThenCopies()
	{
		var cut = await CopyDocIntoEmptyWithConflictAsync();

		var pending = cut.InvokeAsync(() => KeyDownTableAsync(cut, "KeyV", ctrl: true));
		WaitForConflictPrompt(cut, "1 conflicts found");
		Modal(cut, "Move / Copy Conflict").Find("li").TextContent.Should().Be("a.docx");
		await AnswerAsync(Modal(cut, "Move / Copy Conflict"), "Overwrite");
		await pending;

		_provider.Deletes.Should().Equal("/Empty/a.docx");
		_provider.Updates.Should().ContainSingle().Which.Delta["Path"].Should().Be("/Empty/a.docx");
	}

	/// <summary>Cancel at the conflict prompt does nothing; Skip leaves the conflicting item alone.</summary>
	/// <param name="answer">The button chosen.</param>
	[Theory]
	[InlineData("Cancel")]
	[InlineData("Skip")]
	[InlineData("Unexpected")]
	public async Task Conflict_CancelOrSkip_CopiesNothing(string answer)
	{
		var cut = await CopyDocIntoEmptyWithConflictAsync();

		var pending = cut.InvokeAsync(() => KeyDownTableAsync(cut, "KeyV", ctrl: true));
		WaitForConflictPrompt(cut, "conflicts found");
		await AnswerAsync(Modal(cut, "Move / Copy Conflict"), answer);
		await pending;

		_provider.Updates.Should().BeEmpty();
		_provider.Deletes.Should().BeEmpty();
	}

	/// <summary>Rename at the conflict prompt copies under a unique "Copy" name.</summary>
	[Fact]
	public async Task Conflict_Rename_CopiesUnderUniqueName()
	{
		_provider.Items.Add(FileProvider.File("/Empty/a Copy.docx", 1));
		var cut = await CopyDocIntoEmptyWithConflictAsync();

		var pending = cut.InvokeAsync(() => KeyDownTableAsync(cut, "KeyV", ctrl: true));
		WaitForConflictPrompt(cut, "conflicts found");
		await AnswerAsync(Modal(cut, "Move / Copy Conflict"), "Rename");
		await pending;

		_provider.Updates.Should().ContainSingle().Which.Delta["Path"].Should().Be("/Empty/a Copy 1.docx");
	}

	/// <summary>A resolution set in advance is used without prompting.</summary>
	[Fact]
	public async Task Conflict_PresetOverwrite_DoesNotPrompt()
	{
		var cut = await CopyDocIntoEmptyWithConflictAsync(p => p.Add(x => x.ConflictResolution, ConflictResolutions.Overwrite));

		await KeyDownTableAsync(cut, "KeyV", ctrl: true);

		_provider.Deletes.Should().Equal("/Empty/a.docx");
		_provider.Updates.Should().ContainSingle();
	}

	/// <summary>The application sees the conflicts and may choose the resolution itself.</summary>
	[Fact]
	public async Task Conflict_ApplicationChoosesSkip_DoesNotPrompt()
	{
		MoveCopyArgs? seen = null;
		var cut = await CopyDocIntoEmptyWithConflictAsync(p => p.Add(x => x.MoveCopyConflict, (MoveCopyArgs a) => { seen = a; a.ConflictResolution = ConflictResolutions.Skip; }));

		await KeyDownTableAsync(cut, "KeyV", ctrl: true);

		seen!.Conflicts.Should().ContainSingle().Which.Path.Should().Be("/Empty/a.docx");
		seen.TargetPath.Should().Be("/Empty");
		seen.IsCopy.Should().BeTrue();
		_provider.Updates.Should().BeEmpty();
	}

	/// <summary>Pasting into the source folder warns that the names are the same and hides Overwrite.</summary>
	[Fact]
	public async Task Conflict_SameFolder_HidesOverwrite_AndRenameCopies()
	{
		var cut = RenderExplorer(p => p.Add(x => x.AllowRenameConflicts, true));
		await NavigateAsync(cut, "/Docs");
		await SelectRowsAsync(cut, "/Docs/a.docx");
		await KeyDownTableAsync(cut, "KeyC", ctrl: true);
		await Table(cut).InvokeAsync(() => Table(cut).Instance.ClearSelectionAsync());

		var pending = cut.InvokeAsync(() => KeyDownTableAsync(cut, "KeyV", ctrl: true));
		WaitForConflictPrompt(cut, "The source and destination filenames are the same.");
		var buttons = Modal(cut, "Move / Copy Conflict").Instance.Buttons;
		buttons.Single(x => x.Key == "Overwrite").IsVisible.Should().BeFalse();
		buttons.Single(x => x.Key == "Rename").ShiftRight.Should().BeTrue();
		await AnswerAsync(Modal(cut, "Move / Copy Conflict"), "Rename");
		await pending;

		_provider.Updates.Should().ContainSingle().Which.Delta["Path"].Should().Be("/Docs/a Copy.docx");
	}

	/// <summary>Overwriting an item with itself is refused with an explanation.</summary>
	[Fact]
	public async Task Conflict_OverwriteOntoItself_IsReported()
	{
		var cut = RenderExplorer(p => p.Add(x => x.ConflictResolution, ConflictResolutions.Overwrite));
		await NavigateAsync(cut, "/Docs");
		await SelectRowsAsync(cut, "/Docs/a.docx");
		await KeyDownTableAsync(cut, "KeyC", ctrl: true);
		await Table(cut).InvokeAsync(() => Table(cut).Instance.ClearSelectionAsync());

		await KeyDownTableAsync(cut, "KeyV", ctrl: true);

		_exceptions.Should().ContainSingle().Which.Message.Should().Contain("Source and Destination are the same");
		_provider.Updates.Should().BeEmpty();
	}

	/// <summary>More than five conflicts are summarised with a count of the rest.</summary>
	[Fact]
	public async Task Conflict_ManyConflicts_AreSummarised()
	{
		for (var i = 0; i < 7; i++)
		{
			_provider.Items.Add(FileProvider.File($"/Docs/f{i}.txt", 1));
			_provider.Items.Add(FileProvider.File($"/Empty/f{i}.txt", 1));
		}

		var cut = RenderExplorer();
		await NavigateAsync(cut, "/Docs");
		await SelectRowsAsync(cut, [.. Enumerable.Range(0, 7).Select(i => $"/Docs/f{i}.txt")]);
		await KeyDownTableAsync(cut, "KeyC", ctrl: true);
		await NavigateAsync(cut, "/Empty");

		var pending = cut.InvokeAsync(() => KeyDownTableAsync(cut, "KeyV", ctrl: true));
		WaitForConflictPrompt(cut, "7 conflicts found");
		Modal(cut, "Move / Copy Conflict").FindAll("li").Select(x => x.TextContent).Should().HaveCount(6).And.Contain("+ 2 other items");
		await AnswerAsync(Modal(cut, "Move / Copy Conflict"), "Cancel");
		await pending;
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

	/// <summary>Dropping files onto a folder row moves them, or copies with Ctrl held.</summary>
	[Fact]
	public async Task Drop_FilesOntoFolder_MovesOrCopies()
	{
		var cut = RenderExplorer();
		await NavigateAsync(cut, "/Docs");
		var rows = Table(cut).Instance.ItemsToDisplay;

		await DropAsync(cut, rows.Single(x => x.Name == "Sub"), new List<FileExplorerItem> { rows.Single(x => x.Name == "a.docx") }, ctrl: false);
		await DropAsync(cut, rows.Single(x => x.Name == "Sub"), rows.Single(x => x.Name == "b.xlsx"), ctrl: true);

		_provider.Updates.Select(x => (x.Delta["Path"], x.Delta["Copy"])).Should().Equal(("/Docs/Sub/a.docx", false), ("/Docs/Sub/b.xlsx", true));
	}

	/// <summary>A drop with no target goes to the current folder; a tree node target is unwrapped to its folder.</summary>
	[Fact]
	public async Task Drop_NoTargetOrTreeNodeTarget_IsResolved()
	{
		var cut = RenderExplorer();
		await NavigateAsync(cut, "/Docs");
		var file = Table(cut).Instance.ItemsToDisplay.Single(x => x.Name == "a.docx");

		await DropAsync(cut, Node(cut, "/Empty"), file, ctrl: true);
		await NavigateAsync(cut, "/Docs/Sub");
		await DropAsync(cut, null, file, ctrl: true);

		_provider.Updates.Select(x => x.Delta["Path"]).Should().Equal("/Empty/a.docx", "/Docs/Sub/a.docx");
	}

	/// <summary>Drops onto a file, onto the item itself or its own sub-folder, or of items that cannot move, are ignored.</summary>
	[Fact]
	public async Task Drop_InvalidTargetsAndPayloads_AreIgnored()
	{
		var cut = RenderExplorer();
		await NavigateAsync(cut, "/Docs");
		var rows = Table(cut).Instance.ItemsToDisplay;
		var sub = rows.Single(x => x.Name == "Sub");
		var docs = _provider.Get("/Docs");

		await DropAsync(cut, rows.Single(x => x.Name == "a.docx"), rows.Single(x => x.Name == "b.xlsx"), ctrl: false);
		await DropAsync(cut, sub, sub, ctrl: false);
		await DropAsync(cut, sub, docs, ctrl: false);
		await DropAsync(cut, sub, rows.Single(x => x.Name == ".."), ctrl: false);
		await DropAsync(cut, sub, "not an item", ctrl: false);

		_provider.Updates.Should().BeEmpty();
	}

	/// <summary>
	/// A drop into a folder that cannot accept items is ignored, whether it lands on the folder's row or on the
	/// table whitespace while that folder is current (#174).
	/// </summary>
	[Fact]
	public async Task Drop_IntoReadOnlyFolder_IsIgnored()
	{
		// a different file for each drop, so that a drop wrongly accepted cannot raise a conflict prompt
		_provider.Items.Add(FileProvider.File("/Docs/c.txt", 1));
		var cut = RenderExplorer();
		await NavigateAsync(cut, "/Docs");
		var rows = Table(cut).Instance.ItemsToDisplay;
		var (a, b, c) = (rows.Single(x => x.Name == "a.docx"), rows.Single(x => x.Name == "b.xlsx"), rows.Single(x => x.Name == "c.txt"));
		await NavigateAsync(cut, "/");
		var media = Table(cut).Instance.ItemsToDisplay.Single(x => x.Name == "Media");

		await DropAsync(cut, media, a, ctrl: false);
		await DropAsync(cut, Node(cut, "/Media"), b, ctrl: true);
		await NavigateAsync(cut, "/Media");
		await DropAsync(cut, null, c, ctrl: true);

		_provider.Updates.Should().BeEmpty();
	}

	/// <summary>
	/// The ".." row is read-only itself, but a drop on it still moves the items into the parent folder it stands for
	/// when that folder accepts items.
	/// </summary>
	[Fact]
	public async Task Drop_OntoParentFolderRow_MovesIntoParent()
	{
		_provider.Items.Add(FileProvider.File("/Docs/Sub/d.txt", 1));
		var cut = RenderExplorer();
		await NavigateAsync(cut, "/Docs/Sub");
		var rows = Table(cut).Instance.ItemsToDisplay;

		await DropAsync(cut, rows.Single(x => x.Name == ".."), rows.Single(x => x.Name == "d.txt"), ctrl: false);

		_provider.Updates.Should().ContainSingle().Which.Delta["Path"].Should().Be("/Docs/d.txt");
	}

	/// <summary>Dropping the current folder somewhere else moves it and follows it to its target.</summary>
	[Fact]
	public async Task Drop_CurrentFolder_MovesIt_AndSelectsTarget()
	{
		var cut = RenderExplorer();
		await NavigateAsync(cut, "/Docs/Sub");
		var tree = Tree(cut);
		var current = tree.Instance.SelectedNode!.Data!;

		await tree.InvokeAsync(() => tree.Instance.Drop.InvokeAsync(new DropEventArgs(Node(cut, "/Empty"), current, false)));

		_provider.Updates.Should().ContainSingle().Which.Delta["Path"].Should().Be("/Empty/Sub");
		cut.Instance.FolderPath.Should().Be("/Empty");
	}
}
