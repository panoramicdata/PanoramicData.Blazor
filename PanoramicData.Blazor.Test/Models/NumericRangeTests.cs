using AwesomeAssertions;
using PanoramicData.Blazor.Models;

namespace PanoramicData.Blazor.Test.Models;

/// <summary>Tests for <see cref="NumericRange"/>.</summary>
public class NumericRangeTests
{
	/// <summary>The parameterless constructor gives an empty range at zero.</summary>
	[Fact]
	public void DefaultConstructor_IsZeroToZero()
	{
		var range = new NumericRange();

		range.Start.Should().Be(0);
		range.End.Should().Be(0);
	}

	/// <summary>The two-value constructor sets start and end, which stay settable.</summary>
	[Fact]
	public void ValueConstructor_SetsStartAndEnd()
	{
		var range = new NumericRange(2.5, 7.5);
		range.Start.Should().Be(2.5);
		range.End.Should().Be(7.5);

		range.Start = 1;
		range.End = 9;

		range.Start.Should().Be(1);
		range.End.Should().Be(9);
	}
}
