using AwesomeAssertions;
using Bunit;
using PanoramicData.Blazor.Models;

namespace PanoramicData.Blazor.Test.Components;

/// <summary>
/// Toolbar tests for <see cref="PDFileExplorer"/>.
/// </summary>
public partial class PDFileExplorerTests
{
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
}
