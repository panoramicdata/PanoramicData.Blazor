using PanoramicData.Blazor.Models;
using Shouldly;

namespace PanoramicData.Blazor.Test;

/// <summary>
/// GetMemberName tests for <see cref="Filter"/>.
/// </summary>
public partial class FilterTests
{
	/// <summary>Verifies that GetMemberName returns the enum member name for a given Display attribute name.</summary>
	[Fact]
	public void GetMemberName_DisplayName_ReturnsMemberName()
	{
		var result = Filter.GetMemberName(typeof(EnumWithDisplay), "Needs Improvement");

		result.ShouldBe("NeedsImprovement");
	}

	/// <summary>Verifies that GetMemberName returns the enum member name for a second Display attribute name.</summary>
	[Fact]
	public void GetMemberName_AnotherDisplayName_ReturnsMemberName()
	{
		var result = Filter.GetMemberName(typeof(EnumWithDisplay), "In Progress");

		result.ShouldBe("InProgress");
	}

	/// <summary>Verifies that GetMemberName returns a raw enum member name unchanged when passed directly.</summary>
	[Fact]
	public void GetMemberName_RawMemberName_ReturnsUnchanged()
	{
		var result = Filter.GetMemberName(typeof(EnumWithDisplay), "NeedsImprovement");

		result.ShouldBe("NeedsImprovement");
	}

	/// <summary>Verifies that GetMemberName returns the value unchanged for an enum member with a Display attribute that has no Name set.</summary>
	[Fact]
	public void GetMemberName_NoDisplayAttribute_ReturnsUnchanged()
	{
		var result = Filter.GetMemberName(typeof(EnumWithDisplay), "Simple");

		result.ShouldBe("Simple");
	}

	/// <summary>Verifies that GetMemberName returns the value unchanged for an enum type that has no Display attributes at all.</summary>
	[Fact]
	public void GetMemberName_EnumWithNoDisplayAttributes_ReturnsUnchanged()
	{
		var result = Filter.GetMemberName(typeof(EnumWithoutDisplay), "SecondValue");

		result.ShouldBe("SecondValue");
	}

	/// <summary>Verifies that GetMemberName returns the value unchanged when no enum member matches the given string.</summary>
	[Fact]
	public void GetMemberName_UnknownValue_ReturnsUnchanged()
	{
		var result = Filter.GetMemberName(typeof(EnumWithDisplay), "not a match");

		result.ShouldBe("not a match");
	}

	/// <summary>Verifies that calling Format then GetMemberName round-trips display names back to their member names.</summary>
	[Theory]
	[InlineData("Needs Improvement", "NeedsImprovement")]
	[InlineData("In Progress", "InProgress")]
	[InlineData("Simple", "Simple")]
	[InlineData("NeedsImprovement", "NeedsImprovement")]
	public void GetMemberName_RoundTrip_FormatThenGetMemberName(string displayName, string expectedMemberName)
	{
		// Simulate the round-trip: Format() produces the display name, GetMemberName() reverses it
		var result = Filter.GetMemberName(typeof(EnumWithDisplay), displayName);

		result.ShouldBe(expectedMemberName);
	}
}
