using AwesomeAssertions;
using PanoramicData.Blazor.Attributes;

namespace PanoramicData.Blazor.Test.Attributes;

/// <summary>Tests for <see cref="FilterKeyAttribute"/>.</summary>
public class FilterKeyAttributeTests
{
	/// <summary>The constructor captures the key, which remains settable.</summary>
	[Fact]
	public void Constructor_CapturesValue()
	{
		var attribute = new FilterKeyAttribute("abc") { Value = "xyz" };

		attribute.Value.Should().Be("xyz");
	}

	/// <summary>A property with the attribute is filtered by the attribute's key, exactly as declared.</summary>
	[Fact]
	public void Get_ReturnsDeclaredKey()
	{
		var property = typeof(Item).GetProperty(nameof(Item.DateCreated))!;

		FilterKeyAttribute.Get(property).Should().Be("Created");
	}

	/// <summary>A property without the attribute falls back to its name with the first letter lower-cased.</summary>
	[Fact]
	public void Get_FallsBackToCamelCasedPropertyName()
	{
		var property = typeof(Item).GetProperty(nameof(Item.FirstName))!;

		FilterKeyAttribute.Get(property).Should().Be("firstName");
	}

	private sealed class Item
	{
		[FilterKey("Created")]
		public DateTime DateCreated { get; set; }

		public string FirstName { get; set; } = string.Empty;
	}
}
