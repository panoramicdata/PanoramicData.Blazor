using AwesomeAssertions;
using PanoramicData.Blazor.Models;

namespace PanoramicData.Blazor.Test.Models;

/// <summary>Tests for <see cref="PDDashboardTab"/>.</summary>
public class PDDashboardTabTests
{
	/// <summary>A new tab has no name, tiles or overrides.</summary>
	[Fact]
	public void New_IsEmpty()
	{
		var tab = new PDDashboardTab();

		tab.Name.Should().BeEmpty();
		tab.Css.Should().BeNull();
		tab.ColumnCount.Should().BeNull();
		tab.TileRowHeightPx.Should().BeNull();
		tab.RotationIntervalSecondsOverride.Should().BeNull();
		tab.Tiles.Should().BeEmpty();
	}

	/// <summary>All members round-trip.</summary>
	[Fact]
	public void SettableMembers_RoundTrip()
	{
		var tab = new PDDashboardTab
		{
			Name = "Overview",
			Css = "tab-dark",
			ColumnCount = 4,
			TileRowHeightPx = 120,
			RotationIntervalSecondsOverride = 30,
			Tiles = [new PDDashboardTile()]
		};

		tab.Name.Should().Be("Overview");
		tab.Css.Should().Be("tab-dark");
		tab.ColumnCount.Should().Be(4);
		tab.TileRowHeightPx.Should().Be(120);
		tab.RotationIntervalSecondsOverride.Should().Be(30);
		tab.Tiles.Should().ContainSingle();
	}
}
