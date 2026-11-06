using AwesomeAssertions;
using Bunit;
using PanoramicData.Blazor.Models;

namespace PanoramicData.Blazor.Test.Components;

/// <summary>
/// The tab showing, which a host can read, and is told of as it changes.
/// </summary>
public partial class PDStudioTests
{
	private IRenderedComponent<PDStudio> RenderReportingTabs(List<string> reported, PDStudioOptions? options = null)
		=> RenderStudio(options, p => p
			.Add(x => x.OutputTabs, _planTab)
			.Add(x => x.ActiveTabChanged, key => reported.Add(key)));

	/// <summary>
	/// Verifies that the tab showing is reported once it first shows, and can be read.
	/// </summary>
	[Fact]
	public void ActiveTabChanged_ReportsTheFirstTabShown()
	{
		var reported = new List<string>();

		var studio = RenderReportingTabs(reported);

		reported.Should().Equal("results");
		studio.Instance.ActiveTabKey.Should().Be("results");
	}

	/// <summary>
	/// Verifies that a click and <see cref="PDStudio.SelectTab(string)"/> each report the tab they show, and that
	/// choosing the tab already showing reports nothing.
	/// </summary>
	[Fact]
	public async Task ActiveTabChanged_ReportsEachChange_ByClickOrSelectTab_ButNotTheSameTabAgain()
	{
		var reported = new List<string>();
		var studio = RenderReportingTabs(reported);

		await studio.FindAll(".pd-studio-tab")[2].ClickAsync(new());
		await studio.InvokeAsync(() => studio.Instance.SelectTab("messages"));
		await studio.InvokeAsync(() => studio.Instance.SelectTab("messages"));

		reported.Should().Equal("results", "plan", "messages");
		studio.Instance.ActiveTabKey.Should().Be("messages");
	}

	/// <summary>
	/// Verifies that when the tab showing goes away, the tab shown in its place is reported.
	/// </summary>
	[Fact]
	public async Task ActiveTabChanged_ReportsTheTabShownInPlaceOfOneThatHasGone()
	{
		var reported = new List<string>();
		var studio = RenderReportingTabs(reported);
		await studio.InvokeAsync(() => studio.Instance.SelectTab("plan"));

		studio.Render(p => p.Add(x => x.OutputTabs, Array.Empty<PDStudioOutputTab>()));

		reported.Should().Equal("results", "plan", "results");
		studio.Instance.ActiveTabKey.Should().Be("results");
	}

	/// <summary>
	/// Verifies that each tab says whether it is selected, as "true" or "false", and follows the selection.
	/// </summary>
	[Fact]
	public async Task Tabs_SayWhetherTheyAreSelected_AsTrueOrFalse()
	{
		var studio = RenderStudio(more: p => p.Add(x => x.OutputTabs, _planTab));

		studio.FindAll("[role='tab']").Select(tab => tab.GetAttribute("aria-selected")).Should().Equal("true", "false", "false");

		await studio.InvokeAsync(() => studio.Instance.SelectTab("plan"));

		studio.FindAll("[role='tab']").Select(tab => tab.GetAttribute("aria-selected")).Should().Equal("false", "false", "true");
	}

	/// <summary>
	/// Verifies that the Messages tab comes before the host's own tabs, or after them with MessagesTabLast, and still
	/// opens either way.
	/// </summary>
	[Fact]
	public async Task MessagesTab_ComesBeforeTheHostsTabs_OrLastWhenAskedTo()
	{
		Tabs(RenderStudio(more: p => p.Add(x => x.OutputTabs, _planTab))).Should().Equal("Results", "Messages", "Plan");

		var last = RenderStudio(new PDStudioOptions { MessagesTabLast = true }, p => p.Add(x => x.OutputTabs, _planTab));

		Tabs(last).Should().Equal("Results", "Plan", "Messages");
		await last.FindAll("[role='tab']")[2].ClickAsync(new());
		last.Instance.ActiveTabKey.Should().Be("messages");
	}

	/// <summary>
	/// Verifies that with the Results tab turned off, the first tab reported is Messages.
	/// </summary>
	[Fact]
	public void ActiveTabChanged_WithoutTheResultsTab_ReportsMessagesFirst()
	{
		var reported = new List<string>();

		RenderReportingTabs(reported, new PDStudioOptions { ShowResultsTab = false });

		reported.Should().Equal("messages");
	}
}
