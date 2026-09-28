using AngleSharp.Dom;
using AwesomeAssertions;
using Bunit;
using Microsoft.AspNetCore.Components.Web;
using PanoramicData.Blazor.Extensions;
using PanoramicData.Blazor.Models;

namespace PanoramicData.Blazor.Test;

/// <summary>
/// Tests that <see cref="PDFileModal"/> opens in open and save modes, reflects the user's selection and
/// typed filename in its OK button, and reports the chosen path, confirming before an overwrite.
/// </summary>
public class PDFileModalTests : BunitContext
{
	private readonly List<string> _results = [];

	/// <summary>Sets up the rendering context.</summary>
	public PDFileModalTests()
	{
		JSInterop.Mode = JSRuntimeMode.Loose;
		Services.AddPanoramicDataBlazor();
	}

	/// <summary>Verifies that open mode shows the open title and button, disabled, with no filename box.</summary>
	[Fact]
	public async Task Open_mode_shows_a_disabled_open_button()
	{
		var component = RenderModal();

		await ShowOpenAsync(component);

		component.Find(".modal-title").TextContent.Should().Be("File Open");
		OkButton(component).TextContent.Trim().Should().Be("Open");
		OkButton(component).HasAttribute("disabled").Should().BeTrue();
		FilenameBox(component).ClassList.Should().Contain("d-none");
	}

	/// <summary>Verifies that selecting a file enables Open, and Open reports the file's path.</summary>
	[Fact]
	public async Task Opening_a_selected_file_reports_its_path()
	{
		var component = RenderModal();
		await ShowOpenAsync(component);

		await Row(component, "/readme.txt").MouseUpAsync(new MouseEventArgs());
		OkButton(component).HasAttribute("disabled").Should().BeFalse();
		await OkButton(component).ClickAsync(new());

		_results.Should().Equal("/readme.txt");
	}

	/// <summary>Verifies that selecting a folder in file mode leaves Open disabled.</summary>
	[Fact]
	public async Task Selecting_a_folder_in_file_mode_leaves_open_disabled()
	{
		var component = RenderModal();
		await ShowOpenAsync(component);

		await Row(component, "/Docs").MouseUpAsync(new MouseEventArgs());

		OkButton(component).HasAttribute("disabled").Should().BeTrue();
	}

	/// <summary>Verifies that Cancel reports an empty result.</summary>
	[Fact]
	public async Task Cancel_reports_an_empty_result()
	{
		var component = RenderModal();
		await ShowOpenAsync(component);

		await FooterButton(component, "Cancel").ClickAsync(new());

		_results.Should().Equal(string.Empty);
	}

	/// <summary>Verifies that double-clicking a file opens it at once.</summary>
	[Fact]
	public async Task Double_clicking_a_file_opens_it()
	{
		var component = RenderModal();
		await ShowOpenAsync(component);

		await Row(component, "/readme.txt").DoubleClickAsync(new MouseEventArgs());

		_results.Should().Equal("/readme.txt");
	}

	/// <summary>Verifies that double-clicking a folder does not open it as a result.</summary>
	[Fact]
	public async Task Double_clicking_a_folder_reports_nothing()
	{
		var component = RenderModal();
		await ShowOpenAsync(component);

		await Row(component, "/Docs").DoubleClickAsync(new MouseEventArgs());

		_results.Should().BeEmpty();
	}

	/// <summary>Verifies that in folder mode the folder navigated to can be chosen, and its files are not listed.</summary>
	[Fact]
	public async Task Folder_mode_chooses_the_current_folder()
	{
		var show = SetupPendingShow();
		var component = RenderModal();

		await ShowFolderOpenAsync(component, show, "/Docs");
		component.WaitForAssertion(() => OkButton(component).HasAttribute("disabled").Should().BeFalse());
		component.FindAll("tr[id='/Docs/notes.md']").Should().BeEmpty();
		await OkButton(component).ClickAsync(new());

		_results.Should().Equal("/Docs");
	}

	/// <summary>Verifies that in folder mode a selected folder is chosen in preference to the current one.</summary>
	[Fact]
	public async Task Folder_mode_chooses_a_selected_folder()
	{
		var show = SetupPendingShow();
		var component = RenderModal();
		await ShowFolderOpenAsync(component, show);
		component.WaitForAssertion(() => Row(component, "/Docs"));

		await Row(component, "/Docs").MouseUpAsync(new MouseEventArgs());
		await OkButton(component).ClickAsync(new());

		_results.Should().Equal("/Docs");
	}

