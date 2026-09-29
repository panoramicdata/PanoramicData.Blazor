using AwesomeAssertions;
using PanoramicData.Blazor.Models;

namespace PanoramicData.Blazor.Test.Models;

/// <summary>Tests for <see cref="SplitOptions"/>.</summary>
public class SplitOptionsTests
{
	/// <summary>A new set of options describes a horizontal, centred split with a 30 pixel snap.</summary>
	[Fact]
	public void New_HasDocumentedDefaults()
	{
		var options = new SplitOptions();

		options.Sizes.Should().BeNull();
		options.MinSize.Should().BeNull();
		options.ExpandToMin.Should().BeFalse();
		options.GutterSize.Should().Be(0);
		options.GutterAlign.Should().Be("center");
		options.Direction.Should().Be("horizontal");
		options.SnapOffset.Should().Be(30);
		options.DragInterval.Should().Be(1);
		options.Cursor.Should().Be("col-resize");
	}

	/// <summary>All members round-trip.</summary>
	[Fact]
	public void SettableMembers_RoundTrip()
	{
		var options = new SplitOptions
		{
			Sizes = [30, 70],
			MinSize = [100, 200],
			ExpandToMin = true,
			GutterSize = 8,
			GutterAlign = "start",
			Direction = "vertical",
			SnapOffset = 0,
			DragInterval = 5,
			Cursor = "row-resize"
		};

		options.Sizes.Should().Equal(30, 70);
		options.MinSize.Should().Equal(100, 200);
		options.ExpandToMin.Should().BeTrue();
		options.GutterSize.Should().Be(8);
		options.GutterAlign.Should().Be("start");
		options.Direction.Should().Be("vertical");
		options.SnapOffset.Should().Be(0);
		options.DragInterval.Should().Be(5);
		options.Cursor.Should().Be("row-resize");
	}
}
