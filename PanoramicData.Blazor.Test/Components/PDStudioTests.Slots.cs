using AwesomeAssertions;
using Bunit;
using PanoramicData.Blazor.Models;

namespace PanoramicData.Blazor.Test.Components;

/// <summary>
/// Places a host can fill: a left panel, the toolbar's right-hand end, a footer, and extra output tabs.
/// </summary>
public partial class PDStudioTests
{
	private static readonly PDStudioOutputTab[] _planTab =
		[new("plan", "Plan", b => b.AddMarkupContent(0, "<p class=\"plan\">Seq Scan</p>"))];

	/// <summary>
	/// Verifies that each slot shows the host's content where it belongs, and is left out when not given.
	/// </summary>
	[Fact]
	public void Slots_ShowTheHostsContent()
	{
		var bare = RenderStudio();
		bare.FindAll(".pd-studio-left-panel, .pd-studio-toolbar-end, .pd-studio-footer").Should().BeEmpty();

		var studio = RenderStudio(more: p => p
			.Add(x => x.LeftPanelContent, b => b.AddMarkupContent(0, "<span class=\"schema\">Tables</span>"))
			.Add(x => x.ToolbarEndContent, b => b.AddMarkupContent(0, "<span class=\"link\">Copy link</span>"))
			.Add(x => x.FooterContent, b => b.AddMarkupContent(0, "<span class=\"cursor\">Ln 2, Col 1</span>")));

		studio.Find(".pd-studio-left-panel .schema").TextContent.Should().Be("Tables");
		studio.Find(".pd-studio-toolbar .pd-studio-toolbar-end .link").TextContent.Should().Be("Copy link");
		studio.Find(".pd-studio-footer .cursor").TextContent.Should().Be("Ln 2, Col 1");
	}

	/// <summary>
	/// Verifies that a host's tab follows Results and Messages and shows its content when chosen, by click or by
	/// <see cref="PDStudio.SelectTab(string)"/>.
	/// </summary>
	[Fact]
	public async Task OutputTabs_AreAddedAndSelectable()
	{
		var studio = RenderStudio(more: p => p.Add(x => x.OutputTabs, _planTab));

		Tabs(studio).Should().Equal("Results", "Messages", "Plan");
		studio.Find(".plan").ParentElement!.HasAttribute("hidden").Should().BeTrue();

		studio.FindAll(".pd-studio-tab")[2].Click();
		studio.Find(".plan").ParentElement!.HasAttribute("hidden").Should().BeFalse();

		await studio.InvokeAsync(() => studio.Instance.SelectTab("messages"));
		studio.Find(".pd-studio-tab.active").TextContent.Should().Contain("Messages");
	}

	/// <summary>
	/// Verifies that selecting a tab the host then removes, or one that never existed, falls back to Results.
	/// </summary>
	[Fact]
	public async Task OutputTabs_RemovedOrUnknown_FallBackToResults()
	{
		var studio = RenderStudio(more: p => p.Add(x => x.OutputTabs, _planTab));
		await studio.InvokeAsync(() => studio.Instance.SelectTab("plan"));

		studio.Render(p => p.Add(x => x.OutputTabs, Array.Empty<PDStudioOutputTab>()));
		studio.Find(".pd-studio-tab.active").TextContent.Trim().Should().Be("Results");

		await studio.InvokeAsync(() => studio.Instance.SelectTab("nonsense"));
		studio.Find(".pd-studio-tab.active").TextContent.Trim().Should().Be("Results");
	}
}
