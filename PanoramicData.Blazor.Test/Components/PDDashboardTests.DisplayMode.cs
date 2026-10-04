using AwesomeAssertions;
using Bunit;
using Microsoft.AspNetCore.Components.Web;
using PanoramicData.Blazor.Enums;

namespace PanoramicData.Blazor.Test.Components;

/// <summary>
/// Display mode header and tab rotation tests for <see cref="PDDashboard"/>.
/// </summary>
public partial class PDDashboardTests
{
	/// <summary>The name row is shown outside display mode when ShowName is set.</summary>
	[Fact]
	public void ShowName_ShowsTheNameRow()
	{
		var dashboard = RenderDashboard(TwoTabs(), p => p.Add(x => x.Name, "Ops").Add(x => x.ShowName, true));

		dashboard.Find(".pd-dashboard-name-text").TextContent.Should().Be("Ops");
	}

	/// <summary>Display mode hides the tab bar and shows the configured header content.</summary>
	[Theory]
	[InlineData(DisplayModeHeaderContent.DashboardName, "Ops", null)]
	[InlineData(DisplayModeHeaderContent.TabName, null, "One")]
	[InlineData(DisplayModeHeaderContent.Both, "Ops", "One")]
	public void DisplayMode_ShowsTheConfiguredHeader(DisplayModeHeaderContent header, string? name, string? tabName)
	{
		var dashboard = RenderDashboard(TwoTabs(), p => p
			.Add(x => x.DisplayMode, true)
			.Add(x => x.DisplayModeHeader, header)
			.Add(x => x.Name, "Ops"));

		dashboard.FindAll(".pd-dashboard-tabs").Should().BeEmpty();
		dashboard.FindAll(".pd-dashboard-display-name").Select(e => e.TextContent).Should().Equal(name is null ? [] : [name]);
		dashboard.FindAll(".pd-dashboard-display-tabname").Select(e => e.TextContent).Should().Equal(tabName is null ? [] : [tabName]);
		dashboard.FindAll(".pd-dashboard-display-separator").Count.Should().Be(header == DisplayModeHeaderContent.Both ? 1 : 0);
	}

	/// <summary>In display mode with rotation, the header's arrows move between tabs, wrapping round.</summary>
	[Fact]
	public async Task DisplayModeNavigation_MovesBetweenTabs()
	{
		var dashboard = RenderDashboard(TwoTabs(), p => p
			.Add(x => x.DisplayMode, true)
			.Add(x => x.DisplayModeHeader, DisplayModeHeaderContent.TabName)
			.Add(x => x.IsRotationEnabled, true)
			.Add(x => x.RotationIntervalSeconds, 3600));

		await dashboard.Find("button[title='Next tab']").ClickAsync(new MouseEventArgs());
		dashboard.Instance.ActiveTabIndex.Should().Be(1);

		await dashboard.Find("button[title='Next tab']").ClickAsync(new MouseEventArgs());
		dashboard.Instance.ActiveTabIndex.Should().Be(0);

		await dashboard.Find("button[title='Previous tab']").ClickAsync(new MouseEventArgs());
		dashboard.Instance.ActiveTabIndex.Should().Be(1);
	}

	/// <summary>The pause button toggles between pause and resume.</summary>
	[Fact]
	public async Task PauseButton_TogglesRotation()
	{
		var dashboard = RenderDashboard(TwoTabs(), p => p
			.Add(x => x.DisplayMode, true)
			.Add(x => x.DisplayModeHeader, DisplayModeHeaderContent.TabName)
			.Add(x => x.IsRotationEnabled, true));

		await dashboard.Find("button[title='Pause rotation']").ClickAsync(new MouseEventArgs());
		dashboard.Find("button[title='Resume rotation'] span").ClassList.Should().Contain("fa-play");

		await dashboard.Find("button[title='Resume rotation']").ClickAsync(new MouseEventArgs());
		dashboard.FindAll("button[title='Pause rotation']").Should().ContainSingle();
	}

	/// <summary>With rotation enabled the active tab advances on its own and wraps round.</summary>
	[Fact]
	public void Rotation_AdvancesTheActiveTab()
	{
		var tabs = TwoTabs();
		tabs[1].RotationIntervalSecondsOverride = 1;

		var dashboard = RenderDashboard(tabs, p => p.Add(x => x.IsRotationEnabled, true).Add(x => x.RotationIntervalSeconds, 1));

		dashboard.WaitForAssertion(() => dashboard.Instance.ActiveTabIndex.Should().Be(1), TimeSpan.FromSeconds(5));
		dashboard.WaitForAssertion(() => dashboard.Instance.ActiveTabIndex.Should().Be(0), TimeSpan.FromSeconds(5));
	}

	/// <summary>Changing the rotation parameters after first render restarts the timer with the new interval.</summary>
	[Fact]
	public void ChangingRotation_RestartsTheTimer()
	{
		var dashboard = RenderDashboard(TwoTabs(), p => p.Add(x => x.IsRotationEnabled, false));

		dashboard.Render(p => p.Add(x => x.IsRotationEnabled, true).Add(x => x.RotationIntervalSeconds, 1));

		dashboard.WaitForAssertion(() => dashboard.Instance.ActiveTabIndex.Should().Be(1), TimeSpan.FromSeconds(5));
	}
}
