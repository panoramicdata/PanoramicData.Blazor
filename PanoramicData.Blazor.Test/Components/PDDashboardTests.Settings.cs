using AwesomeAssertions;
using Bunit;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;
using PanoramicData.Blazor.Models;

namespace PanoramicData.Blazor.Test.Components;

/// <summary>
/// Settings dialog, view-mode property override and disposal tests for <see cref="PDDashboard"/>.
/// </summary>
public partial class PDDashboardTests
{
	/// <summary>The settings dialog edits the dashboard name, tab name, grid and properties, applied on Apply.</summary>
	[Fact]
	public async Task SettingsDialog_AppliesChanges()
	{
		var tabs = TwoTabs();
		var dashboard = RenderDashboard(tabs, p => p
			.Add(x => x.IsEditable, true)
			.Add(x => x.Name, "Ops")
			.Add(x => x.Properties, new Dictionary<string, string> { ["region"] = "eu", ["old"] = "x" }));

		await dashboard.Find(".pd-dashboard-settings-btn").ClickAsync(new MouseEventArgs());
		var inputs = dashboard.FindAll(".pd-dashboard-config-field input");
		inputs[0].GetAttribute("value").Should().Be("Ops");
		await inputs[0].ChangeAsync(new ChangeEventArgs { Value = "  Support  " });
		await dashboard.FindAll(".pd-dashboard-config-field input")[1].ChangeAsync(new ChangeEventArgs { Value = "Main" });
		await dashboard.FindAll(".pd-dashboard-config-field input")[2].ChangeAsync(new ChangeEventArgs { Value = "8" });
		await dashboard.FindAll(".pd-dashboard-config-field input")[3].ChangeAsync(new ChangeEventArgs { Value = "60" });

		var rows = dashboard.FindAll(".pd-dashboard-config-property-row");
		await rows[0].QuerySelectorAll("input")[1].InputAsync(new ChangeEventArgs { Value = "us" });
		await dashboard.FindAll(".pd-dashboard-config-property-row")[1].QuerySelector("button")!.ClickAsync(new MouseEventArgs());
		var newRow = dashboard.FindAll(".pd-dashboard-config-property-row")[^1];
		await newRow.QuerySelectorAll("input")[0].ChangeAsync(new ChangeEventArgs { Value = " team " });
		await dashboard.FindAll(".pd-dashboard-config-property-row")[^1].QuerySelectorAll("input")[1].ChangeAsync(new ChangeEventArgs { Value = "blue" });
		await dashboard.FindAll(".pd-dashboard-config-property-row")[^1].QuerySelector("button")!.ClickAsync(new MouseEventArgs());

		await dashboard.Find(".pd-dashboard-config-footer .btn-primary").ClickAsync(new MouseEventArgs());

		dashboard.Instance.Name.Should().Be("Support");
		tabs[0].Name.Should().Be("Main");
		tabs[0].ColumnCount.Should().Be(8);
		tabs[0].TileRowHeightPx.Should().Be(60);
		dashboard.Instance.Properties.Should().BeEquivalentTo(new Dictionary<string, string> { ["region"] = "us", ["team"] = "blue" });
		dashboard.FindAll(".pd-dashboard-config-dialog").Should().BeEmpty();
		_events.Should().Equal("settings");
	}

	/// <summary>Cancelling the settings dialog discards changes; a blank name and no properties clear both.</summary>
	[Fact]
	public async Task SettingsDialog_CancelDiscards_AndBlankValuesClear()
	{
		var tabs = TwoTabs();
		var dashboard = RenderDashboard(tabs, p => p.Add(x => x.IsEditable, true).Add(x => x.Name, "Ops"));

		await dashboard.Find(".pd-dashboard-settings-btn").ClickAsync(new MouseEventArgs());
		await dashboard.FindAll(".pd-dashboard-config-field input")[1].ChangeAsync(new ChangeEventArgs { Value = "Changed" });
		await dashboard.Find(".pd-dashboard-config-footer .btn-outline-secondary").ClickAsync(new MouseEventArgs());
		tabs[0].Name.Should().Be("One");

		await dashboard.Find(".pd-dashboard-settings-btn").ClickAsync(new MouseEventArgs());
		await dashboard.FindAll(".pd-dashboard-config-field input")[0].ChangeAsync(new ChangeEventArgs { Value = " " });
		await dashboard.FindAll(".pd-dashboard-config-property-row")[^1].QuerySelector("button")!.ClickAsync(new MouseEventArgs());
		await dashboard.Find(".pd-dashboard-config-footer .btn-primary").ClickAsync(new MouseEventArgs());

		dashboard.Instance.Name.Should().BeNull();
		dashboard.Instance.Properties.Should().BeNull();
	}

	/// <summary>Property overrides in view mode are cascaded to widgets and can be reset.</summary>
	[Fact]
	public async Task ViewModePropertyOverrides_AreCascaded()
	{
		var tabs = new List<PDDashboardTab>
		{
			new()
			{
				Tiles =
				[
					new PDDashboardTile
					{
						ChildContent = b =>
						{
							b.OpenComponent<PropertiesProbe>(0);
							b.CloseComponent();
						}
					}
				]
			}
		};
		var dashboard = RenderDashboard(tabs, p => p
			.Add(x => x.AllowViewModePropertyEdit, true)
			.Add(x => x.Properties, new Dictionary<string, string> { ["region"] = "eu" }));
		var probe = dashboard.FindComponent<PropertiesProbe>();
		probe.Instance.Properties!["region"].Should().Be("eu");

		await dashboard.Find(".pd-dashboard-props-btn").ClickAsync(new MouseEventArgs());
		await dashboard.Find(".pd-dashboard-config-field input").InputAsync(new ChangeEventArgs { Value = "us" });
		await dashboard.Find(".pd-dashboard-config-footer .btn-primary").ClickAsync(new MouseEventArgs());
		dashboard.FindComponent<PropertiesProbe>().Instance.Properties!["region"].Should().Be("us");

		await dashboard.Find(".pd-dashboard-props-btn").ClickAsync(new MouseEventArgs());
		await dashboard.Find(".pd-dashboard-config-footer .btn-outline-secondary").ClickAsync(new MouseEventArgs());
		dashboard.FindComponent<PropertiesProbe>().Instance.Properties!["region"].Should().Be("eu");
	}

	/// <summary>With no properties defined, the override dialog says so; overrides still reach widgets.</summary>
	[Fact]
	public async Task ViewModePropertyOverrides_WithNoProperties_SaySo()
	{
		var dashboard = RenderDashboard(TwoTabs(), p => p.Add(x => x.AllowViewModePropertyEdit, true));

		await dashboard.Find(".pd-dashboard-props-btn").ClickAsync(new MouseEventArgs());

		dashboard.Find(".pd-dashboard-config-body p").TextContent.Should().Be("No dashboard properties are defined.");
		await dashboard.Find(".pd-dashboard-config-close").ClickAsync(new MouseEventArgs());
		dashboard.FindAll(".pd-dashboard-config-dialog").Should().BeEmpty();
	}

	/// <summary>Disposing the dashboard stops its timer and is safe.</summary>
	[Fact]
	public async Task Dispose_IsSafe()
	{
		var dashboard = RenderDashboard(TwoTabs(), p => p.Add(x => x.IsRotationEnabled, true));

		var act = () => dashboard.InvokeAsync(() => dashboard.Instance.DisposeAsync().AsTask());

		await act.Should().NotThrowAsync();
	}
}
