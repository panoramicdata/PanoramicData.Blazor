using AwesomeAssertions;
using Bunit;
using Microsoft.AspNetCore.Components.Web;
using PanoramicData.Blazor.Models;

namespace PanoramicData.Blazor.Test.Components;

/// <summary>
/// Tab switching, adding and removing tests for <see cref="PDDashboard"/>.
/// </summary>
public partial class PDDashboardTests
{
	/// <summary>Tabs are listed when there are several, and clicking one shows its tiles with its own grid settings.</summary>
	[Fact]
	public async Task ClickingATab_SwitchesToIt()
	{
		var changes = new List<int>();
		var dashboard = RenderDashboard(TwoTabs(), p => p.Add(x => x.ActiveTabChanged, (int i) => changes.Add(i)));

		var tabButtons = dashboard.FindAll(".pd-dashboard-tabs .nav-link").Where(b => b.TextContent.Trim() is "One" or "Two").ToList();
		tabButtons.Should().HaveCount(2);
		tabButtons[0].ClassList.Should().Contain("active");

		await tabButtons[1].ClickAsync(new MouseEventArgs());

		dashboard.Instance.ActiveTabIndex.Should().Be(1);
		changes.Should().Equal(1);
		dashboard.FindAll(".tile-text").Select(t => t.TextContent).Should().Equal("C");
		dashboard.Find(".pd-dashboard-grid").ClassList.Should().Contain("tab-two");
		dashboard.Find(".pd-dashboard-grid").GetAttribute("style").Should().Contain("repeat(4, 1fr)").And.Contain("50px");
		Navigation.Uri.Should().EndWith("?tab=1");
	}

	/// <summary>A tab in the URL is opened on first render; an out of range one falls back to the start tab.</summary>
	[Theory]
	[InlineData("?tab=1", 0, 1)]
	[InlineData("?tab=9", 1, 1)]
	[InlineData("?tab=x", 0, 0)]
	public void UrlTab_IsOpenedWhenValid(string query, int startTab, int expected)
	{
		Navigation.NavigateTo(query);

		var dashboard = RenderDashboard(TwoTabs(), p => p.Add(x => x.StartTab, startTab));

		dashboard.Instance.ActiveTabIndex.Should().Be(expected);
	}

	/// <summary>GoToTabAsync ignores out of range and current indexes.</summary>
	[Fact]
	public async Task GoToTab_IgnoresInvalidIndexes()
	{
		var changes = new List<int>();
		var dashboard = RenderDashboard(TwoTabs(), p => p.Add(x => x.ActiveTabChanged, (int i) => changes.Add(i)));

		await dashboard.InvokeAsync(() => dashboard.Instance.GoToTabAsync(-1));
		await dashboard.InvokeAsync(() => dashboard.Instance.GoToTabAsync(5));
		await dashboard.InvokeAsync(() => dashboard.Instance.GoToTabAsync(0));
		changes.Should().BeEmpty();

		await dashboard.InvokeAsync(() => dashboard.Instance.GoToTabAsync(1));
		changes.Should().Equal(1);
	}

	/// <summary>RemoveTabAsync removes a tab, raises OnTabRemove and keeps the active index in range.</summary>
	[Fact]
	public async Task RemoveTab_RemovesAndClampsTheActiveIndex()
	{
		var tabs = TwoTabs();
		PDDashboardTab? removed = null;
		var dashboard = RenderDashboard(tabs, p => p.Add(x => x.OnTabRemove, (PDDashboardTab t) => removed = t));
		await dashboard.InvokeAsync(() => dashboard.Instance.GoToTabAsync(1));

		await dashboard.InvokeAsync(() => dashboard.Instance.RemoveTabAsync(1));
		await dashboard.InvokeAsync(() => dashboard.Instance.RemoveTabAsync(7));

		removed!.Name.Should().Be("Two");
		tabs.Should().ContainSingle();
		dashboard.Instance.ActiveTabIndex.Should().Be(0);
		_events.Should().Equal("settings");
	}

	/// <summary>Adding a tab appends it, raises OnTabAdd and switches to it.</summary>
	[Fact]
	public async Task AddTab_AppendsAndSelectsIt()
	{
		var tabs = TwoTabs();
		PDDashboardTab? added = null;
		var dashboard = RenderDashboard(tabs, p => p.Add(x => x.IsEditable, true).Add(x => x.OnTabAdd, (PDDashboardTab t) => added = t));

		await dashboard.Find(".pd-dashboard-tab-add").ClickAsync(new MouseEventArgs());

		tabs.Should().HaveCount(3);
		added!.Name.Should().Be("Tab 3");
		dashboard.Instance.ActiveTabIndex.Should().Be(2);
		_events.Should().Contain("settings");
	}
}
