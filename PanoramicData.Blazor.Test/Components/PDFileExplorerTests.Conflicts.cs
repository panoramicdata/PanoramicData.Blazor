using AwesomeAssertions;
using Bunit;
using PanoramicData.Blazor.Arguments;
using PanoramicData.Blazor.Models;

namespace PanoramicData.Blazor.Test.Components;

/// <summary>
/// Name conflict tests for moves and copies in <see cref="PDFileExplorer"/>.
/// </summary>
public partial class PDFileExplorerTests
{
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

	/// <summary>A folder renamed on copy gets "Copy" after its whole name, having no extension to keep.</summary>
	[Fact]
	public async Task PresetRename_Folder_AppendsCopyToTheName()
	{
		var cut = RenderExplorer(p => p.Add(x => x.ConflictResolution, ConflictResolutions.Rename));
		await NavigateAsync(cut, "/Docs");
		await SelectRowsAsync(cut, "/Docs/Sub");
		await KeyDownTableAsync(cut, "KeyC", ctrl: true);
		await Table(cut).InvokeAsync(() => Table(cut).Instance.ClearSelectionAsync());

		await KeyDownTableAsync(cut, "KeyV", ctrl: true);

		_provider.Updates.Should().ContainSingle().Which.Delta["Path"].Should().Be("/Docs/Sub Copy");
	}

	/// <summary>The root folder has no name, so renamed on copy it is simply called "Copy".</summary>
	[Fact]
	public async Task PresetRename_RootFolder_IsCalledCopy()
	{
		var cut = RenderExplorer(p => p.Add(x => x.ConflictResolution, ConflictResolutions.Rename));
		await KeyDownTreeAsync(cut, "KeyC", ctrl: true);
		await NavigateAsync(cut, "/Empty");

		await KeyDownTreeAsync(cut, "KeyV", ctrl: true);

		_provider.Updates.Should().ContainSingle().Which.Delta["Path"].Should().Be("/Empty/Copy");
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
}
