using AwesomeAssertions;

namespace PanoramicData.Blazor.Test.Components;

/// <summary>
/// Tests for the drawing utilities of <see cref="PDTimeline"/>.
/// </summary>
public partial class PDTimelineTests
{
	/// <summary>
	/// Verifies the arc path, including the large-arc flag for sweeps over 180 degrees.
	/// </summary>
	[Fact]
	public void DescribeArc_BuildsSvgArc()
	{
		PDTimeline.Utilities.DescribeArc(50, 50, 10, 0, 90).Should().Be("M 50.00 60.00 A 10 10 0 0 0 60.00 50.00");
		PDTimeline.Utilities.DescribeArc(50, 50, 10, 0, 270).Should().Be("M 50.00 40.00 A 10 10 0 1 0 60.00 50.00");
	}

	/// <summary>
	/// Verifies the polar to cartesian conversion measures from the positive X axis.
	/// </summary>
	[Fact]
	public void PolarToCartesian_MeasuresFromXAxis()
	{
		var (x, y) = PDTimeline.Utilities.PolarToCartesian(10, 20, 5, 90);

		x.Should().BeApproximately(10, 1e-9);
		y.Should().BeApproximately(25, 1e-9);
	}

	/// <summary>
	/// Verifies the left- and right-facing arrow paths.
	/// </summary>
	[Fact]
	public void ArrowPath_FacesEitherWay()
	{
		PDTimeline.Utilities.ArrowPath(0, 100, 20, 5, true).Should().Be("M 5 50l 10 -10l 0 20Z");
		PDTimeline.Utilities.ArrowPath(380, 100, 20, 5, false).Should().Be("M 395 50l -10 -10l 0 20Z");
	}

	/// <summary>
	/// Verifies the text-info defaults.
	/// </summary>
	[Fact]
	public void TextInfo_HasDefaults()
	{
		var info = new PDTimeline.TextInfo { Text = "a", Skip = 2 };

		info.OffsetX.Should().Be(3);
		info.OffsetY.Should().Be(14);
		info.Skip.Should().Be(2);
		info.Text.Should().Be("a");
	}
}