	/// <summary>Verifies that CanSelectFolder can refuse a folder, keeping OK disabled.</summary>
	[Fact]
	public async Task CanSelectFolder_can_refuse_a_folder()
	{
		var show = SetupPendingShow();
		var component = RenderModal(p => p.Add(x => x.CanSelectFolder, item => item.Path != "/Docs" && item.Path != "/"));
		await ShowFolderOpenAsync(component, show);
		component.WaitForAssertion(() => Row(component, "/Docs"));

		await Row(component, "/Docs").MouseUpAsync(new MouseEventArgs());

		OkButton(component).HasAttribute("disabled").Should().BeTrue();
	}

	/// <summary>Verifies that save mode shows the save title and button, and the initial filename.</summary>
	[Fact]
	public async Task Save_mode_shows_the_initial_filename()
	{
		var component = RenderModal();

		await component.InvokeAsync(() => component.Instance.ShowSaveAsAsync("/new.md"));

		component.Find(".modal-title").TextContent.Should().Be("File Save");
		OkButton(component).TextContent.Trim().Should().Be("Save");
		OkButton(component).HasAttribute("disabled").Should().BeFalse();
		FilenameBox(component).GetAttribute("value").Should().Be("new.md");
		FilenameBox(component).ClassList.Should().NotContain("d-none");
	}

	/// <summary>Verifies that saving reports the current folder joined with the filename.</summary>
	[Fact]
	public async Task Saving_reports_the_folder_and_filename()
	{
		var component = RenderModal();
		await component.InvokeAsync(() => component.Instance.ShowSaveAsAsync());
		component.WaitForAssertion(() => Row(component, "/readme.txt"));

		await TypeFilenameAsync(component, "new.md");
		await OkButton(component).ClickAsync(new());

		_results.Should().Equal("/new.md");
	}

	/// <summary>Verifies that the Save button follows whether a filename has been typed.</summary>
	[Fact]
	public async Task The_save_button_follows_the_typed_filename()
	{
		var component = RenderModal();
		await component.InvokeAsync(() => component.Instance.ShowSaveAsAsync());

		await TypeFilenameAsync(component, "new.md");
		OkButton(component).HasAttribute("disabled").Should().BeFalse();

		await TypeFilenameAsync(component, " ");
		OkButton(component).HasAttribute("disabled").Should().BeTrue();
	}

	/// <summary>Verifies that pressing Enter in the filename box saves.</summary>
	[Fact]
	public async Task Enter_in_the_filename_box_saves()
	{
		var component = RenderModal();
		await component.InvokeAsync(() => component.Instance.ShowSaveAsAsync());
		component.WaitForAssertion(() => Row(component, "/readme.txt"));

		await TypeFilenameAsync(component, "new.md");
		await FilenameBox(component).KeyUpAsync(new KeyboardEventArgs { Code = "Enter", Key = "Enter" });

		component.WaitForAssertion(() => _results.Should().Equal("/new.md"));
	}

	/// <summary>Verifies that saving over an existing file asks first, and confirming reports the path.</summary>
	[Fact]
	public async Task Confirming_an_overwrite_reports_the_path()
	{
		var component = RenderModal();
		await component.InvokeAsync(() => component.Instance.ShowSaveAsAsync());
		component.WaitForAssertion(() => Row(component, "/readme.txt"));
		await TypeFilenameAsync(component, "readme.txt");

		var save = OkButton(component).ClickAsync(new());
		component.WaitForAssertion(() => _results.Should().BeEmpty());
		await ConfirmButton(component, "Yes").ClickAsync(new());
		await save;

		_results.Should().Equal("/readme.txt");
		ConfirmDialog(component).TextContent.Should().Contain("readme.txt");
	}

	/// <summary>Verifies that declining an overwrite reports nothing and leaves the dialog open.</summary>
	[Fact]
	public async Task Declining_an_overwrite_reports_nothing()
	{
		var component = RenderModal();
		await component.InvokeAsync(() => component.Instance.ShowSaveAsAsync());
		component.WaitForAssertion(() => Row(component, "/readme.txt"));
		await TypeFilenameAsync(component, "readme.txt");

		var save = OkButton(component).ClickAsync(new());
		await ConfirmButton(component, "No").ClickAsync(new());
		await save;

		_results.Should().BeEmpty();
	}

