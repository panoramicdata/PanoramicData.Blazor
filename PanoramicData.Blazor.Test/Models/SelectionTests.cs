using AwesomeAssertions;
using PanoramicData.Blazor.Models;

namespace PanoramicData.Blazor.Test.Models;

/// <summary>Tests for <see cref="Selection{TItem}"/>.</summary>
public class SelectionTests
{
	/// <summary>An empty selection describes itself as none.</summary>
	[Fact]
	public void ToString_Empty_IsNone()
	{
		var selection = new Selection<string>();

		selection.AllSelected.Should().BeFalse();
		selection.Items.Should().BeEmpty();
		selection.ToString().Should().Be("(None)");
	}

	/// <summary>A selection of everything describes itself as all, whatever items it also holds.</summary>
	[Fact]
	public void ToString_AllSelected_IsAll()
	{
		var selection = new Selection<string> { AllSelected = true, Items = ["a"] };

		selection.ToString().Should().Be("(All)");
	}

	/// <summary>A partial selection lists its items, showing a null item as an empty entry.</summary>
	[Fact]
	public void ToString_Items_ListsThem()
	{
		var selection = new Selection<string?> { Items = ["a", null, "c"] };

		selection.ToString().Should().Be("a, , c");
	}
}
