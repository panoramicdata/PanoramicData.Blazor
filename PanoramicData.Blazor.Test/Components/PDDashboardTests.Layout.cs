using AwesomeAssertions;
using Bunit;
using Microsoft.AspNetCore.Components.Web;
using PanoramicData.Blazor.Models;

namespace PanoramicData.Blazor.Test.Components;

/// <summary>
/// Tile drag and drop, resizing and placement tests for <see cref="PDDashboard"/>.
/// </summary>
public partial class PDDashboardTests
{
	/// <summary>Dragging a tile over another moves it there, and dropping raises OnTileMove.</summary>
	[Fact]
	public async Task DragAndDrop_MovesATile()
	{
		var tabs = TwoTabs();
		var moves = new List<(int Row, int Col)>();
		var dashboard = RenderDashboard(tabs, p => p
			.Add(x => x.IsEditable, true)
			.Add(x => x.OnTileMove, ((PDDashboardTile Tile, int NewRow, int NewColumn) m) => moves.Add((m.NewRow, m.NewColumn))));
		var a = tabs[0].Tiles[0];
		var b = tabs[0].Tiles[1];

		await TileElement(dashboard, "A").DragStartAsync(new DragEventArgs());
		TileElement(dashboard, "A").ClassList.Should().Contain("pd-dashboard-tile-dragging");
		await TileElement(dashboard, "B").DragOverAsync(new DragEventArgs());
		TileElement(dashboard, "B").ClassList.Should().Contain("pd-dashboard-tile-dragover");
		await TileElement(dashboard, "B").DropAsync(new DragEventArgs());

		a.ColumnIndex.Should().Be(1);
		b.ColumnIndex.Should().Be(0);
		moves.Should().Equal((0, 1));
		_events.Should().Equal("settings");
	}

	/// <summary>
	/// Dragging a tile moves it when edit mode was entered with the built-in edit button rather than the
	/// IsEditable parameter (issue #168).
	/// </summary>
	[Fact]
	public async Task DragAndDrop_MovesATile_InEditModeFromTheEditButton()
	{
		var tabs = TwoTabs();
		var moves = new List<(int Row, int Col)>();
		var dashboard = RenderDashboard(tabs, p => p
			.Add(x => x.OnTileMove, ((PDDashboardTile Tile, int NewRow, int NewColumn) m) => moves.Add((m.NewRow, m.NewColumn))));
		await ToggleEditAsync(dashboard);
		dashboard.Instance.EffectiveIsEditable.Should().BeTrue();

		await TileElement(dashboard, "A").DragStartAsync(new DragEventArgs());
		await TileElement(dashboard, "B").DragOverAsync(new DragEventArgs());
		await TileElement(dashboard, "B").DropAsync(new DragEventArgs());

		tabs[0].Tiles[0].ColumnIndex.Should().Be(1);
		tabs[0].Tiles[1].ColumnIndex.Should().Be(0);
		moves.Should().Equal((0, 1));
	}

	/// <summary>Dropping a tile on itself restores the original layout and raises no move.</summary>
	[Fact]
	public async Task DropOnSelf_RestoresTheLayout()
	{
		var tabs = TwoTabs();
		var dashboard = RenderDashboard(tabs, p => p.Add(x => x.IsEditable, true));

		await TileElement(dashboard, "A").DragStartAsync(new DragEventArgs());
		await TileElement(dashboard, "B").DragOverAsync(new DragEventArgs());
		await TileElement(dashboard, "B").DragLeaveAsync(new DragEventArgs());
		await TileElement(dashboard, "A").DropAsync(new DragEventArgs());

		tabs[0].Tiles[0].ColumnIndex.Should().Be(0);
		tabs[0].Tiles[1].ColumnIndex.Should().Be(1);
		_events.Should().BeEmpty();
	}

