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
