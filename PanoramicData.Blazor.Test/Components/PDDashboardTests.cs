using AwesomeAssertions;
using Bunit;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.Extensions.DependencyInjection;
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
		dashboard.Instance.Id.Should().MatchRegex("^pd-dashboard-[0-9]+$");
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

	/// <summary>A component that captures the cascading dashboard properties.</summary>
	private sealed class PropertiesProbe : ComponentBase
	{
		/// <summary>The captured properties.</summary>
		[CascadingParameter(Name = "DashboardProperties")]
		public Dictionary<string, string>? Properties { get; set; }
	}
}
