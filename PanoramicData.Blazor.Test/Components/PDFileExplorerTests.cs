using AwesomeAssertions;
using Bunit;
using PanoramicData.Blazor.Extensions;
using PanoramicData.Blazor.Models;

namespace PanoramicData.Blazor.Test.Components;

/// <summary>
/// Tests for <see cref="PDFileExplorer"/>. The class is split across several files by area: this file holds the
/// shared fixture (the rendering helper and the child component finders) and the first-render and disposal tests.
/// </summary>
/// <remarks>
/// Most of the explorer's behaviour sits in private handlers wired to its child tree, table, drop zones, context
/// menus and modals. The tests reach them the way the running component does: by raising the child component's
/// event callback, or by using the child's public API, and then asserting on the explorer's observable state,
/// its markup and the calls it made to the data provider.
/// </remarks>
public partial class PDFileExplorerTests : BunitContext
{
	private const string SplitterModulePath = "./_content/PanoramicData.Blazor/PDSplitter.razor.js";

	private readonly FileProvider _provider = new();
	private readonly BunitJSModuleInterop _common;
	private readonly BunitJSModuleInterop _splitter;
	private readonly List<Exception> _exceptions = [];

	/// <summary>Sets up the rendering context, the shared JavaScript module and the splitter module.</summary>
	public PDFileExplorerTests()
	{
		JSInterop.Mode = JSRuntimeMode.Loose;
		Services.AddPanoramicDataBlazor();
		_common = JSInterop.SetupModule(JSInteropVersionHelper.CommonJsUrl);
		_splitter = JSInterop.SetupModule(SplitterModulePath);
		_splitter.Setup<bool>("hasSplitJs").SetResult(true);
		_splitter.Setup<double[]>("getSizes", _ => true).SetResult([20, 60, 20]);
	}

	private IRenderedComponent<PDFileExplorer> RenderExplorer(Action<ComponentParameterCollectionBuilder<PDFileExplorer>>? configure = null)
	{
		var cut = Render<PDFileExplorer>(p =>
		{
			p.Add(x => x.DataProvider, _provider);
			p.Add(x => x.AutoExpand, true);
			p.Add(x => x.ExceptionHandler, (Exception ex) => _exceptions.Add(ex));
			configure?.Invoke(p);
		});

		cut.WaitForAssertion(() => cut.Instance.FolderPath.Should().Be("/"));
		return cut;
	}

	private static IRenderedComponent<PDTree<FileExplorerItem>> Tree(IRenderedComponent<PDFileExplorer> cut)
		=> cut.FindComponent<PDTree<FileExplorerItem>>();

	private static IRenderedComponent<PDTable<FileExplorerItem>> Table(IRenderedComponent<PDFileExplorer> cut)
		=> cut.FindComponent<PDTable<FileExplorerItem>>();

	private static IRenderedComponent<PDModal> Modal(IRenderedComponent<PDFileExplorer> cut, string title)
		=> cut.FindComponents<PDModal>().Single(m => m.Instance.Title == title);

	private static IRenderedComponent<PDModal> UploadModal(IRenderedComponent<PDFileExplorer> cut)
		=> cut.FindComponents<PDModal>().Single(m => m.Instance.Size == ModalSizes.Large);

	private static IRenderedComponent<PDContextMenu> TreeMenu(IRenderedComponent<PDFileExplorer> cut)
		=> cut.FindComponents<PDContextMenu>()[0];

	private static IRenderedComponent<PDContextMenu> TableMenu(IRenderedComponent<PDFileExplorer> cut)
		=> cut.FindComponents<PDContextMenu>()[1];

	private static string[] RowNames(IRenderedComponent<PDFileExplorer> cut)
		=> [.. Table(cut).Instance.ItemsToDisplay.Select(x => x.Name)];

	private static ToolbarItem ToolbarButton(IRenderedComponent<PDFileExplorer> cut, string key)
		=> cut.Instance.ToolbarItems.Single(x => x.Key == key);

	/// <summary>The explorer renders its toolbar, both panes and the standard columns, and loads the root folder.</summary>
	[Fact]
	public void FirstRender_LoadsRootFolder_IntoTreeAndTable()
	{
		var cut = RenderExplorer();

		cut.Find("div.pdfileexplorer").Should().NotBeNull();
		cut.Find("div.pdfe-toolbar").ClassList.Should().NotContain("d-none");
		cut.Instance.Id.Should().StartWith("pdfe");
		RowNames(cut).Should().Equal("Docs", "Empty", "Media");
		cut.Instance.GetTreeSelectedFolder()!.Path.Should().Be("/");
		cut.Instance.TreeRootNode!.Find("/").Should().NotBeNull();
		cut.Instance.FileItems.Should().HaveCount(3);
	}

	/// <summary>A whitespace new-folder name is rejected as a parameter.</summary>
	[Fact]
	public void BlankNewFolderName_Throws()
	{
		var act = () => Render<PDFileExplorer>(p => p.Add(x => x.DataProvider, _provider).Add(x => x.NewFolderName, " "));

		act.Should().Throw<ArgumentException>().WithMessage("*NewFolderName*");
	}

	/// <summary>The delete, conflict and upload dialogs have their buttons replaced on first render.</summary>
	[Fact]
	public void FirstRender_ConfiguresDialogButtons()
	{
		var cut = RenderExplorer();

		Modal(cut, "Delete").Instance.Buttons.Select(x => x.Key).Should().Equal("yes", "no");
		Modal(cut, "Move / Copy Conflict").Instance.Buttons.Select(x => x.Key).Should().Equal("Overwrite", "Rename", "Skip", "Cancel");
		var upload = UploadModal(cut).Instance.Buttons;
		upload.Single(x => x.Key == "Yes").IsVisible.Should().BeFalse();
		((ToolbarButton)upload.Single(x => x.Key == "No")).Text.Should().Be("Close");
	}

	/// <summary>Disposing the explorer disposes its module without error.</summary>
	[Fact]
	public async Task Dispose_DoesNotThrow()
	{
		var cut = RenderExplorer();

		var act = async () => await cut.Instance.DisposeAsync();

		await act.Should().NotThrowAsync();
	}
}