	/// <summary>A cancelled drag, by drag end or Escape, restores the original layout.</summary>
	[Theory]
	[InlineData(true)]
	[InlineData(false)]
	public async Task CancelledDrag_RestoresTheLayout(bool viaEscape)
	{
		var tabs = TwoTabs();
		var dashboard = RenderDashboard(tabs, p => p.Add(x => x.IsEditable, true));

		await TileElement(dashboard, "A").DragStartAsync(new DragEventArgs());
		await TileElement(dashboard, "B").DragOverAsync(new DragEventArgs());
		tabs[0].Tiles[0].ColumnIndex.Should().Be(1, "the drag previews the move");

		if (viaEscape)
		{
			await dashboard.Find(".pd-dashboard").KeyDownAsync(new KeyboardEventArgs { Key = "Escape" });
		}
		else
		{
			await TileElement(dashboard, "A").DragEndAsync(new DragEventArgs());
		}

		tabs[0].Tiles[0].ColumnIndex.Should().Be(0);
		tabs[0].Tiles[1].ColumnIndex.Should().Be(1);
	}

	/// <summary>Dropping on empty grid space finalises the previewed layout.</summary>
	[Fact]
	public async Task DropOnTheGrid_FinalisesTheMove()
	{
		var tabs = TwoTabs();
		var moves = 0;
		var dashboard = RenderDashboard(tabs, p => p
			.Add(x => x.IsEditable, true)
			.Add(x => x.OnTileMove, ((PDDashboardTile Tile, int NewRow, int NewColumn) _) => moves++));

		await TileElement(dashboard, "A").DragStartAsync(new DragEventArgs());
		await TileElement(dashboard, "B").DragOverAsync(new DragEventArgs());
		await dashboard.Find(".pd-dashboard-grid").DropAsync(new DragEventArgs());
		await dashboard.Find(".pd-dashboard-grid").DropAsync(new DragEventArgs());

		moves.Should().Be(1);
		tabs[0].Tiles[0].ColumnIndex.Should().Be(1);
		_events.Should().Equal("settings");
	}

	/// <summary>Dragging the resize handle changes the tile's spans and raises OnTileResize on release.</summary>
	[Fact]
	public async Task Resize_ChangesTheSpans()
	{
		var tabs = TwoTabs();
		var resizes = new List<(int Rows, int Cols)>();
		var dashboard = RenderDashboard(tabs, p => p
			.Add(x => x.IsEditable, true)
			.Add(x => x.TileRowHeightPx, 100)
			.Add(x => x.OnTileResize, ((PDDashboardTile Tile, int NewRowSpan, int NewColumnSpan) r) => resizes.Add((r.NewRowSpan, r.NewColumnSpan))));

		await TileElement(dashboard, "A").QuerySelector(".pd-dashboard-tile-resize-handle")!.PointerDownAsync(new PointerEventArgs { ClientX = 0, ClientY = 0 });
		await dashboard.Find(".pd-dashboard-resize-overlay").PointerMoveAsync(new PointerEventArgs { ClientX = 210, ClientY = 190 });
		TileElement(dashboard, "A").GetAttribute("data-resize-info").Should().Be("3 × 3");
		await dashboard.Find(".pd-dashboard-resize-overlay").PointerUpAsync(new PointerEventArgs());

		resizes.Should().Equal((3, 3));
		tabs[0].Tiles[1].ColumnIndex.Should().Be(3, "B is moved clear of the enlarged tile");
		dashboard.FindAll(".pd-dashboard-resize-overlay").Should().BeEmpty();
		_events.Should().Equal("settings");
	}

	/// <summary>A resize is clamped to the columns remaining to the tile's right, and never below one.</summary>
	[Fact]
	public async Task Resize_IsClamped()
	{
		var tabs = TwoTabs();
		var dashboard = RenderDashboard(tabs, p => p.Add(x => x.IsEditable, true).Add(x => x.ColumnCount, 4).Add(x => x.TileRowHeightPx, 100));

		await TileElement(dashboard, "B").QuerySelector(".pd-dashboard-tile-resize-handle")!.PointerDownAsync(new PointerEventArgs());
		await dashboard.Find(".pd-dashboard-resize-overlay").PointerMoveAsync(new PointerEventArgs { ClientX = 5000, ClientY = -5000 });

		tabs[0].Tiles[1].ColumnSpanCount.Should().Be(3);
		tabs[0].Tiles[1].RowSpanCount.Should().Be(1);
	}

