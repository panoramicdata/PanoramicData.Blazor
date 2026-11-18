using AwesomeAssertions;
using Bunit;
using PanoramicData.Blazor.Models;

namespace PanoramicData.Blazor.Test.Components;

/// <summary>
/// The toolbar's button groups: Execute with the host's buttons and an optional Cancel, then New and Examples with the
/// host's buttons.
/// </summary>
public partial class PDStudioTests
{
	private static List<string> GroupKeys(IRenderedComponent<PDStudio> studio, string group)
		=> [.. studio.Find($".pdtoolbargroup.{group}").Children.Select(item => item.QuerySelector("button")?.TextContent.Trim() ?? item.TextContent.Trim())];

	/// <summary>
	/// Verifies that by default Execute is alone in its group and still turns into Cancel, and New is in the next group.
	/// </summary>
	[Fact]
	public void Groups_ByDefault_ExecuteIsAlone_AndNewFollowsInItsOwnGroup()
	{
		var studio = RenderStudio();

		GroupKeys(studio, "pd-studio-execute-group").Should().Equal("Execute");
		GroupKeys(studio, "pd-studio-menu-group").Should().Equal("New");
		studio.FindComponents<PDToolbarButton>().Select(b => b.Instance.Key).Should().NotContain("cancel");
		PlayButton(studio).ClassList.Should().Contain("pd-studio-execute");
	}

	/// <summary>
	/// Verifies that the host's buttons join each group, after the studio's own, with Cancel last in the first group.
	/// </summary>
	[Fact]
	public void Groups_HoldTheHostsButtons_InOrder()
	{
		var studio = RenderStudio(new PDStudioOptions { ShowCancelButton = true }, p => p
			.Add(x => x.ExecuteGroupContent, b => b.AddMarkupContent(0, "<div class=\"pdtoolbaritem\"><button>Explain</button></div>"))
			.Add(x => x.MenuGroupContent, b => b.AddMarkupContent(0, "<div class=\"pdtoolbaritem\"><button>Restore</button></div>")));

		GroupKeys(studio, "pd-studio-execute-group").Should().Equal("Execute", "Explain", "Cancel");
		GroupKeys(studio, "pd-studio-menu-group").Should().Equal("New", "Restore");
	}

	/// <summary>
	/// Verifies that a host's menu group content is shown even when the studio offers no New.
	/// </summary>
	[Fact]
	public void MenuGroupContent_IsShown_WithoutNew()
	{
		var studio = RenderStudio(new PDStudioOptions { ShowNewButton = false }, p => p
			.Add(x => x.MenuGroupContent, b => b.AddMarkupContent(0, "<div class=\"pdtoolbaritem\"><button>Restore</button></div>")));

		GroupKeys(studio, "pd-studio-menu-group").Should().Equal("Restore");
	}

	/// <summary>
	/// Verifies that with a Cancel button of its own, Cancel is enabled only while code runs and stops it, Execute is
	/// disabled meanwhile and never turns into Cancel, so the toolbar keeps its shape.
	/// </summary>
	[Fact]
	public async Task ShowCancelButton_CancelIsEnabledOnlyWhileCodeRuns_AndStopsIt()
	{
		var studio = RenderStudio(new PDStudioOptions { ShowCancelButton = true });
		_service.Block = true;
		await TypeAsync(studio, "sleep(10)");

		ToolbarButton(studio, "cancel").HasAttribute("disabled").Should().BeTrue();
		PlayButton(studio).ClassList.Should().NotContain("pd-studio-execute");

		var running = PlayButton(studio).ClickAsync(new());
		studio.WaitForAssertion(() => ToolbarButton(studio, "cancel").HasAttribute("disabled").Should().BeFalse());
		PlayButton(studio).TextContent.Should().Contain("Execute");
		PlayButton(studio).HasAttribute("disabled").Should().BeTrue();

		await ToolbarButton(studio, "cancel").ClickAsync(new());
		await running;

		studio.WaitForAssertion(() => ToolbarButton(studio, "cancel").HasAttribute("disabled").Should().BeTrue());
		PlayButton(studio).HasAttribute("disabled").Should().BeFalse();
		Status(studio).Should().Be("Cancelled");
		LogMessages(studio).Should().Contain("Code execution was cancelled");
	}
}
