using AwesomeAssertions;
using Bunit;
using PanoramicData.Blazor.Models;

namespace PanoramicData.Blazor.Test.Components;

/// <summary>
/// Navigation and selection reporting tests for <see cref="PDFileExplorer"/>.
/// </summary>
public partial class PDFileExplorerTests
{
	/// <summary>Without auto expand nothing is selected and the table stays empty until a folder is chosen.</summary>
	[Fact]
	public void WithoutAutoExpand_NothingIsSelected()
	{
		var readyCount = 0;
		var cut = Render<PDFileExplorer>(p => p
			.Add(x => x.DataProvider, _provider)
			.Add(x => x.Ready, () => readyCount++));

		cut.WaitForAssertion(() => Tree(cut).Instance.RootNode.Nodes.Should().NotBeNullOrEmpty());
		cut.Instance.FolderPath.Should().BeEmpty();
		cut.Instance.GetTreeSelectedFolder().Should().BeNull();
		readyCount.Should().Be(0);
	}

	/// <summary>With auto expand the first node is selected and Ready is raised.</summary>
	[Fact]
	public void AutoExpand_SelectsRoot_AndRaisesReady()
	{
		var readyCount = 0;

		var cut = RenderExplorer(p => p.Add(x => x.Ready, () => readyCount++));

		cut.WaitForAssertion(() => readyCount.Should().Be(1));
		Tree(cut).Instance.SelectedNode!.Key.Should().Be("/");
	}

	/// <summary>Navigating to a path descends folder by folder, raising FolderChanged for each.</summary>
	[Fact]
	public async Task NavigateToAsync_DescendsFolderByFolder()
	{
		var folders = new List<string>();
		var cut = RenderExplorer(p => p.Add(x => x.FolderChanged, (FileExplorerItem f) => folders.Add(f.Path)));
		folders.Clear();

		await NavigateAsync(cut, "/Docs/Sub");

		cut.Instance.FolderPath.Should().Be("/Docs/Sub");
		folders.Should().Equal("/Docs", "/Docs/Sub");
		cut.Instance.IsNavigating.Should().BeFalse();
	}

	/// <summary>Navigating from a sub-folder goes back via the root.</summary>
	[Fact]
	public async Task NavigateToAsync_FromSubFolder_StartsAtRoot()
	{
		var cut = RenderExplorer();
		await NavigateAsync(cut, "/Docs");

		await NavigateAsync(cut, "/Media");

		cut.Instance.FolderPath.Should().Be("/Media");
	}

	/// <summary>A blank path is ignored.</summary>
	[Fact]
	public async Task NavigateToAsync_BlankPath_DoesNothing()
	{
		var cut = RenderExplorer();

		await NavigateAsync(cut, " ");

		cut.Instance.FolderPath.Should().Be("/");
	}

	/// <summary>SelectedFilesAndFolders lists the selected paths, and SelectionChanged carries the selected items.</summary>
	[Fact]
	public async Task Selection_IsReportedToTheApplication()
	{
		FileExplorerItem[]? reported = null;
		var cut = RenderExplorer(p => p.Add(x => x.SelectionChanged, (FileExplorerItem[] s) => reported = s));

		await SelectRowsAsync(cut, "/Docs", "/Media");

		cut.Instance.SelectedFilesAndFolders.Should().BeEquivalentTo("/Docs", "/Media");
		reported!.Select(x => x.Path).Should().BeEquivalentTo("/Docs", "/Media");
	}
}
