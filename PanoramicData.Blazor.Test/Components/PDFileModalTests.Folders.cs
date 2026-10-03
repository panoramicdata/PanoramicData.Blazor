using AwesomeAssertions;
using Bunit;
using Microsoft.AspNetCore.Components.Web;
using PanoramicData.Blazor.Models;

namespace PanoramicData.Blazor.Test;

/// <summary>
/// Folder mode tests for <see cref="PDFileModal"/>.
/// </summary>
public partial class PDFileModalTests
{
	/// <summary>Verifies that in folder mode the folder navigated to can be chosen, and its files are not listed.</summary>
	[Fact]
	public async Task Folder_mode_chooses_the_current_folder()
	{
		var show = SetupPendingShow();
		var component = RenderModal();

		await ShowFolderOpenAsync(component, show, "/Docs");
		component.WaitForAssertion(() => OkButton(component).HasAttribute("disabled").Should().BeFalse(), Patience);
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
		component.WaitForAssertion(() => Row(component, "/Docs"), Patience);

		await Row(component, "/Docs").MouseUpAsync(new MouseEventArgs());
		await OkButton(component).ClickAsync(new());

		_results.Should().Equal("/Docs");
	}

	/// <summary>
	/// Verifies that in folder mode, with nothing selected, the current folder can be chosen unless CanSelectFolder
	/// refuses it, and that a selection of several items cannot be chosen.
	/// </summary>
	[Theory]
	[InlineData(false, 0, true)]
	[InlineData(true, 0, false)]
	[InlineData(false, 2, false)]
	public async Task Folder_mode_enables_OK_for_the_current_folder_only_without_a_selection(bool refuseFolders, int selected, bool expectEnabled)
	{
		var show = SetupPendingShow();
		var component = refuseFolders ? RenderModal(p => p.Add(x => x.CanSelectFolder, _ => false)) : RenderModal();
		await ShowFolderOpenAsync(component, show);
		component.WaitForAssertion(() => Row(component, "/Docs"), Patience);
		var explorer = component.FindComponent<PDFileExplorer>();
		var selection = Enumerable.Range(1, selected)
			.Select(i => new FileExplorerItem { Path = $"/Folder{i}", Name = $"Folder{i}", EntryType = FileExplorerItemType.Directory })
			.ToArray();

		await explorer.InvokeAsync(() => explorer.Instance.SelectionChanged.InvokeAsync(selection));

		component.WaitForAssertion(() => OkButton(component).HasAttribute("disabled").Should().Be(!expectEnabled), Patience);
	}

	/// <summary>Verifies that CanSelectFolder can refuse a folder, keeping OK disabled.</summary>
	[Fact]
	public async Task CanSelectFolder_can_refuse_a_folder()
	{
		var show = SetupPendingShow();
		var component = RenderModal(p => p.Add(x => x.CanSelectFolder, item => item.Path != "/Docs" && item.Path != "/"));
		await ShowFolderOpenAsync(component, show);
		component.WaitForAssertion(() => Row(component, "/Docs"), Patience);

		await Row(component, "/Docs").MouseUpAsync(new MouseEventArgs());

		OkButton(component).HasAttribute("disabled").Should().BeTrue();
	}

	/// <summary>
	/// Verifies that opening in folder mode while the explorer is already at the root stops listing files: the
	/// navigation to the root does nothing there, so the table has to be reloaded instead (#191).
	/// </summary>
	[Fact]
	public async Task Folder_mode_at_the_root_hides_files_already_listed()
	{
		var show = SetupPendingShow();
		var component = RenderModal();
		var opening = component.InvokeAsync(() => component.Instance.ShowOpenAsync());
		show.SetVoidResult();
		await opening;
		component.WaitForAssertion(() => Row(component, "/readme.txt"), Patience);

		await ShowFolderOpenAsync(component, show);

		component.WaitForAssertion(() => component.FindAll("tr[id='/readme.txt']").Should().BeEmpty(), Patience);
		component.FindAll("tr[id='/Docs']").Should().ContainSingle();
	}

	/// <summary>
	/// Makes the dialog's show call wait until released, as it does in a browser, so the dialog renders its new
	/// mode before the explorer loads the folder.
	/// </summary>
	private JSRuntimeInvocationHandler SetupPendingShow()
		=> JSInterop.SetupModule("./_content/PanoramicData.Blazor/PDModal.razor.js").SetupModule("initialize", _ => true).SetupVoid("show");

	private static async Task ShowFolderOpenAsync(IRenderedComponent<PDFileModal> component, JSRuntimeInvocationHandler show, string initialFolder = "")
	{
		var opening = initialFolder.Length == 0
			? component.InvokeAsync(() => component.Instance.ShowOpenAsync(true))
			: component.InvokeAsync(() => component.Instance.ShowOpenAsync(true, "", initialFolder));
		show.SetVoidResult();
		await opening;
	}
}
