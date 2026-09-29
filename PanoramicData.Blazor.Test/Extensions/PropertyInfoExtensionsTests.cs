using AwesomeAssertions;
using PanoramicData.Blazor.Attributes;
using PanoramicData.Blazor.Extensions;
using System.ComponentModel.DataAnnotations;

namespace PanoramicData.Blazor.Test.Extensions;

/// <summary>Tests for <see cref="PropertyInfoExtensions"/>.</summary>
public class PropertyInfoExtensionsTests
{
	/// <summary>The display short name is taken from the Display attribute with its first letter lower-cased.</summary>
	[Fact]
	public void GetDisplayShortName_LowersFirstChar()
	{
		typeof(Item).GetProperty(nameof(Item.DateCreated))!.GetDisplayShortName().Should().Be("created");
	}

	/// <summary>The filter key is taken from the FilterKey attribute with its first letter lower-cased.</summary>
	[Fact]
	public void GetFilterKey_LowersFirstChar()
	{
		typeof(Item).GetProperty(nameof(Item.DateCreated))!.GetFilterKey().Should().Be("createdOn");
	}

	/// <summary>Without the attributes, both lookups return null.</summary>
	[Fact]
	public void MissingAttributes_ReturnNull()
	{
		var property = typeof(Item).GetProperty(nameof(Item.Name))!;

		property.GetDisplayShortName().Should().BeNull();
		property.GetFilterKey().Should().BeNull();
	}

	/// <summary>A Display attribute without a short name gives no short name.</summary>
	[Fact]
	public void DisplayWithoutShortName_ReturnsNull()
	{
		typeof(Item).GetProperty(nameof(Item.Age))!.GetDisplayShortName().Should().BeNull();
	}

	private sealed class Item
	{
		[Display(ShortName = "Created")]
		[FilterKey("CreatedOn")]
		public DateTime DateCreated { get; set; }

		public string Name { get; set; } = string.Empty;

		[Display(Name = "Age in years")]
		public int Age { get; set; }
	}
}
