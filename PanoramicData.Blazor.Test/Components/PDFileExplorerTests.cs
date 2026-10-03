using AwesomeAssertions;
using Bunit;
using Microsoft.AspNetCore.Components.Web;
using PanoramicData.Blazor.Arguments;
using PanoramicData.Blazor.Extensions;
using PanoramicData.Blazor.Models;

namespace PanoramicData.Blazor.Test.Components;

/// <summary>
/// Tests for <see cref="PDFileExplorer"/>. The class is split across several files by area: this file holds the
/// shared fixture (an in-memory file system provider and helpers) and the rendering, toolbar and navigation tests.
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

	private static async Task NavigateAsync(IRenderedComponent<PDFileExplorer> cut, string path)
		=> await cut.InvokeAsync(() => cut.Instance.NavigateToAsync(path));

	private static async Task SelectRowsAsync(IRenderedComponent<PDFileExplorer> cut, params string[] paths)
	{
		var table = Table(cut);
		for (var i = 0; i < paths.Length; i++)
		{
			var path = paths[i];
			var ctrl = i > 0;
			await table.InvokeAsync(() => table.Instance.SelectItemAsync(path, false, ctrl));
		}
	}

	private static async Task ClickToolbarAsync(IRenderedComponent<PDFileExplorer> cut, string key)
	{
		var toolbar = cut.FindComponents<PDToolbar>().First(t => t.Instance.Items == cut.Instance.ToolbarItems);
		await toolbar.InvokeAsync(() => toolbar.Instance.ButtonClick.InvokeAsync(new KeyedEventArgs<MouseEventArgs>(key, new MouseEventArgs())));
	}

	private static async Task AnswerAsync(IRenderedComponent<PDModal> modal, string key)
		=> await modal.InvokeAsync(() => modal.Instance.OnButtonClick(new KeyedEventArgs<MouseEventArgs>(key, new MouseEventArgs())));

	private static async Task KeyDownTableAsync(IRenderedComponent<PDFileExplorer> cut, string code, bool ctrl = false)
	{
		var table = Table(cut);
		await table.InvokeAsync(() => table.Instance.KeyDown.InvokeAsync(new KeyboardEventArgs { Code = code, CtrlKey = ctrl }));
	}

	private static async Task KeyDownTreeAsync(IRenderedComponent<PDFileExplorer> cut, string code, bool ctrl = false)
	{
		var tree = Tree(cut);
		await tree.InvokeAsync(() => tree.Instance.KeyDown.InvokeAsync(new KeyboardEventArgs { Code = code, CtrlKey = ctrl }));
	}

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

	/// <summary>The toolbar gets its standard buttons, and without an upload URL no upload button or menu entries.</summary>
	[Fact]
	public void FirstRender_AddsStandardToolbarButtons()
	{
		var cut = RenderExplorer();

		cut.Instance.ToolbarItems.Select(x => x.Key).Should().Equal("navigate-up", "refresh", "create-folder", "delete");
		cut.Instance.TableContextItems.Select(x => x.Text).Should().NotContain("Upload Files");
		cut.Instance.TreeContextItems.Select(x => x.Text).Should().NotContain("Upload Files");
	}

	/// <summary>An upload URL adds the upload button and the upload menu entries.</summary>
	[Fact]
	public void UploadUrl_AddsUploadButtonAndMenuEntries()
	{
		var cut = RenderExplorer(p => p.Add(x => x.UploadUrl, "https://example.com/upload"));

		cut.Instance.ToolbarItems.Select(x => x.Key).Should().Equal("navigate-up", "refresh", "upload", "create-folder", "delete");
		cut.Instance.TableContextItems[1].Text.Should().Be("Upload Files");
		cut.Instance.TreeContextItems[0].Text.Should().Be("Upload Files");
	}

	/// <summary>On a touch device an Open button is added to the toolbar.</summary>
	[Fact]
	public void TouchDevice_AddsOpenButton()
	{
		_common.Setup<bool>("isTouchDevice").SetResult(true);

		var cut = RenderExplorer();

		cut.Instance.ToolbarItems.Select(x => x.Key).Should().Contain("open");
	}

	/// <summary>Hiding the toolbar hides its container; hiding the up button hides that button.</summary>
	[Fact]
	public void ShowToolbarAndNavigateUpFlags_HideElements()
	{
		var cut = RenderExplorer(p => p.Add(x => x.ShowToolbar, false).Add(x => x.ShowNavigateUpButton, false));

		cut.Find("div.pdfe-toolbar").ClassList.Should().Contain("d-none");
		ToolbarButton(cut, "navigate-up").IsVisible.Should().BeFalse();
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

	/// <summary>The navigate-up toolbar button moves to the parent folder.</summary>
	[Fact]
	public async Task NavigateUpButton_MovesToParent()
	{
		var cut = RenderExplorer();
		await NavigateAsync(cut, "/Docs/Sub");
		ToolbarButton(cut, "navigate-up").IsEnabled.Should().BeTrue();

		await ClickToolbarAsync(cut, "navigate-up");

		cut.Instance.FolderPath.Should().Be("/Docs");
	}

	/// <summary>At the root the navigate-up button is disabled.</summary>
	[Fact]
	public void NavigateUpButton_IsDisabledAtRoot()
	{
		var cut = RenderExplorer();

		ToolbarButton(cut, "navigate-up").IsEnabled.Should().BeFalse();
	}

	/// <summary>The refresh button re-queries the provider for the tree and the table.</summary>
	[Fact]
	public async Task RefreshButton_RequeriesTreeAndTable()
	{
		var cut = RenderExplorer();
		_provider.Requests.Clear();

		await ClickToolbarAsync(cut, "refresh");

		_provider.Requests.Should().Contain(string.Empty).And.Contain("/");
	}

	/// <summary>An unknown toolbar key is passed to the application.</summary>
	[Fact]
	public async Task UnknownToolbarKey_IsRaisedToTheApplication()
	{
		var clicked = new List<string>();
		var cut = RenderExplorer(p => p.Add(x => x.ToolbarClick, (string k) => clicked.Add(k)));

		await ClickToolbarAsync(cut, "custom");

		clicked.Should().Equal("custom");
	}

	/// <summary>The open button navigates into the selected folder.</summary>
	[Fact]
	public async Task OpenButton_NavigatesIntoSelectedFolder()
	{
		_common.Setup<bool>("isTouchDevice").SetResult(true);
		var cut = RenderExplorer();
		await SelectRowsAsync(cut, "/Docs");
		ToolbarButton(cut, "open").IsEnabled.Should().BeTrue();

		await ClickToolbarAsync(cut, "open");

		cut.Instance.FolderPath.Should().Be("/Docs");
	}

	/// <summary>The toolbar state reflects the selection and the current folder, and the application may alter it.</summary>
	[Fact]
	public async Task ToolbarState_TracksSelection_AndIsOfferedToTheApplication()
	{
		var updates = 0;
		var cut = RenderExplorer(p => p.Add(x => x.UpdateToolbarState, (List<ToolbarItem> _) => updates++));
		ToolbarButton(cut, "delete").IsEnabled.Should().BeFalse();
		ToolbarButton(cut, "create-folder").IsEnabled.Should().BeTrue();

		await SelectRowsAsync(cut, "/Docs");

		ToolbarButton(cut, "delete").IsEnabled.Should().BeTrue();
		updates.Should().BeGreaterThan(0);
	}

	/// <summary>In a read-only folder the create-folder and upload buttons are disabled.</summary>
	[Fact]
	public async Task ReadOnlyFolder_DisablesCreateAndUpload()
	{
		var cut = RenderExplorer(p => p.Add(x => x.UploadUrl, "https://example.com/upload"));

		await NavigateAsync(cut, "/Media");

		ToolbarButton(cut, "create-folder").IsEnabled.Should().BeFalse();
		ToolbarButton(cut, "upload").IsEnabled.Should().BeFalse();
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

	/// <summary>Disposing the explorer disposes its module without error.</summary>
	[Fact]
	public async Task Dispose_DoesNotThrow()
	{
		var cut = RenderExplorer();

		var act = async () => await cut.Instance.DisposeAsync();

		await act.Should().NotThrowAsync();
	}
}
