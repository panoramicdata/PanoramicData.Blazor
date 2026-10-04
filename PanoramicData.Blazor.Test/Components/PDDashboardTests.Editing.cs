using AwesomeAssertions;
using Bunit;
using Microsoft.AspNetCore.Components.Web;
using PanoramicData.Blazor.Models;

namespace PanoramicData.Blazor.Test.Components;

/// <summary>
/// Edit mode, tile and maximise tests for <see cref="PDDashboard"/>.
/// </summary>
public partial class PDDashboardTests
{
	/// <summary>The edit button toggles edit mode and raises OnEditModeChanged each time.</summary>
	[Fact]
	public async Task EditButton_TogglesEditMode()
	{
		var modes = new List<bool>();
		var dashboard = RenderDashboard(TwoTabs(), p => p.Add(x => x.OnEditModeChanged, (bool b) => modes.Add(b)));
		dashboard.Find(".pd-dashboard-edit-btn").GetAttribute("title").Should().Be("Edit dashboard");

		await ToggleEditAsync(dashboard);
		dashboard.Instance.EffectiveIsEditable.Should().BeTrue();
		dashboard.Find(".pd-dashboard-edit-btn").GetAttribute("title").Should().Be("Done editing");
		dashboard.FindAll(".pd-dashboard-tile-add").Should().ContainSingle();

		await ToggleEditAsync(dashboard);
		dashboard.Instance.EffectiveIsEditable.Should().BeFalse();
		modes.Should().Equal(true, false);
	}

	/// <summary>Changing the IsEditable parameter raises OnEditModeChanged and hides the built-in edit button.</summary>
	[Fact]
	public void IsEditableParameter_RaisesOnEditModeChanged()
	{
		var modes = new List<bool>();
		var dashboard = RenderDashboard(TwoTabs(), p => p.Add(x => x.OnEditModeChanged, (bool b) => modes.Add(b)));

		dashboard.Render(p => p.Add(x => x.IsEditable, true));

		modes.Should().Equal(true);
		dashboard.FindAll(".pd-dashboard-edit-btn").Should().BeEmpty();
		dashboard.FindAll(".pd-dashboard-settings-btn").Should().ContainSingle();
	}

	/// <summary>Without an OnTileAdd handler, the add widget tile adds a blank widget at the next free position.</summary>
	[Fact]
	public async Task AddTile_WithoutAHandler_AddsABlankWidget()
	{
		var tabs = TwoTabs();
		var dashboard = RenderDashboard(tabs, p => p.Add(x => x.IsEditable, true));

		await dashboard.Find(".pd-dashboard-tile-add").ClickAsync(new MouseEventArgs());

		tabs[0].Tiles.Should().HaveCount(3);
		tabs[0].Tiles[2].ColumnIndex.Should().Be(2);
		dashboard.Markup.Should().Contain("New Widget");
		_events.Should().Equal("settings");
	}

	/// <summary>With an OnTileAdd handler, adding a tile is left to the handler.</summary>
	[Fact]
	public async Task AddTile_WithAHandler_DelegatesToIt()
	{
		var tabs = TwoTabs();
		var dashboard = RenderDashboard(tabs, p => p.Add(x => x.IsEditable, true).Add(x => x.OnTileAdd, () => _events.Add("add")));

		await dashboard.Find(".pd-dashboard-tile-add").ClickAsync(new MouseEventArgs());

		tabs[0].Tiles.Should().HaveCount(2);
		_events.Should().Equal("add", "settings");
	}

	/// <summary>Without confirmation, deleting a tile removes it, compacts the rest and raises OnTileDelete.</summary>
	[Fact]
	public async Task DeleteTile_WithoutConfirmation_RemovesAndCompacts()
	{
		var tabs = TwoTabs();
		PDDashboardTile? deleted = null;
		var dashboard = RenderDashboard(tabs, p => p
			.Add(x => x.IsEditable, true)
			.Add(x => x.ConfirmTileDelete, false)
			.Add(x => x.OnTileDelete, (PDDashboardTile t) => deleted = t));
		var first = tabs[0].Tiles[0];

		await TileElement(dashboard, "A").QuerySelector("button[title='Delete']")!.ClickAsync(new MouseEventArgs());

		deleted.Should().BeSameAs(first);
		tabs[0].Tiles.Should().ContainSingle().Which.ColumnIndex.Should().Be(0, "B moves left to fill the gap");
		_events.Should().Equal("settings");
	}

	/// <summary>With confirmation, a tile is deleted only when the user answers Yes.</summary>
	[Theory]
	[InlineData("Yes", 1)]
	[InlineData("No", 2)]
	public async Task DeleteTile_WithConfirmation_FollowsTheAnswer(string answer, int remaining)
	{
		var tabs = TwoTabs();
		var dashboard = RenderDashboard(tabs, p => p.Add(x => x.IsEditable, true));
		dashboard.Find(".modal-title").TextContent.Should().Be("Delete Widget");

		var deleting = dashboard.InvokeAsync(() => TileElement(dashboard, "A").QuerySelector("button[title='Delete']")!.ClickAsync(new MouseEventArgs()));
		dashboard.WaitForAssertion(() => dashboard.FindAll($"#pd-tbr-btn-{answer}").Should().ContainSingle());
		await dashboard.Find($"#pd-tbr-btn-{answer}").ClickAsync(new MouseEventArgs());
		await deleting;

		tabs[0].Tiles.Should().HaveCount(remaining);
	}

	/// <summary>Maximising a tile enlarges it with a backdrop, and restoring returns it to the grid.</summary>
	[Fact]
	public async Task MaximiseAndRestore()
	{
		var dashboard = RenderDashboard(TwoTabs(), p => p.Add(x => x.ShowMaximize, true).Add(x => x.MaximizePercent, 70));

		await TileElement(dashboard, "A").QuerySelector("button[title='Maximize']")!.ClickAsync(new MouseEventArgs());
		TileElement(dashboard, "A").ClassList.Should().Contain("pd-dashboard-tile-maximized");
		TileElement(dashboard, "A").GetAttribute("style").Should().Contain("width: 70%");
		dashboard.FindAll(".pd-dashboard-maximize-backdrop").Should().ContainSingle();

		await dashboard.Find(".pd-dashboard-maximize-close").ClickAsync(new MouseEventArgs());
		TileElement(dashboard, "A").ClassList.Should().NotContain("pd-dashboard-tile-maximized");

		await TileElement(dashboard, "A").QuerySelector("button[title='Maximize']")!.ClickAsync(new MouseEventArgs());
		await dashboard.Find(".pd-dashboard-maximize-backdrop").ClickAsync(new MouseEventArgs());
		dashboard.FindAll(".pd-dashboard-maximize-backdrop").Should().BeEmpty();
	}

	/// <summary>A tile can opt out of the maximise button, and the button is absent in view mode by default.</summary>
	[Fact]
	public void MaximiseButton_FollowsTheTileAndDashboardSettings()
	{
		var tabs = TwoTabs();
		tabs[0].Tiles[1].ShowMaximize = true;

		var dashboard = RenderDashboard(tabs);

		TileElement(dashboard, "A").QuerySelectorAll("button[title='Maximize']").Should().BeEmpty();
		TileElement(dashboard, "B").QuerySelectorAll("button[title='Maximize']").Should().ContainSingle();
	}
}
