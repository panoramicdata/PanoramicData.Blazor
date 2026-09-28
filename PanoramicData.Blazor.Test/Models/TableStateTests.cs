using AwesomeAssertions;
using PanoramicData.Blazor.Models;

namespace PanoramicData.Blazor.Test.Models;

/// <summary>Tests for <see cref="TableState"/>.</summary>
public class TableStateTests
{
	/// <summary>A new table state has no column state, and column state can be added and replaced.</summary>
	[Fact]
	public void Columns_StartEmptyAndRoundTrip()
	{
		var state = new TableState();
		state.Columns.Should().BeEmpty();

		state.Columns["Name"] = new ColumnState();
		state.Columns.Should().ContainKey("Name");

		state.Columns = new Dictionary<string, ColumnState>();
		state.Columns.Should().BeEmpty();
	}
}
