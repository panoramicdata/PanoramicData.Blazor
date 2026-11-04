using AwesomeAssertions;
using Bunit;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;
using PanoramicData.Blazor.Models;

namespace PanoramicData.Blazor.Test.Components;

/// <summary>
/// A host putting its own content in place of the code editor, and leaving out the Results tab.
/// </summary>
public partial class PDStudioTests
{
	/// <summary>
	/// Verifies that without EditorContent the code editor shows as it always has, with nothing in its place.
	/// </summary>
	[Fact]
	public void EditorContent_WhenNotGiven_LeavesTheCodeEditorShowing()
	{
		var studio = RenderStudio();

		studio.Find(".pd-studio-editor-container").HasAttribute("hidden").Should().BeFalse();
		studio.FindAll(".pd-studio-editor-content").Should().BeEmpty();
	}

	/// <summary>
	/// Verifies that EditorContent takes the code editor's place, and that the code editor, hidden meanwhile, still
	/// holds its code when the content is cleared.
	/// </summary>
	[Fact]
	public async Task EditorContent_TakesTheCodeEditorsPlace_AndTheCodeEditorKeepsItsCode()
	{
		var studio = RenderStudio();
		await TypeAsync(studio, "1 + 1");

		studio.Render(p => p.Add(x => x.EditorContent, b => b.AddMarkupContent(0, "<div class=\"rich\">Rich text</div>")));

		studio.Find(".pd-studio-editor-content .rich").TextContent.Should().Be("Rich text");
		studio.Find(".pd-studio-editor-container").HasAttribute("hidden").Should().BeTrue();
		studio.FindComponents<PDMonacoEditor>().Should().ContainSingle("the code editor stays in the page while hidden");
		studio.Instance.Code.Should().Be("1 + 1");

		studio.Render(p => p.Add(x => x.EditorContent, (RenderFragment?)null));

		studio.FindAll(".pd-studio-editor-content").Should().BeEmpty();
		studio.Find(".pd-studio-editor-container").HasAttribute("hidden").Should().BeFalse();
		studio.FindComponent<PDMonacoEditor>().Instance.Value.Should().Be("1 + 1");
	}

	/// <summary>
	/// Verifies that the Results tab is shown first unless the host turns it off.
	/// </summary>
	[Fact]
	public void ResultsTab_IsShownFirst_ByDefault()
	{
		var studio = RenderStudio(more: p => p.Add(x => x.OutputTabs, _planTab));

		Tabs(studio).Should().Equal("Results", "Messages", "Plan");
		studio.Find(".pd-studio-tab.active").TextContent.Trim().Should().Be("Results");
		studio.FindComponents<PDStudioResults>().Should().ContainSingle();
	}

	/// <summary>
	/// Verifies that with the Results tab turned off, Messages is selected instead and Results cannot be, and that
	/// code still runs.
	/// </summary>
	[Fact]
	public async Task ResultsTab_TurnedOff_IsLeftOut_SoMessagesIsSelected_AndCodeStillRuns()
	{
		var executed = new List<string>();
		var studio = RenderStudio(new PDStudioOptions { ShowResultsTab = false }, p => p
			.Add(x => x.OutputTabs, _planTab)
			.Add(x => x.OnCodeExecuted, code => executed.Add(code)));

		Tabs(studio).Should().Equal("Messages", "Plan");
		studio.Find(".pd-studio-tab.active").TextContent.Should().Contain("Messages");
		studio.FindComponents<PDStudioResults>().Should().BeEmpty();

		await studio.InvokeAsync(() => studio.Instance.SelectTab("results"));
		studio.Find(".pd-studio-tab.active").TextContent.Should().Contain("Messages");

		await TypeAsync(studio, "1 + 1");
		await PlayButton(studio).ClickAsync(new MouseEventArgs());
		studio.WaitForAssertion(() => executed.Should().Equal("1 + 1"));
	}

	/// <summary>
	/// Verifies that with neither Results nor Messages shown, the host's first tab is selected.
	/// </summary>
	[Fact]
	public void ResultsAndMessagesTurnedOff_SelectTheHostsFirstTab()
	{
		var studio = RenderStudio(new PDStudioOptions { ShowResultsTab = false, IsLoggingVisible = false }, p => p.Add(x => x.OutputTabs, _planTab));

		Tabs(studio).Should().Equal("Plan");
		studio.Find(".pd-studio-tab.active").TextContent.Trim().Should().Be("Plan");
		studio.Find(".plan").ParentElement!.HasAttribute("hidden").Should().BeFalse();
	}
}
