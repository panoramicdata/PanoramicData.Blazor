using AwesomeAssertions;
using PanoramicData.Blazor.Enums;
using PanoramicData.Blazor.Models;

namespace PanoramicData.Blazor.Test.Models;

/// <summary>Tests for <see cref="ListenerConfiguration"/>.</summary>
public class ListenerConfigurationTests
{
	/// <summary>A new configuration uses manual activation, a three second keyword timeout and no tokens.</summary>
	[Fact]
	public void New_HasDocumentedDefaults()
	{
		var config = new ListenerConfiguration();

		config.Mode.Should().Be(ListenerMode.ManualActivation);
		config.Keyword.Should().BeEmpty();
		config.KeywordSilenceTimeout.Should().Be(TimeSpan.FromSeconds(3));
		config.KeywordTimeoutToken.Should().BeNull();
		config.ManualStartToken.Should().BeNull();
		config.ManualStopToken.Should().BeNull();
	}

	/// <summary>All members round-trip.</summary>
	[Fact]
	public void SettableMembers_RoundTrip()
	{
		var config = new ListenerConfiguration
		{
			Mode = ListenerMode.KeywordActivation,
			Keyword = "merlin",
			KeywordSilenceTimeout = TimeSpan.FromSeconds(10),
			KeywordTimeoutToken = "[timeout]",
			ManualStartToken = "[start]",
			ManualStopToken = "[stop]"
		};

		config.Mode.Should().Be(ListenerMode.KeywordActivation);
		config.Keyword.Should().Be("merlin");
		config.KeywordSilenceTimeout.Should().Be(TimeSpan.FromSeconds(10));
		config.KeywordTimeoutToken.Should().Be("[timeout]");
		config.ManualStartToken.Should().Be("[start]");
		config.ManualStopToken.Should().Be("[stop]");
	}
}
