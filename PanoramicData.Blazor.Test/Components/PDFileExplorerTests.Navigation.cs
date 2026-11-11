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

	/// <summary>Navigating to an item opens its folder once, without opening the folders above it, and selects it.</summary>
	[Fact]
	public async Task NavigateToItemAsync_OpensOnlyTheItemsFolder_AndSelectsTheItem()
	{
		_provider.Items.Remove(_provider.Get("/Docs/Sub"));
		_provider.Items.AddRange(
		[
			FileProvider.Dir("/Docs/Sub", hasSubFolders: true),
			FileProvider.Dir("/Docs/Sub/Deep"),
			FileProvider.File("/Docs/Sub/Deep/report.pdf", 100)
		]);
		var folders = new List<string>();
		FileExplorerItem[]? reported = null;
		var cut = RenderExplorer(p => p
			.Add(x => x.FolderChanged, (FileExplorerItem f) => folders.Add(f.Path))
			.Add(x => x.SelectionChanged, (FileExplorerItem[] s) => reported = s));
		await NavigateAsync(cut, "/Media");
		folders.Clear();
		_provider.Requests.Clear();

		var found = await cut.InvokeAsync(() => cut.Instance.NavigateToItemAsync("/Docs/Sub/Deep/report.pdf"));

		found.Should().BeTrue();
		cut.Instance.FolderPath.Should().Be("/Docs/Sub/Deep");
		folders.Should().Equal("/Docs/Sub/Deep");
		_provider.Requests.Should().Equal("/Docs", "/Docs/Sub", "/Docs/Sub/Deep");
		cut.Instance.SelectedFilesAndFolders.Should().Equal("/Docs/Sub/Deep/report.pdf");
		reported!.Select(x => x.Path).Should().Equal("/Docs/Sub/Deep/report.pdf");
		Tree(cut).Instance.SelectedNode!.Key.Should().Be("/Docs/Sub/Deep");
		cut.Instance.IsNavigating.Should().BeFalse();
		_common.Invocations["scrollIntoViewEx"].Should().ContainSingle()
			.Which.Arguments[0].Should().Be($"#{Table(cut).Instance.Id} tr[id=\"/Docs/Sub/Deep/report.pdf\"]");
	}

	/// <summary>A folder can be the item: its parent folder opens with the folder selected.</summary>
	[Fact]
	public async Task NavigateToItemAsync_AFolder_OpensItsParent_AndSelectsIt()
	{
		var cut = RenderExplorer();

		var found = await cut.InvokeAsync(() => cut.Instance.NavigateToItemAsync("/Docs/Sub"));

		found.Should().BeTrue();
		cut.Instance.FolderPath.Should().Be("/Docs");
		cut.Instance.SelectedFilesAndFolders.Should().Equal("/Docs/Sub");
	}

	/// <summary>An item that is no longer in its folder leaves the folder open, with nothing selected.</summary>
	[Fact]
	public async Task NavigateToItemAsync_AMissingItem_OpensItsFolder_AndReturnsFalse()
	{
		var cut = RenderExplorer();

		var found = await cut.InvokeAsync(() => cut.Instance.NavigateToItemAsync("/Docs/gone.docx"));

		found.Should().BeFalse();
		cut.Instance.FolderPath.Should().Be("/Docs");
		cut.Instance.SelectedFilesAndFolders.Should().BeEmpty();
	}

	/// <summary>A missing folder on the way stops at the deepest folder found.</summary>
	[Fact]
	public async Task NavigateToItemAsync_AMissingFolder_OpensTheDeepestFolderFound_AndReturnsFalse()
	{
		var cut = RenderExplorer();

		var found = await cut.InvokeAsync(() => cut.Instance.NavigateToItemAsync("/Docs/Gone/a.docx"));

		found.Should().BeFalse();
		cut.Instance.FolderPath.Should().Be("/Docs");
		cut.Instance.IsNavigating.Should().BeFalse();
	}

	/// <summary>A blank path, or the root itself, which is in no folder, is ignored.</summary>
	[Theory]
	[InlineData(" ")]
	[InlineData("/")]
	public async Task NavigateToItemAsync_NoItem_DoesNothing(string path)
	{
		var cut = RenderExplorer();
		await NavigateAsync(cut, "/Docs");

		var found = await cut.InvokeAsync(() => cut.Instance.NavigateToItemAsync(path));

		found.Should().BeFalse();
		cut.Instance.FolderPath.Should().Be("/Docs");
	}

	/// <summary>Navigating to a folder opens it once, without opening the folders above it or first showing its parent.</summary>
	[Fact]
	public async Task NavigateToFolderAsync_OpensOnlyTheFolder()
	{
		_provider.Items.Remove(_provider.Get("/Docs/Sub"));
		_provider.Items.AddRange(
		[
			FileProvider.Dir("/Docs/Sub", hasSubFolders: true),
			FileProvider.Dir("/Docs/Sub/Deep"),
			FileProvider.File("/Docs/Sub/Deep/report.pdf", 100)
		]);
		var folders = new List<string>();
		var cut = RenderExplorer(p => p.Add(x => x.FolderChanged, (FileExplorerItem f) => folders.Add(f.Path)));
		await NavigateAsync(cut, "/Media");
		folders.Clear();
		_provider.Requests.Clear();

		var found = await cut.InvokeAsync(() => cut.Instance.NavigateToFolderAsync("/Docs/Sub/Deep"));

		found.Should().BeTrue();
		cut.Instance.FolderPath.Should().Be("/Docs/Sub/Deep");
		folders.Should().Equal("/Docs/Sub/Deep");
		_provider.Requests.Should().Equal("/Docs", "/Docs/Sub", "/Docs/Sub/Deep");
		Tree(cut).Instance.SelectedNode!.Key.Should().Be("/Docs/Sub/Deep");
		cut.Instance.SelectedFilesAndFolders.Should().BeEmpty();
		RowNames(cut).Should().Equal("..", "report.pdf");
		cut.Instance.IsNavigating.Should().BeFalse();
	}

	/// <summary>A missing folder on the way stops at the deepest folder found.</summary>
	[Fact]
	public async Task NavigateToFolderAsync_AMissingFolder_OpensTheDeepestFolderFound_AndReturnsFalse()
	{
		var cut = RenderExplorer();

		var found = await cut.InvokeAsync(() => cut.Instance.NavigateToFolderAsync("/Docs/Gone"));

		found.Should().BeFalse();
		cut.Instance.FolderPath.Should().Be("/Docs");
		cut.Instance.IsNavigating.Should().BeFalse();
	}

	/// <summary>The root is a folder, so it can be opened.</summary>
	[Fact]
	public async Task NavigateToFolderAsync_TheRoot_OpensIt()
	{
		var cut = RenderExplorer();
		await NavigateAsync(cut, "/Docs");

		var found = await cut.InvokeAsync(() => cut.Instance.NavigateToFolderAsync("/"));

		found.Should().BeTrue();
		cut.Instance.FolderPath.Should().Be("/");
	}

	/// <summary>A blank path is ignored.</summary>
	[Fact]
	public async Task NavigateToFolderAsync_ABlankPath_DoesNothing()
	{
		var cut = RenderExplorer();
		await NavigateAsync(cut, "/Docs");

		var found = await cut.InvokeAsync(() => cut.Instance.NavigateToFolderAsync(" "));

		found.Should().BeFalse();
		cut.Instance.FolderPath.Should().Be("/Docs");
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
