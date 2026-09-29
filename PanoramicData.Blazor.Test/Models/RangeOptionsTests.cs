using AwesomeAssertions;
using PanoramicData.Blazor.Models;

namespace PanoramicData.Blazor.Test.Models;

/// <summary>Tests for <see cref="RangeOptions"/> and <see cref="TrackOptions"/>.</summary>
public class RangeOptionsTests
{
	/// <summary>By default the track fills half of the available height.</summary>
	[Fact]
	public void New_TrackIsHalfHeight()
	{
		new RangeOptions().Track.Height.Should().Be(0.5);
	}

	/// <summary>The records compare by value and can be copied with changes.</summary>
	[Fact]
	public void Records_CompareByValue()
	{
		var options = new RangeOptions { Track = new TrackOptions { Height = 0.25 } };

		options.Should().Be(new RangeOptions { Track = new TrackOptions { Height = 0.25 } });
		(options with { Track = new TrackOptions() }).Track.Height.Should().Be(0.5);
		options.Track.Should().NotBe(new TrackOptions());
	}
}
