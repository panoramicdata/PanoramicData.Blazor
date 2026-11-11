using AwesomeAssertions;
using Bunit;
using Microsoft.JSInterop;
using PanoramicData.Blazor.Models;

namespace PanoramicData.Blazor.Test.Components;

/// <summary>
/// How <see cref="PDFileExplorer"/> contains failures: given an ExceptionHandler, a failure in a handler is passed to it
/// rather than ending the Blazor circuit, and without one it is thrown as before.
/// </summary>
public partial class PDFileExplorerTests
{
	private static void FailOnFolderChange(FileExplorerItem folder) => throw new InvalidOperationException($"Cannot open {folder.Path}");

	private static Task SelectTreeNodeAsync(IRenderedComponent<PDFileExplorer> cut, string path)
		=> Tree(cut).InvokeAsync(() => Tree(cut).Instance.SelectNode(cut.Instance.TreeRootNode!.Find(path)!, false));

	/// <summary>A failing application handler is passed to ExceptionHandler, and the folder still opens.</summary>
	[Fact]
	public async Task AFailingHandler_IsPassedToTheExceptionHandler()
	{
		var cut = RenderExplorer(p => p.Add(x => x.FolderChanged, folder => FailOnFolderChange(folder)));
		_exceptions.Should().ContainSingle().Which.Message.Should().Be("Cannot open /", "opening the root on the first render fails too");
		_exceptions.Clear();

		await SelectTreeNodeAsync(cut, "/Docs");

		_exceptions.Should().ContainSingle().Which.Message.Should().Be("Cannot open /Docs");
		cut.Instance.FolderPath.Should().Be("/Docs");
		RowNames(cut).Should().Equal("..", "Sub", "a.docx", "b.xlsx");
	}

	/// <summary>Without an ExceptionHandler a failing handler is thrown, as it always was.</summary>
	[Fact]
	public async Task WithoutAnExceptionHandler_AFailingHandler_IsThrownAsBefore()
	{
		var cut = Render<PDFileExplorer>(p => p
			.Add(x => x.DataProvider, _provider)
			.Add(x => x.AutoExpand, true)
			.Add(x => x.FolderChanged, folder => FailOnFolderChange(folder)));
		cut.WaitForAssertion(() => Tree(cut).Instance.SelectedNode.Should().NotBeNull());

		var act = () => SelectTreeNodeAsync(cut, "/Docs");

		await act.Should().ThrowAsync<InvalidOperationException>().WithMessage("Cannot open /Docs");
	}

	/// <summary>A toolbar state handler that fails on the first render is passed on, and the explorer still loads.</summary>
	[Fact]
	public void AFailingToolbarStateHandler_OnTheFirstRender_IsPassedOn()
	{
		static void FailToUpdateToolbar(List<ToolbarItem> items) => throw new InvalidOperationException("No toolbar state");

		var cut = RenderExplorer(p => p.Add(x => x.UpdateToolbarState, items => FailToUpdateToolbar(items)));

		cut.WaitForAssertion(() => _exceptions.Should().NotBeEmpty());
		_exceptions.Should().AllSatisfy(ex => ex.Message.Should().Be("No toolbar state"));
		RowNames(cut).Should().Equal("Docs", "Empty", "Media");
	}

	/// <summary>An item is still selected when the browser cannot scroll it into view.</summary>
	[Fact]
	public async Task NavigateToItemAsync_WhenScrollingFails_StillSelectsTheItem()
	{
		var cut = RenderExplorer();
		_common.SetupVoid("scrollIntoView", _ => true).SetException(new JSException("No scrolling"));
		_common.SetupVoid("scrollIntoViewEx", _ => true).SetException(new JSException("No scrolling"));

		var found = await cut.InvokeAsync(() => cut.Instance.NavigateToItemAsync("/Docs/a.docx"));

		found.Should().BeTrue();
		cut.Instance.SelectedFilesAndFolders.Should().Equal("/Docs/a.docx");
		_exceptions.Should().BeEmpty();
	}

	/// <summary>A navigation started while another is still loading the tree supersedes it.</summary>
	[Fact]
	public async Task NavigateToItemAsync_ALaterCall_SupersedesOneStillLoading()
	{
		var docsAnswer = new TaskCompletionSource();
		var cut = RenderExplorer();
		_provider.BeforeAnswer = folder => folder == "/Docs" ? docsAnswer.Task : Task.CompletedTask;

		var first = cut.InvokeAsync(() => cut.Instance.NavigateToItemAsync("/Docs/Sub/notes.txt"));
		var second = await cut.InvokeAsync(() => cut.Instance.NavigateToItemAsync("/Media/pic.png"));
		docsAnswer.SetResult();

		(await first).Should().BeFalse();
		second.Should().BeTrue();
		cut.Instance.FolderPath.Should().Be("/Media");
		cut.Instance.SelectedFilesAndFolders.Should().Equal("/Media/pic.png");
		cut.Instance.IsNavigating.Should().BeFalse();
	}
}