	/// <summary>Verifies that waiting for an open returns the selected file's path.</summary>
	[Fact]
	public async Task Waiting_for_an_open_returns_the_selected_path()
	{
		var component = RenderModal();
		await ShowOpenAsync(component);

		var result = component.InvokeAsync(() => component.Instance.ShowOpenAndWaitResultAsync());
		await Row(component, "/readme.txt").MouseUpAsync(new MouseEventArgs());
		await OkButton(component).ClickAsync(new());

		(await result).Should().Be("/readme.txt");
		_results.Should().BeEmpty();
	}

	/// <summary>Verifies that waiting for an open returns an empty path when cancelled.</summary>
	[Fact]
	public async Task Waiting_for_an_open_returns_empty_when_cancelled()
	{
		var component = RenderModal();
		await ShowOpenAsync(component);

		var result = component.InvokeAsync(() => component.Instance.ShowOpenAndWaitResultAsync(folderSelect: true));
		await FooterButton(component, "Cancel").ClickAsync(new());

		(await result).Should().BeEmpty();
	}

	/// <summary>Verifies that waiting for a save returns the path, after an overwrite has been declined and a new name typed.</summary>
	[Fact]
	public async Task Waiting_for_a_save_asks_again_after_a_declined_overwrite()
	{
		var component = RenderModal();

		var result = component.InvokeAsync(() => component.Instance.ShowSaveAsAndWaitResultAsync());
		component.WaitForAssertion(() => Row(component, "/readme.txt"));
		await TypeFilenameAsync(component, "readme.txt");
		await OkButton(component).ClickAsync(new());
		await ConfirmButton(component, "No").ClickAsync(new());
		await TypeFilenameAsync(component, "other.txt");
		await OkButton(component).ClickAsync(new());

		(await result).Should().Be("/other.txt");
	}

	/// <summary>Verifies that waiting for a save returns the path when an overwrite is confirmed.</summary>
	[Fact]
	public async Task Waiting_for_a_save_returns_the_path_when_overwrite_confirmed()
	{
		var component = RenderModal();

		var result = component.InvokeAsync(() => component.Instance.ShowSaveAsAndWaitResultAsync("/readme.txt"));
		component.WaitForAssertion(() => Row(component, "/readme.txt"));
		await OkButton(component).ClickAsync(new());
		await ConfirmButton(component, "Yes").ClickAsync(new());

		(await result).Should().Be("/readme.txt");
	}

	/// <summary>Verifies that waiting for a save returns an empty path when cancelled.</summary>
	[Fact]
	public async Task Waiting_for_a_save_returns_empty_when_cancelled()
	{
		var component = RenderModal();

		var result = component.InvokeAsync(() => component.Instance.ShowSaveAsAndWaitResultAsync());
		component.WaitForAssertion(() => Row(component, "/readme.txt"));
		await FooterButton(component, "Cancel").ClickAsync(new());

		(await result).Should().BeEmpty();
	}

	/// <summary>Verifies that button texts, titles and icons can be replaced.</summary>
	[Fact]
	public async Task Texts_and_icons_can_be_replaced()
	{
		var component = RenderModal(p => p
			.Add(x => x.OpenTitle, "Pick")
			.Add(x => x.OpenButtonText, "Choose")
			.Add(x => x.GetItemIconCssClass, item => $"icon-{item.Name}"));

		await ShowOpenAsync(component);

		component.Find(".modal-title").TextContent.Should().Be("Pick");
		OkButton(component).TextContent.Trim().Should().Be("Choose");
		Row(component, "/readme.txt").QuerySelector(".icon-readme\\.txt").Should().NotBeNull();
	}

	/// <summary>Verifies that refreshing asks the data provider for the folder contents again.</summary>
	[Fact]
	public async Task Refreshing_requests_the_contents_again()
	{
		var provider = new FileProvider();
		var component = RenderModal(provider: provider);
		await ShowOpenAsync(component);
		var before = provider.Requests;

		await component.InvokeAsync(component.Instance.RefreshFileExplorerAsync);

		provider.Requests.Should().BeGreaterThan(before);
	}

