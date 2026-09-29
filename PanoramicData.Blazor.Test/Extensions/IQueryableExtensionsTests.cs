using AwesomeAssertions;
using PanoramicData.Blazor.Attributes;
using PanoramicData.Blazor.Extensions;
using PanoramicData.Blazor.Models;
using System.ComponentModel.DataAnnotations;

namespace PanoramicData.Blazor.Test.Extensions;

/// <summary>
/// Tests for the property resolution and filter operators of <see cref="IQueryableExtensions"/> that the
/// original enum-focused <c>IQueryableExtensionsTests</c> leave uncovered.
/// </summary>
public class IQueryableExtensionsTests
{
	private static IQueryable<Item> Items() => new List<Item>
	{
		new() { Id = 1, Name = "Alpha", Notes = "first", Score = 10, Owner = new Owner { Name = "Ann" } },
		new() { Id = 2, Name = "Beta", Notes = "", Score = 20, Owner = null },
		new() { Id = 3, Name = "Gamma", Notes = null, Score = 30, Owner = new Owner { Name = "Bob" } }
	}.AsQueryable();

	private static int[] Ids(Filter filter, IDictionary<string, string>? mappings = null)
		=> [.. Items().ApplyFilter(filter, mappings).Select(i => i.Id)];

	/// <summary>A filter with a blank key leaves the query unchanged.</summary>
	[Fact]
	public void BlankKey_ReturnsQueryUnchanged()
	{
		Ids(new Filter(FilterTypes.Equals, " ", "x")).Should().Equal(1, 2, 3);
	}

	/// <summary>An explicit key mapping names the property to filter on.</summary>
	[Fact]
	public void KeyMapping_ResolvesProperty()
	{
		var filter = new Filter(FilterTypes.Equals, "n", "Beta");

		Ids(filter, new Dictionary<string, string> { ["n"] = "Name" }).Should().Equal(2);
		filter.PropertyName.Should().Be("Name");
	}

	/// <summary>A key matching a FilterKey attribute or a Display short name resolves to that property.</summary>
	[Fact]
	public void Attributes_ResolveProperty()
	{
		Ids(new Filter(FilterTypes.Equals, "pts", "20")).Should().Equal(2);
		Ids(new Filter(FilterTypes.Equals, "memo", "first")).Should().Equal(1);
	}

	/// <summary>With no mapping or attribute, the key with its first letter upper-cased is used as the property.</summary>
	[Fact]
	public void UnmatchedKey_FallsBackToUpperFirstChar()
	{
		var filter = new Filter(FilterTypes.Equals, "name", "Gamma");

		Ids(filter).Should().Equal(3);
		filter.PropertyName.Should().Be("Name");
	}

	/// <summary>A key that resolves to no real property leaves the query unchanged rather than throwing.</summary>
	[Fact]
	public void UnknownProperty_ReturnsQueryUnchanged()
	{
		Ids(new Filter(FilterTypes.Equals, "missing", "x")).Should().Equal(1, 2, 3);
	}

	/// <summary>An explicitly blank property name leaves the query unchanged.</summary>
	[Fact]
	public void BlankPropertyName_ReturnsQueryUnchanged()
	{
		Ids(new Filter(FilterTypes.Equals, "Name", "Alpha") { PropertyName = " " }).Should().Equal(1, 2, 3);
	}

	/// <summary>A nested property is filtered with null propagation, so a missing parent does not throw.</summary>
	[Fact]
	public void NestedProperty_UsesNullPropagation()
	{
		Ids(new Filter(FilterTypes.Equals, "owner", "Bob") { PropertyName = "Owner.Name" }).Should().Equal(3);
	}

	/// <summary>Each string operator selects the expected items, skipping null values.</summary>
	[Theory]
	[InlineData(FilterTypes.DoesNotContain, "ph", new[] { 2, 3 })]
	[InlineData(FilterTypes.StartsWith, "G", new[] { 3 })]
	[InlineData(FilterTypes.EndsWith, "ta", new[] { 2 })]
	[InlineData(FilterTypes.DoesNotEqual, "Beta", new[] { 1, 3 })]
	public void StringOperators_FilterItems(FilterTypes type, string value, int[] expected)
	{
		Ids(new Filter(type, "Name", value)).Should().Equal(expected);
	}

	/// <summary>Each comparison operator selects the expected items.</summary>
	[Theory]
	[InlineData(FilterTypes.GreaterThanOrEqual, new[] { 2, 3 })]
	[InlineData(FilterTypes.LessThan, new[] { 1 })]
	[InlineData(FilterTypes.LessThanOrEqual, new[] { 1, 2 })]
	public void ComparisonOperators_FilterItems(FilterTypes type, int[] expected)
	{
		Ids(new Filter(type, "Score", "20")).Should().Equal(expected);
	}

	/// <summary>The null and empty operators distinguish null values from empty strings.</summary>
	[Theory]
	[InlineData(FilterTypes.IsNull, new[] { 3 })]
	[InlineData(FilterTypes.IsNotNull, new[] { 1, 2 })]
	[InlineData(FilterTypes.IsEmpty, new[] { 2 })]
	[InlineData(FilterTypes.IsNotEmpty, new[] { 1, 3 })]
	public void NullAndEmptyOperators_FilterItems(FilterTypes type, int[] expected)
	{
		Ids(new Filter(type, "Notes", string.Empty)).Should().Equal(expected);
	}

	/// <summary>An undefined operator leaves the query unchanged.</summary>
	[Fact]
	public void UndefinedOperator_ReturnsQueryUnchanged()
	{
		Ids(new Filter((FilterTypes)99, "Name", "Alpha")).Should().Equal(1, 2, 3);
	}

	/// <summary>Several filters are applied together, each narrowing the result.</summary>
	[Fact]
	public void ApplyFilters_AppliesAll()
	{
		Filter[] filters = [new Filter(FilterTypes.GreaterThan, "Score", "10"), new Filter(FilterTypes.StartsWith, "Name", "G")];

		Items().ApplyFilters(filters).Select(i => i.Id).Should().Equal(3);
	}

	private sealed class Owner
	{
		public string Name { get; set; } = string.Empty;
	}

	private sealed class Item
	{
		public int Id { get; set; }

		public string Name { get; set; } = string.Empty;

		[Display(ShortName = "memo")]
		public string? Notes { get; set; }

		[FilterKey("pts")]
		public int Score { get; set; }

		public Owner? Owner { get; set; }
	}
}
