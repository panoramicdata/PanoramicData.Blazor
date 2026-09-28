using AwesomeAssertions;
using PanoramicData.Blazor.Extensions;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;

namespace PanoramicData.Blazor.Test.Extensions;

/// <summary>Tests for <see cref="EnumExtensions"/>.</summary>
public class EnumExtensionsTests
{
	/// <summary>The display name and description come from the Display attribute.</summary>
	[Fact]
	public void DisplayAttribute_SuppliesNameAndDescription()
	{
		Colour.DarkRed.GetEnumDisplayName().Should().Be("Dark red");
		Colour.DarkRed.GetEnumDisplayDescription().Should().Be("A deep red");
	}

	/// <summary>The description comes from the Description attribute.</summary>
	[Fact]
	public void DescriptionAttribute_SuppliesDescription()
	{
		Colour.Blue.GetEnumDescription().Should().Be("Sky blue");
	}

	/// <summary>Each lookup returns null when its attribute is absent.</summary>
	[Fact]
	public void MissingAttributes_ReturnNull()
	{
		Colour.Green.GetEnumDisplayName().Should().BeNull();
		Colour.Green.GetEnumDisplayDescription().Should().BeNull();
		Colour.Green.GetEnumDescription().Should().BeNull();
		Colour.Blue.GetEnumDisplayName().Should().BeNull();
		Colour.DarkRed.GetEnumDescription().Should().BeNull();
	}

	/// <summary>A value with no named member, such as an undefined number, returns null rather than throwing.</summary>
	[Fact]
	public void UndefinedValue_ReturnsNull()
	{
		((Colour)99).GetEnumDisplayName().Should().BeNull();
		((Colour)99).GetEnumDescription().Should().BeNull();
	}

	private enum Colour
	{
		[Display(Name = "Dark red", Description = "A deep red")]
		DarkRed,

		Green,

		[Description("Sky blue")]
		Blue
	}
}