	/// <summary>Outside edit mode a drag moves nothing.</summary>
	[Fact]
	public async Task Drag_OutsideEditMode_MovesNothing()
	{
		var tabs = TwoTabs();
		var dashboard = RenderDashboard(tabs);

		await TileElement(dashboard, "A").DragStartAsync(new DragEventArgs());
		await TileElement(dashboard, "B").DragOverAsync(new DragEventArgs());

		tabs[0].Tiles[0].ColumnIndex.Should().Be(0);
		tabs[0].Tiles[1].ColumnIndex.Should().Be(1);
	}

	/// <summary>Dragging over a tile with no drag in progress moves nothing.</summary>
	[Fact]
	public async Task DragOver_WithoutADrag_MovesNothing()
	{
		var tabs = TwoTabs();
		var dashboard = RenderDashboard(tabs, p => p.Add(x => x.IsEditable, true));

		await TileElement(dashboard, "B").DragOverAsync(new DragEventArgs());

		tabs[0].Tiles[0].ColumnIndex.Should().Be(0);
		tabs[0].Tiles[1].ColumnIndex.Should().Be(1);
		TileElement(dashboard, "B").ClassList.Should().NotContain("pd-dashboard-tile-dragover");
	}

	/// <summary>A pointer move with no resize in progress changes no tile.</summary>
	[Fact]
	public async Task ResizeMove_WithoutAResize_ChangesNothing()
	{
		var tabs = TwoTabs();
		var dashboard = RenderDashboard(tabs, p => p.Add(x => x.IsEditable, true));

		await dashboard.InvokeAsync(() => dashboard.Instance.OnResizePointerMove(new PointerEventArgs { ClientX = 500, ClientY = 500 }));

		tabs[0].Tiles.Should().AllSatisfy(t => (t.ColumnSpanCount, t.RowSpanCount).Should().Be((1, 1)));
	}

	/// <summary>
	/// Packing the grid around a tile wider than the grid places it on the next empty row rather than searching
	/// for a column that cannot exist.
	/// </summary>
	[Fact]
	public async Task Drag_WithATileWiderThanTheGrid_StillPacksTheGrid()
	{
		var tabs = new List<PDDashboardTab>
		{
			new() { ColumnCount = 2, Tiles = [Tile(0, 0, "A"), Tile(0, 1, "B"), Tile(1, 0, "Wide", colSpan: 4)] }
		};
		var dashboard = RenderDashboard(tabs, p => p.Add(x => x.IsEditable, true));

		await TileElement(dashboard, "A").DragStartAsync(new DragEventArgs());
		await TileElement(dashboard, "B").DragOverAsync(new DragEventArgs());

		tabs[0].Tiles[0].ColumnIndex.Should().Be(1);
		tabs[0].Tiles[1].ColumnIndex.Should().Be(0);
		(tabs[0].Tiles[2].RowIndex, tabs[0].Tiles[2].ColumnIndex).Should().Be((1, 0));
	}

	/// <summary>FindNextAvailablePosition finds the first gap that fits, or the next empty row.</summary>
	[Fact]
	public void FindNextAvailablePosition_FindsTheFirstGap()
	{
		var tabs = new List<PDDashboardTab> { new() { Tiles = [Tile(0, 0, "A"), Tile(0, 2, "B"), Tile(1, 0, "C", colSpan: 3)] } };
		var dashboard = RenderDashboard(tabs, p => p.Add(x => x.ColumnCount, 3));

		dashboard.Instance.FindNextAvailablePosition().Should().Be((0, 1));
		dashboard.Instance.FindNextAvailablePosition(colSpan: 2).Should().Be((2, 0));
		dashboard.Instance.FindNextAvailablePosition(colSpan: 1, rowSpan: 3).Should().Be((2, 0));
	}

	/// <summary>An empty tab places the next tile at the origin.</summary>
	[Fact]
	public void FindNextAvailablePosition_OnAnEmptyTab_IsTheOrigin()
	{
		var dashboard = RenderDashboard([new PDDashboardTab()]);

		dashboard.Instance.FindNextAvailablePosition(2, 2).Should().Be((0, 0));
	}

	/// <summary>With no tab showing, the next tile would go at the origin.</summary>
	[Fact]
	public void FindNextAvailablePosition_WithNoActiveTab_IsTheOrigin()
	{
		var dashboard = RenderDashboard(TwoTabs(), p => p.Add(x => x.StartTab, 5));

		dashboard.Instance.FindNextAvailablePosition().Should().Be((0, 0));
	}
}
