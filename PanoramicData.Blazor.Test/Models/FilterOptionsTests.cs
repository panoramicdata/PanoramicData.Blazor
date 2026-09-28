using AwesomeAssertions;
using PanoramicData.Blazor.Models;

namespace PanoramicData.Blazor.Test.Models;

/// <summary>Tests for <see cref="FilterOptions"/>.</summary>
public class FilterOptionsTests
{
	private static IEnumerable<System.Reflection.PropertyInfo> AllowProperties()
		=> typeof(FilterOptions).GetProperties().Where(p => p.PropertyType == typeof(bool) && p.Name.StartsWith("Allow", StringComparison.Ordinal));

	/// <summary>A new set of options allows every filter operation.</summary>
	[Fact]
	public void New_AllowsEverything()
	{
		var options = new FilterOptions();

		AllowProperties().Should().HaveCount(17);
		foreach (var property in AllowProperties())
		{
			property.GetValue(options).Should().Be(true, property.Name);
		}
	}

	/// <summary>The single value preset allows only equality.</summary>
	[Fact]
	public void SingleValue_AllowsOnlyEquals()
	{
		var options = FilterOptions.SingleValue();

		foreach (var property in AllowProperties())
		{
			property.GetValue(options).Should().Be(property.Name == nameof(FilterOptions.AllowEquals), property.Name);
		}
	}

	/// <summary>Each option can be switched off individually.</summary>
	[Fact]
	public void EachOption_CanBeSwitchedOff()
	{
		foreach (var property in AllowProperties())
		{
			var options = new FilterOptions();

			property.SetValue(options, false);

			property.GetValue(options).Should().Be(false, property.Name);
		}
	}
}
