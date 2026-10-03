using PanoramicData.Blazor.Models;
using Shouldly;

namespace PanoramicData.Blazor.Test;

/// <summary>
/// UpdateFrom tests for <see cref="Filter"/>.
/// </summary>
public partial class FilterTests
{
	/// <summary>Verifies that UpdateFrom parses filter text and updates properties when the key matches.</summary>
	[Fact]
	public void UpdateFrom_MatchingKey_UpdatesFilterProperties()
	{
		var filter = new Filter { Key = "status" };

		filter.UpdateFrom("status:!Open price:>10");

		filter.FilterType.ShouldBe(FilterTypes.DoesNotEqual);
		filter.Value.ShouldBe("Open");
	}

	/// <summary>Verifies that UpdateFrom clears the filter when no matching key is found in the filter text.</summary>
	[Fact]
	public void UpdateFrom_NoMatchingKey_ClearsFilter()
	{
		var filter = new Filter(FilterTypes.GreaterThan, "status", "Active");

		filter.UpdateFrom("price:>10");

		filter.FilterType.ShouldBe(FilterTypes.Equals);
		filter.Value.ShouldBe(string.Empty);
	}

	/// <summary>Verifies that UpdateFrom clears the filter when given an empty string.</summary>
	[Fact]
	public void UpdateFrom_EmptyText_ClearsFilter()
	{
		var filter = new Filter(FilterTypes.Contains, "name", "test");

		filter.UpdateFrom("");

		filter.FilterType.ShouldBe(FilterTypes.Equals);
		filter.Value.ShouldBe(string.Empty);
	}

	/// <summary>Verifies that UpdateFrom matches filter keys case-insensitively.</summary>
	[Fact]
	public void UpdateFrom_CaseInsensitiveKeyMatch_UpdatesFilter()
	{
		var filter = new Filter { Key = "Status" };

		filter.UpdateFrom("status:Open");

		filter.FilterType.ShouldBe(FilterTypes.Equals);
		filter.Value.ShouldBe("Open");
	}

	/// <summary>Verifies that UpdateFrom clears the filter when given a null string.</summary>
	[Fact]
	public void UpdateFrom_NullText_ClearsFilter()
	{
		var filter = new Filter(FilterTypes.Contains, "name", "test");

		filter.UpdateFrom(null!);

		filter.FilterType.ShouldBe(FilterTypes.Equals);
		filter.Value.ShouldBe(string.Empty);
	}

	/// <summary>Verifies that UpdateFrom clears the filter when given a whitespace-only string.</summary>
	[Fact]
	public void UpdateFrom_WhitespaceText_ClearsFilter()
	{
		var filter = new Filter(FilterTypes.Contains, "name", "test");

		filter.UpdateFrom("   ");

		filter.FilterType.ShouldBe(FilterTypes.Equals);
		filter.Value.ShouldBe(string.Empty);
	}

	/// <summary>Verifies that UpdateFrom correctly round-trips a quoted multi-word value, preserving the unquoted value.</summary>
	[Fact]
	public void UpdateFrom_MultiWordValue_PreservesValue()
	{
		// Regression: UpdateFrom must correctly round-trip quoted multi-word values from search text
		var filter = new Filter { Key = "name" };

		filter.UpdateFrom("name:\"On Microsoft Schedule\"");

		filter.FilterType.ShouldBe(FilterTypes.Equals);
		filter.Value.ShouldBe("On Microsoft Schedule");
	}

	/// <summary>Verifies that UpdateFrom correctly handles a DoesNotEqual filter with a quoted multi-word value.</summary>
	[Fact]
	public void UpdateFrom_MultiWordDoesNotEqualValue_PreservesValue()
	{
		var filter = new Filter { Key = "name" };

		filter.UpdateFrom("name:!\"On Microsoft Schedule\"");

		filter.FilterType.ShouldBe(FilterTypes.DoesNotEqual);
		filter.Value.ShouldBe("On Microsoft Schedule");
	}
}
