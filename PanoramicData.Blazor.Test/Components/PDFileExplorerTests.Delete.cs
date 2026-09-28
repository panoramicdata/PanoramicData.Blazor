using AwesomeAssertions;
using Bunit;
using PanoramicData.Blazor.Arguments;
using PanoramicData.Blazor.Models;

namespace PanoramicData.Blazor.Test.Components;

/// <summary>
/// Delete behaviour of <see cref="PDFileExplorer"/>: the confirmation prompt, application overrides, and the
/// provider calls for files and folders.
/// </summary>
public partial class PDFileExplorerTests
{
	private static void WaitForDeletePrompt(IRenderedComponent<PDFileExplorer> cut, string message)
		=> cut.WaitForAssertion(() => Modal(cut, "Delete").Find(".modal-body").TextContent.Should().Contain(message));

	/// <summary>Deleting one selected file asks by name, and Yes deletes it and refreshes.</summary>
	[Fact]
	public async Task DeleteFile_Confirmed_DeletesAndRefreshes()
	{
		var cut = RenderExplorer();
		await NavigateAsync(cut, "/Docs");
		await SelectRowsAsync(cut, "/Docs/a.docx");

		var pending = cut.InvokeAsync(() => ClickToolbarAsync(cut, "delete"));
		WaitForDeletePrompt(cut, "Are you sure you wish to delete 'a.docx'?");
		await AnswerAsync(Modal(cut, "Delete"), "yes");
		await pending;

		_provider.Deletes.Should().Equal("/Docs/a.docx");
		RowNames(cut).Should().NotContain("a.docx");
	}

	/// <summary>Deleting several items asks with a count, and No deletes nothing.</summary>
	[Fact]
	public async Task DeleteFiles_Declined_DeletesNothing()
	{
		var cut = RenderExplorer();
		await NavigateAsync(cut, "/Docs");
		await SelectRowsAsync(cut, "/Docs/a.docx", "/Docs/b.xlsx");

		var pending = cut.InvokeAsync(() => KeyDownTableAsync(cut, "Delete"));
		WaitForDeletePrompt(cut, "Are you sure you wish to delete these 2 items?");
		await AnswerAsync(Modal(cut, "Delete"), "no");
		await pending;

		_provider.Deletes.Should().BeEmpty();
	}

	/// <summary>The application may decide the delete itself, skipping the prompt, and a failing item is reported without stopping the rest.</summary>
	[Fact]
	public async Task DeleteFiles_ApplicationConfirms_DeletesEachAndReportsFailures()
	{
		_provider.ThrowOnDeletePath = "/Docs/a.docx";
		DeleteArgs? request = null;
		var cut = RenderExplorer(p => p.Add(x => x.DeleteRequest, (DeleteArgs a) => { request = a; a.Resolution = DeleteArgs.DeleteResolutions.Delete; }));
		await NavigateAsync(cut, "/Docs");
		await SelectRowsAsync(cut, "/Docs/a.docx", "/Docs/b.xlsx");

		await ClickMenuAsync(TableMenu(cut), "Delete");

		request!.Items.Select(x => x.Path).Should().BeEquivalentTo("/Docs/a.docx", "/Docs/b.xlsx");
		_provider.Deletes.Should().Equal("/Docs/b.xlsx");
		_exceptions.Should().ContainSingle().Which.Message.Should().Contain("cannot delete");
	}

	/// <summary>The application may cancel a delete, so nothing is deleted.</summary>
	[Fact]
	public async Task DeleteFiles_ApplicationCancels_DeletesNothing()
	{
		var cut = RenderExplorer(p => p.Add(x => x.DeleteRequest, (DeleteArgs a) => a.Resolution = DeleteArgs.DeleteResolutions.Cancel));
		await SelectRowsAsync(cut, "/Docs");

		await ClickToolbarAsync(cut, "delete");

		_provider.Deletes.Should().BeEmpty();
	}

	/// <summary>Deleting a folder from the tree asks by name, and Yes removes it and selects its parent.</summary>
	[Fact]
	public async Task DeleteFolder_Confirmed_RemovesNodeAndSelectsParent()
	{
		var cut = RenderExplorer();
		await NavigateAsync(cut, "/Docs/Sub");

		var pending = cut.InvokeAsync(() => ClickMenuAsync(TreeMenu(cut), "Delete"));
		WaitForDeletePrompt(cut, "Are you sure you wish to delete 'Sub'?");
		await AnswerAsync(Modal(cut, "Delete"), "yes");
		await pending;

		_provider.Deletes.Should().Equal("/Docs/Sub");
		Tree(cut).Instance.RootNode.Find("/Docs/Sub").Should().BeNull();
		cut.Instance.FolderPath.Should().Be("/Docs");
	}

	/// <summary>Declining a folder delete leaves it in place.</summary>
	[Fact]
	public async Task DeleteFolder_Declined_KeepsFolder()
	{
		var cut = RenderExplorer();
		await NavigateAsync(cut, "/Docs/Sub");

		var pending = cut.InvokeAsync(() => KeyDownTreeAsync(cut, "Delete"));
		WaitForDeletePrompt(cut, "'Sub'");
		await AnswerAsync(Modal(cut, "Delete"), "no");
		await pending;

		_provider.Deletes.Should().BeEmpty();
		Tree(cut).Instance.RootNode.Find("/Docs/Sub").Should().NotBeNull();
	}

	/// <summary>A failing folder delete is reported and the node is kept.</summary>
	[Fact]
	public async Task DeleteFolder_ProviderThrows_IsReported()
	{
		_provider.ThrowOnDeletePath = "/Docs/Sub";
		var cut = RenderExplorer(p => p.Add(x => x.DeleteRequest, (DeleteArgs a) => a.Resolution = DeleteArgs.DeleteResolutions.Delete));
		await NavigateAsync(cut, "/Docs/Sub");

		await ClickMenuAsync(TreeMenu(cut), "Delete");

		_exceptions.Should().ContainSingle();
		Tree(cut).Instance.RootNode.Find("/Docs/Sub").Should().NotBeNull();
	}

	/// <summary>The Delete key does nothing on a tree folder that cannot be deleted.</summary>
	[Fact]
	public async Task TreeDeleteKey_OnUndeletableFolder_DoesNothing()
	{
		_provider.Get("/Docs").CanDelete = false;
		var deleteRequests = 0;
		var cut = RenderExplorer(p => p.Add(x => x.DeleteRequest, (DeleteArgs _) => deleteRequests++));
		await NavigateAsync(cut, "/Docs");

		await KeyDownTreeAsync(cut, "Delete");

		deleteRequests.Should().Be(0);
	}
}
