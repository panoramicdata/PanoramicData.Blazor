using AwesomeAssertions;
using PanoramicData.Blazor.Arguments;

namespace PanoramicData.Blazor.Test.Arguments;

/// <summary>Tests for <see cref="DragOrderChangeArgs{TItem}"/>.</summary>
public class DragOrderChangeArgsTests
{
	/// <summary>The constructor captures the reordered items and the item that moved.</summary>
	[Fact]
	public void Constructor_CapturesItemsAndMovedItem()
	{
		string[] items = ["b", "a", "c"];

		var args = new DragOrderChangeArgs<string>(items, "b");

		args.Items.Should().Equal("b", "a", "c");
		args.Item.Should().Be("b");
	}
}
