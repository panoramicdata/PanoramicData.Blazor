using AwesomeAssertions;
using PanoramicData.Blazor.Models;

namespace PanoramicData.Blazor.Test.Models;

/// <summary>Tests for <see cref="ElementPosition"/>.</summary>
public class ElementPositionTests
{
	/// <summary>Two positions with the same top and left are equal and share a hash code.</summary>
	[Fact]
	public void Equals_SameCoordinates_IsTrue()
	{
		var a = new ElementPosition { Top = 10.5, Left = 20 };
		var b = new ElementPosition { Top = 10.5, Left = 20 };

		a.Equals(b).Should().BeTrue();
		a.GetHashCode().Should().Be(b.GetHashCode());
	}

	/// <summary>Positions that differ in either coordinate are not equal.</summary>
	[Fact]
	public void Equals_DifferentCoordinates_IsFalse()
	{
		var a = new ElementPosition { Top = 1, Left = 2 };

		a.Equals(new ElementPosition { Top = 1, Left = 3 }).Should().BeFalse();
		a.Equals(new ElementPosition { Top = 0, Left = 2 }).Should().BeFalse();
	}

	/// <summary>A position is not equal to null or to an object of another type.</summary>
	[Fact]
	public void Equals_NullOrOtherType_IsFalse()
	{
		var position = new ElementPosition();
		object? missing = null;
		object text = "0,0";

		position.Equals(missing).Should().BeFalse();
		position.Equals(text).Should().BeFalse();
	}
}
