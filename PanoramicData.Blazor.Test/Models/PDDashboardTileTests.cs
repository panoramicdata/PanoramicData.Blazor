using AwesomeAssertions;
using Microsoft.AspNetCore.Components;
using PanoramicData.Blazor.Models;

namespace PanoramicData.Blazor.Test.Models;

/// <summary>Tests for <see cref="PDDashboardTile"/>.</summary>
public class PDDashboardTileTests
{
	/// <summary>A new tile sits at the top left and spans one row and one column.</summary>
	[Fact]
	public void New_HasDocumentedDefaults()
	{
		var tile = new PDDashboardTile();

		tile.RowIndex.Should().Be(0);
		tile.ColumnIndex.Should().Be(0);
		tile.RowSpanCount.Should().Be(1);
		tile.ColumnSpanCount.Should().Be(1);
		tile.Css.Should().BeNull();
		tile.ShowMaximize.Should().BeNull();
		tile.ChildContent.Should().BeNull();
	}

	/// <summary>All members round-trip.</summary>
	[Fact]
	public void SettableMembers_RoundTrip()
	{
		RenderFragment content = builder => builder.AddContent(0, "Hi");

		var tile = new PDDashboardTile
		{
			RowIndex = 1,
			ColumnIndex = 2,
			RowSpanCount = 3,
			ColumnSpanCount = 4,
			Css = "tile",
			ShowMaximize = false,
			ChildContent = content
		};

		tile.RowIndex.Should().Be(1);
		tile.ColumnIndex.Should().Be(2);
		tile.RowSpanCount.Should().Be(3);
		tile.ColumnSpanCount.Should().Be(4);
		tile.Css.Should().Be("tile");
		tile.ShowMaximize.Should().BeFalse();
		tile.ChildContent.Should().BeSameAs(content);
	}
}