	private IRenderedComponent<PDFileModal> RenderModal(
		Action<ComponentParameterCollectionBuilder<PDFileModal>>? configure = null,
		FileProvider? provider = null)
		=> Render<PDFileModal>(parameters =>
		{
			parameters
				.Add(p => p.DataProvider, provider ?? new FileProvider())
				.Add(p => p.ModalHidden, (string result) => _results.Add(result));
			configure?.Invoke(parameters);
		});

	/// <summary>
	/// Makes the dialog's show call wait until released, as it does in a browser, so the dialog renders its new
	/// mode before the explorer loads the folder.
	/// </summary>
	private JSRuntimeInvocationHandler SetupPendingShow()
		=> JSInterop.SetupModule("./_content/PanoramicData.Blazor/PDModal.razor.js").SetupModule("initialize", _ => true).SetupVoid("show");

	private static async Task ShowFolderOpenAsync(IRenderedComponent<PDFileModal> component, JSRuntimeInvocationHandler show, string initialFolder = "")
	{
		var opening = component.InvokeAsync(() => component.Instance.ShowOpenAsync(folderSelect: true, initialFolder: initialFolder));
		show.SetVoidResult();
		await opening;
	}

	private static async Task ShowOpenAsync(IRenderedComponent<PDFileModal> component)
	{
		await component.InvokeAsync(() => component.Instance.ShowOpenAsync());
		component.WaitForAssertion(() => Row(component, "/readme.txt"));
	}

	/// <summary>Types a filename as a user does: the text box reports its value on key up.</summary>
	private static async Task TypeFilenameAsync(IRenderedComponent<PDFileModal> component, string text)
	{
		await FilenameBox(component).InputAsync(new() { Value = text });
		await FilenameBox(component).KeyUpAsync(new KeyboardEventArgs { Key = "a", Code = "KeyA" });
	}

	private static IElement Row(IRenderedComponent<PDFileModal> component, string path)
		=> component.Find($"tr.pdtablerow[id='{path}']");

	private static IElement Footer(IRenderedComponent<PDFileModal> component)
		=> component.Find($"#{component.Instance.Modal.Id} > .modal-dialog > .modal-content > .modal-footer");

	private static IElement FooterButton(IRenderedComponent<PDFileModal> component, string key)
		=> Footer(component).QuerySelector($"#pd-tbr-btn-{key}")!;

	private static IElement OkButton(IRenderedComponent<PDFileModal> component) => FooterButton(component, "OK");

	private static IElement FilenameBox(IRenderedComponent<PDFileModal> component)
		=> Footer(component).QuerySelector("input[type=text]")!;

	private static IElement ConfirmDialog(IRenderedComponent<PDFileModal> component)
		=> component.FindAll(".modal").First(m => m.QuerySelector(".modal-title")?.TextContent == "Confirm Overwrite");

	private static IElement ConfirmButton(IRenderedComponent<PDFileModal> component, string key)
		=> ConfirmDialog(component).QuerySelector($".modal-footer #pd-tbr-btn-{key}")!;

	/// <summary>An in-memory folder tree: a root holding one folder and one file, the folder holding one file.</summary>
	private sealed class FileProvider : DataProviderBase<FileExplorerItem>
	{
		private readonly List<FileExplorerItem> _items =
		[
			new() { Path = "/", Name = "", EntryType = FileExplorerItemType.Directory, HasSubFolders = true },
			new() { Path = "/Docs", Name = "Docs", EntryType = FileExplorerItemType.Directory, HasSubFolders = false },
			new() { Path = "/readme.txt", Name = "readme.txt", EntryType = FileExplorerItemType.File, FileSize = 10 },
			new() { Path = "/Docs/notes.md", Name = "notes.md", EntryType = FileExplorerItemType.File, FileSize = 20 }
		];

		public int Requests { get; private set; }

		public override Task<DataResponse<FileExplorerItem>> GetDataAsync(DataRequest<FileExplorerItem> request, CancellationToken cancellationToken)
		{
			cancellationToken.ThrowIfCancellationRequested();
			Requests++;
			List<FileExplorerItem> items = request.SearchText switch
			{
				null => [.. _items],
				"" => [_items[0]],
				var parent => [.. _items.Where(i => i.Path != "/" && i.ParentPath == parent)]
			};
			return Task.FromResult(new DataResponse<FileExplorerItem>(items, items.Count));
		}
	}
}
