using AwesomeAssertions;
using PanoramicData.Blazor.Models;

namespace PanoramicData.Blazor.Test.Models;

/// <summary>Tests for <see cref="OptionInfo"/>.</summary>
public class OptionInfoTests
{
	/// <summary>A new option has no text or value and is neither selected nor disabled.</summary>
	[Fact]
	public void New_IsEmpty()
	{
		var option = new OptionInfo();

		option.Text.Should().BeEmpty();
		option.Value.Should().BeNull();
		option.IsSelected.Should().BeFalse();
		option.IsDisabled.Should().BeFalse();
	}

	/// <summary>All members round-trip.</summary>
	[Fact]
	public void SettableMembers_RoundTrip()
	{
		var option = new OptionInfo { Text = "Red", Value = 1, IsSelected = true, IsDisabled = true };

		option.Text.Should().Be("Red");
		option.Value.Should().Be(1);
		option.IsSelected.Should().BeTrue();
		option.IsDisabled.Should().BeTrue();
	}
}
