using AwesomeAssertions;
using Bunit;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.Extensions.DependencyInjection;
using PanoramicData.Blazor.Enums;
using PanoramicData.Blazor.Extensions;
using PanoramicData.Blazor.Models;

namespace PanoramicData.Blazor.Test.Components;

/// <summary>
/// Tests that <see cref="PDDashboard"/> renders its tabs and tiles, switches and rotates tabs, and supports
/// editing: adding, deleting, moving, resizing and maximising tiles, and configuring the dashboard.
/// </summary>
public partial class PDDashboardTests : BunitContext
{
	private readonly List<string> _events = [];

	/// <summary>Sets up the rendering context.</summary>
	public PDDashboardTests()
	{
		JSInterop.Mode = JSRuntimeMode.Loose;
		Services.AddPanoramicDataBlazor();
	}

	private NavigationManager Navigation => Services.GetRequiredService<NavigationManager>();

	private static PDDashboardTile Tile(int row, int col, string text, int colSpan = 1, int rowSpan = 1)
		=> new()
		{
			RowIndex = row,
			ColumnIndex = col,
			ColumnSpanCount = colSpan,
			RowSpanCount = rowSpan,
			ChildContent = b => b.AddMarkupContent(0, $"<span class=\"tile-text\">{text}</span>")
		};

	private static List<PDDashboardTab> TwoTabs() =>
	[
		new PDDashboardTab { Name = "One", Tiles = [Tile(0, 0, "A"), Tile(0, 1, "B")] },
		new PDDashboardTab { Name = "Two", ColumnCount = 4, TileRowHeightPx = 50, Css = "tab-two", Tiles = [Tile(0, 0, "C")] }
	];

	private IRenderedComponent<PDDashboard> RenderDashboard(List<PDDashboardTab> tabs, Action<ComponentParameterCollectionBuilder<PDDashboard>>? configure = null)
		=> Render<PDDashboard>(parameters =>
		{
			parameters
				.Add(p => p.Tabs, tabs)
				.Add(p => p.OnSettingsChanged, () => _events.Add("settings"));
			configure?.Invoke(parameters);
		});

	private static AngleSharp.Dom.IElement TileElement(IRenderedComponent<PDDashboard> dashboard, string text)
		=> dashboard.FindAll(".pd-dashboard-tile").Single(t => t.QuerySelector(".tile-text")?.TextContent == text);

	private static Task ToggleEditAsync(IRenderedComponent<PDDashboard> dashboard)
		=> dashboard.Find(".pd-dashboard-edit-btn").ClickAsync(new MouseEventArgs());

	/// <summary>With no tabs a single default tab is created; the default id is dashboard-specific.</summary>
	[Fact]
	public void NoTabs_CreatesADefaultTab()
	{
		var tabs = new List<PDDashboardTab>();

		var dashboard = RenderDashboard(tabs);

		tabs.Should().ContainSingle().Which.Name.Should().Be("Dashboard");
		// The generated id's prefix is not asserted: it depends on the static PDComponentBase.Sequence (issue #159).
		dashboard.Find(".pd-dashboard").Id.Should().Be(dashboard.Instance.Id);
		dashboard.FindAll(".pd-dashboard-tile").Should().BeEmpty();
	}

	/// <summary>A hidden dashboard renders nothing.</summary>
	[Fact]
	public void Hidden_RendersNothing()
	{
		var dashboard = RenderDashboard(TwoTabs(), p => p.Add(x => x.IsVisible, false));

		dashboard.Markup.Trim().Should().BeEmpty();
	}

	/// <summary>The active tab's tiles are placed on a grid with the dashboard's column count and row height.</summary>
	[Fact]
	public void Tiles_ArePlacedOnTheGrid()
	{
		var dashboard = RenderDashboard(TwoTabs(), p => p.Add(x => x.ColumnCount, 6).Add(x => x.TileRowHeightPx, 90).Add(x => x.Css, "dash"));

		var grid = dashboard.Find(".pd-dashboard-grid");
		grid.GetAttribute("style").Should().Contain("repeat(6, 1fr)").And.Contain("grid-auto-rows: 90px");
		TileElement(dashboard, "B").GetAttribute("style").Should().Contain("grid-column: 2 / span 1");
		dashboard.Find(".pd-dashboard").ClassList.Should().Contain("dash");
		dashboard.FindAll(".tile-text").Select(t => t.TextContent).Should().Equal("A", "B");
	}

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

	/// <summary>A component that captures the cascading dashboard properties.</summary>
	private sealed class PropertiesProbe : ComponentBase
	{
		/// <summary>The captured properties.</summary>
		[CascadingParameter(Name = "DashboardProperties")]
		public Dictionary<string, string>? Properties { get; set; }
	}
}
