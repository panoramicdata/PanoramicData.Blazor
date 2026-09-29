using AwesomeAssertions;
using PanoramicData.Blazor.Models;

namespace PanoramicData.Blazor.Test.Models;

/// <summary>Tests for <see cref="ZoombarValue"/>.</summary>
public class ZoomPanInfoTests
{
	/// <summary>A new value is at 100 percent zoom with no pan.</summary>
	[Fact]
	public void New_IsFullZoomNoPan()
	{
		var value = new ZoombarValue();

		value.Zoom.Should().Be(100);
		value.Pan.Should().Be(0);
	}

	/// <summary>Zoom and pan are rounded to two decimal places when set.</summary>
	[Fact]
	public void ZoomAndPan_AreRoundedToTwoPlaces()
	{
		var value = new ZoombarValue { Zoom = 33.3333, Pan = 12.3456 };

		value.Zoom.Should().Be(33.33);
		value.Pan.Should().Be(12.35);
	}
}
