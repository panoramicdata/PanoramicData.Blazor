using AwesomeAssertions;
using PanoramicData.Blazor.Models;

namespace PanoramicData.Blazor.Test.Models;

/// <summary>Tests for <see cref="KeyboardInfo"/>.</summary>
public class KeyboardInfoTests
{
	/// <summary>A new instance describes no key and no modifiers.</summary>
	[Fact]
	public void New_IsEmpty()
	{
		var info = new KeyboardInfo();

		info.KeyCode.Should().Be(0);
		info.Code.Should().BeEmpty();
		info.Key.Should().BeEmpty();
		info.AltKey.Should().BeFalse();
		info.CtrlKey.Should().BeFalse();
		info.ShiftKey.Should().BeFalse();
	}

	/// <summary>All members round-trip.</summary>
	[Fact]
	public void SettableMembers_RoundTrip()
	{
		var info = new KeyboardInfo { KeyCode = 65, Code = "KeyA", Key = "a", AltKey = true, CtrlKey = true, ShiftKey = true };

		info.KeyCode.Should().Be(65);
		info.Code.Should().Be("KeyA");
		info.Key.Should().Be("a");
		info.AltKey.Should().BeTrue();
		info.CtrlKey.Should().BeTrue();
		info.ShiftKey.Should().BeTrue();
	}
}
