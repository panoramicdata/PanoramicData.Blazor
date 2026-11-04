using AwesomeAssertions;
using Bunit;
using PanoramicData.Blazor.Models;

namespace PanoramicData.Blazor.Test.Components;

/// <summary>
/// Badges on a host's tabs: a count or other short text, or a dot, in a colour for what it tells.
/// </summary>
public partial class PDStudioTests
{
	private IRenderedComponent<PDStudio> RenderWithPlanBadge(PDStudioTabBadge? badge)
		=> RenderStudio(more: p => p.Add(x => x.OutputTabs, [_planTab[0] with { Badge = badge }]));

	private static AngleSharp.Dom.IElement PlanTab(IRenderedComponent<PDStudio> studio) => studio.FindAll(".pd-studio-tab")[2];

	/// <summary>Verifies that a host's tab has no badge unless it is given one.</summary>
	[Fact]
	public void TabBadge_WhenNotGiven_IsNotShown()
		=> PlanTab(RenderWithPlanBadge(null)).QuerySelector(".pd-studio-tab-badge").Should().BeNull();

	/// <summary>Verifies that a badge with text shows it after the title, in its kind's colour, with its tooltip.</summary>
	[Fact]
	public void TabBadge_WithText_ShowsIt_InItsKindsColour()
	{
		var studio = RenderWithPlanBadge(new PDStudioTabBadge("3") { Kind = PDStudioTabBadgeKind.Warning, ToolTip = "3 warnings" });

		var badge = PlanTab(studio).QuerySelector(".pd-studio-tab-badge")!;
		badge.TextContent.Should().Be("3");
		badge.ClassList.Should().Contain("pd-studio-tab-badge-warning").And.NotContain("pd-studio-tab-badge-dot");
		badge.GetAttribute("title").Should().Be("3 warnings");
		PlanTab(studio).FirstChild!.TextContent.Trim().Should().Be("Plan");
	}

	/// <summary>Verifies that a badge without text is a dot that says what it means, with the host's own class.</summary>
	[Fact]
	public void TabBadge_WithoutText_IsADot_ThatSaysWhatItMeans()
	{
		var studio = RenderWithPlanBadge(new PDStudioTabBadge { Kind = PDStudioTabBadgeKind.Info, ToolTip = "Running", CssClass = "my-pulse" });

		var badge = PlanTab(studio).QuerySelector(".pd-studio-tab-badge")!;
		badge.TextContent.Should().BeEmpty();
		badge.ClassList.Should().Contain(["pd-studio-tab-badge-dot", "pd-studio-tab-badge-info", "my-pulse"]);
		badge.GetAttribute("role").Should().Be("img");
		badge.GetAttribute("aria-label").Should().Be("Running");
	}

	/// <summary>Verifies that a neutral badge has no kind class, so it keeps the badge's own colours.</summary>
	[Fact]
	public void TabBadge_Neutral_HasNoKindClass()
	{
		var badge = PlanTab(RenderWithPlanBadge(new PDStudioTabBadge("12"))).QuerySelector(".pd-studio-tab-badge")!;

		badge.ClassList.Should().Equal("pd-studio-tab-badge");
	}

	/// <summary>Verifies that a badge follows the tab the host gives the studio next.</summary>
	[Fact]
	public void TabBadge_FollowsTheTabTheHostGivesNext()
	{
		var studio = RenderWithPlanBadge(new PDStudioTabBadge { Kind = PDStudioTabBadgeKind.Info });

		studio.Render(p => p.Add(x => x.OutputTabs, [_planTab[0] with { Badge = new PDStudioTabBadge { Kind = PDStudioTabBadgeKind.Error } }]));
		PlanTab(studio).QuerySelector(".pd-studio-tab-badge")!.ClassList.Should().Contain("pd-studio-tab-badge-error");

		studio.Render(p => p.Add(x => x.OutputTabs, _planTab));
		PlanTab(studio).QuerySelector(".pd-studio-tab-badge").Should().BeNull();
	}
}
