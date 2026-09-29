using AwesomeAssertions;
using Bunit;
using PanoramicData.Blazor.Models;
using PanoramicData.Blazor.PreviewProviders;

namespace PanoramicData.Blazor.Test.Components;

/// <summary>
/// Preview panel behaviour of <see cref="PDFileExplorer"/>: whether the panel and its toggle are present, what it
/// previews, and how toggling resizes the splitter.
/// </summary>
public partial class PDFileExplorerTests
{
	/// <summary>With the preview off, no preview panel or toggle is rendered.</summary>
	[Fact]
	public void PreviewOff_RendersNoPanelOrToggle()
	{
		var cut = RenderExplorer();

		cut.FindAll(".pdfilepreview").Should().BeEmpty();
		cut.Instance.ToolbarItems.Select(x => x.Key).Should().NotContain("preview");
	}

	/// <summary>With the preview on, the selected folder is previewed by name, and no toggle is offered.</summary>
	[Fact]
	public async Task PreviewOn_PreviewsTheSelectedFolder()
	{
		var cut = RenderExplorer(p => p.Add(x => x.PreviewPanel, FilePreviewModes.On));

		await SelectRowsAsync(cut, "/Docs");

		cut.WaitForAssertion(() => cut.Find(".pdfilepreview").TextContent.Should().Contain("Docs").And.Contain("Folder"));
		cut.Instance.ToolbarItems.Select(x => x.Key).Should().NotContain("preview");
	}

	/// <summary>Selecting several rows clears the preview.</summary>
	[Fact]
	public async Task PreviewOn_SeveralRows_ShowsNoPreview()
	{
		var cut = RenderExplorer(p => p.Add(x => x.PreviewPanel, FilePreviewModes.On));

		await SelectRowsAsync(cut, "/Docs", "/Media");

		cut.WaitForAssertion(() => cut.Find(".pdfilepreview").TextContent.Should().Contain("No Preview"));
	}

	/// <summary>An optional preview that starts on offers a toggle which collapses and then restores the panel.</summary>
	[Fact]
	public async Task OptionalOn_ToggleCollapsesAndRestores()
	{
		var cut = RenderExplorer(p => p.Add(x => x.PreviewPanel, FilePreviewModes.OptionalOn));
		cut.Instance.PreviewPanelVisible.Should().BeTrue();
		((ToolbarButton)ToolbarButton(cut, "preview")).IconCssClass.Should().Be("fas fa-fw fa-eye-slash");

		await ClickToolbarAsync(cut, "preview");
		cut.Instance.PreviewPanelVisible.Should().BeFalse();
		((ToolbarButton)ToolbarButton(cut, "preview")).IconCssClass.Should().Be("fas fa-fw fa-eye");

		await ClickToolbarAsync(cut, "preview");
		cut.Instance.PreviewPanelVisible.Should().BeTrue();

		var sizes = _splitter.Invocations["setSizes"].Select(x => (double[])x.Arguments[1]!).ToList();
		sizes.Should().HaveCount(2);
		sizes[0].Should().Equal(20, 80, 0);
		sizes[1].Should().Equal(20, 60, 20);
	}

	/// <summary>An optional preview that starts off is hidden initially.</summary>
	[Fact]
	public void OptionalOff_StartsHidden()
	{
		var cut = RenderExplorer(p => p.Add(x => x.PreviewPanel, FilePreviewModes.OptionalOff));

		cut.Instance.PreviewPanelVisible.Should().BeFalse();
		ToolbarButton(cut, "preview").Should().NotBeNull();
	}

	/// <summary>Without a PreviewProvider parameter the explorer previews through a provider bound to itself.</summary>
	[Fact]
	public void PreviewProvider_Default_IsBoundToTheExplorer()
	{
		var cut = RenderExplorer();

		cut.Instance.PreviewProvider.Should().BeOfType<FileExplorerPreviewProvider>()
			.Which.FileExplorer.Should().BeSameAs(cut.Instance);
	}

	/// <summary>A supplied PreviewProvider is used, not replaced (#174).</summary>
	[Fact]
	public void PreviewProvider_Supplied_IsUsed()
	{
		var supplied = new DefaultPreviewProvider { DateTimeFormat = "yyyy" };

		var cut = RenderExplorer(p => p.Add(x => x.PreviewProvider, supplied).Add(x => x.PreviewPanel, FilePreviewModes.On));

		cut.Instance.PreviewProvider.Should().BeSameAs(supplied);
		cut.FindComponent<PDFilePreview>().Instance.PreviewProvider.Should().BeSameAs(supplied);
	}

	/// <summary>A supplied FileExplorerPreviewProvider that is not yet bound to an explorer is bound to this one.</summary>
	[Fact]
	public void PreviewProvider_SuppliedUnboundFileExplorerProvider_IsBound()
	{
		var supplied = new FileExplorerPreviewProvider();

		var cut = RenderExplorer(p => p.Add(x => x.PreviewProvider, supplied));

		cut.Instance.PreviewProvider.Should().BeSameAs(supplied);
		supplied.FileExplorer.Should().BeSameAs(cut.Instance);
	}

	/// <summary>The preview key does nothing when the preview is not optional.</summary>
	[Fact]
	public async Task PreviewKey_WhenNotOptional_DoesNothing()
	{
		var cut = RenderExplorer(p => p.Add(x => x.PreviewPanel, FilePreviewModes.On));

		await ClickToolbarAsync(cut, "preview");

		cut.Instance.PreviewPanelVisible.Should().BeTrue();
		_splitter.Invocations["setSizes"].Should().BeEmpty();
	}
}
