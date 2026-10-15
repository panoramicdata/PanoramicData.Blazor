using AwesomeAssertions;
using PanoramicData.Blazor.Models;
using System.Text.Json;

namespace PanoramicData.Blazor.Test.Models;

/// <summary>Tests for <see cref="MediaPlayerPersistedState"/>.</summary>
public class MediaPlayerPersistedStateTests
{
	/// <summary>Positions are kept per source, the latest save first, and can be forgotten.</summary>
	[Fact]
	public void Positions_AreKeptPerSource()
	{
		var state = new MediaPlayerPersistedState();

		state.SetPosition("a.wav", TimeSpan.FromSeconds(12.34));
		state.SetPosition("b.wav", TimeSpan.FromSeconds(50));
		state.SetPosition("a.wav", TimeSpan.FromSeconds(20));

		state.Positions.Select(p => p.Src).Should().Equal("a.wav", "b.wav");
		state.GetPosition("a.wav").Should().Be(TimeSpan.FromSeconds(20));
		state.GetPosition("c.wav").Should().BeNull();
		state.GetPosition(null).Should().BeNull();
		state.RemovePosition("a.wav").Should().BeTrue();
		state.RemovePosition("a.wav").Should().BeFalse();
		state.GetPosition("a.wav").Should().BeNull();
	}

	/// <summary>Positions are rounded to a tenth of a second, and zero, negative or non-finite ones are not resumed.</summary>
	[Fact]
	public void GetPosition_IgnoresUselessValues()
	{
		var state = new MediaPlayerPersistedState();
		state.SetPosition("a.wav", TimeSpan.FromSeconds(12.34));
		state.Positions.Add(new MediaPlayerPersistedPosition { Src = "zero.wav", Seconds = 0 });
		state.Positions.Add(new MediaPlayerPersistedPosition { Src = "nan.wav", Seconds = double.NaN });

		state.GetPosition("a.wav").Should().Be(TimeSpan.FromSeconds(12.3));
		state.GetPosition("zero.wav").Should().BeNull();
		state.GetPosition("nan.wav").Should().BeNull();
	}

	/// <summary>Only the most recent positions are kept.</summary>
	[Fact]
	public void SetPosition_KeepsTheMostRecent()
	{
		var state = new MediaPlayerPersistedState();
		for (var i = 0; i < MediaPlayerPersistedState.MaxPositions + 5; i++)
		{
			state.SetPosition($"{i}.wav", TimeSpan.FromSeconds(i + 1));
		}

		state.Positions.Should().HaveCount(MediaPlayerPersistedState.MaxPositions);
		state.GetPosition("0.wav").Should().BeNull();
		state.GetPosition($"{MediaPlayerPersistedState.MaxPositions + 4}.wav").Should().NotBeNull();
	}

	/// <summary>The state survives a round trip through JSON, as local storage keeps it.</summary>
	[Fact]
	public void State_RoundTripsThroughJson()
	{
		var state = new MediaPlayerPersistedState { Rate = 1.5 };
		state.SetPosition("a.wav", TimeSpan.FromSeconds(42));

		var copy = JsonSerializer.Deserialize<MediaPlayerPersistedState>(JsonSerializer.Serialize(state))!;

		copy.Rate.Should().Be(1.5);
		copy.GetPosition("a.wav").Should().Be(TimeSpan.FromSeconds(42));
	}
}
