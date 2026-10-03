using PanoramicData.Blazor.Models;
using Shouldly;

namespace PanoramicData.Blazor.Test;

/// <summary>Tests for the Filter class: parsing every filter type and its edge cases.</summary>
public partial class FilterTests
{
	/// <summary>Verifies that Parse correctly identifies a DoesNotEqual filter from the ! prefix.</summary>
	[Fact]
	public void Parse_DoesNotEqual_ParsesCorrectly()
	{
		var filter = Filter.Parse("status:!Closed");

		filter.Key.ShouldBe("status");
		filter.FilterType.ShouldBe(FilterTypes.DoesNotEqual);
		filter.Value.ShouldBe("Closed");
	}

	/// <summary>Verifies that Parse correctly identifies a StartsWith filter from a trailing wildcard.</summary>
	[Fact]
	public void Parse_StartsWith_ParsesCorrectly()
	{
		var filter = Filter.Parse("name:John*");

		filter.Key.ShouldBe("name");
		filter.FilterType.ShouldBe(FilterTypes.StartsWith);
		filter.Value.ShouldBe("John");
	}

	/// <summary>Verifies that Parse correctly identifies an EndsWith filter from a leading wildcard.</summary>
	[Fact]
	public void Parse_EndsWith_ParsesCorrectly()
	{
		var filter = Filter.Parse("name:*son");

		filter.Key.ShouldBe("name");
		filter.FilterType.ShouldBe(FilterTypes.EndsWith);
		filter.Value.ShouldBe("son");
	}

	/// <summary>Verifies that Parse correctly identifies a Contains filter from surrounding wildcards.</summary>
	[Fact]
	public void Parse_Contains_ParsesCorrectly()
	{
		var filter = Filter.Parse("name:*oh*");

		filter.Key.ShouldBe("name");
		filter.FilterType.ShouldBe(FilterTypes.Contains);
		filter.Value.ShouldBe("oh");
	}

	/// <summary>Verifies that Parse correctly identifies a DoesNotContain filter from the !* prefix and trailing wildcard.</summary>
	[Fact]
	public void Parse_DoesNotContain_ParsesCorrectly()
	{
		var filter = Filter.Parse("name:!*test*");

		filter.Key.ShouldBe("name");
		filter.FilterType.ShouldBe(FilterTypes.DoesNotContain);
		filter.Value.ShouldBe("test");
	}

	/// <summary>Verifies that Parse correctly identifies an In filter from the In() syntax.</summary>
	[Fact]
	public void Parse_In_ParsesCorrectly()
	{
		var filter = Filter.Parse("status:In(A,B,C)");

		filter.Key.ShouldBe("status");
		filter.FilterType.ShouldBe(FilterTypes.In);
		filter.Value.ShouldBe("A,B,C");
	}

	/// <summary>Verifies that Parse correctly identifies a NotIn filter from the !In() syntax.</summary>
	[Fact]
	public void Parse_NotIn_ParsesCorrectly()
	{
		var filter = Filter.Parse("status:!In(A,B)");

		filter.Key.ShouldBe("status");
		filter.FilterType.ShouldBe(FilterTypes.NotIn);
		filter.Value.ShouldBe("A,B");
	}

	/// <summary>Verifies that Parse preserves quotes around multi-word items within an In() filter.</summary>
	[Fact]
	public void Parse_InWithQuotedMultiWordItems_PreservesQuotes()
	{
		var filter = Filter.Parse("name:In(\"Chain Test I\"|\"A - Test Schedule\")");

		filter.Key.ShouldBe("name");
		filter.FilterType.ShouldBe(FilterTypes.In);
		filter.Value.ShouldBe("\"Chain Test I\"|\"A - Test Schedule\"");
	}

	/// <summary>Verifies that Parse preserves quotes around multi-word items within a !In() filter.</summary>
	[Fact]
	public void Parse_NotInWithQuotedMultiWordItems_PreservesQuotes()
	{
		var filter = Filter.Parse("name:!In(\"Chain Test I\"|\"A - Test Schedule\")");

		filter.Key.ShouldBe("name");
		filter.FilterType.ShouldBe(FilterTypes.NotIn);
		filter.Value.ShouldBe("\"Chain Test I\"|\"A - Test Schedule\"");
	}

	/// <summary>Verifies that Parse correctly identifies a GreaterThan filter from the > prefix.</summary>
	[Fact]
	public void Parse_GreaterThan_ParsesCorrectly()
	{
		var filter = Filter.Parse("price:>100");

		filter.Key.ShouldBe("price");
		filter.FilterType.ShouldBe(FilterTypes.GreaterThan);
		filter.Value.ShouldBe("100");
	}

	/// <summary>Verifies that Parse correctly identifies a GreaterThanOrEqual filter from the >= prefix.</summary>
	[Fact]
	public void Parse_GreaterThanOrEqual_ParsesCorrectly()
	{
		var filter = Filter.Parse("price:>=100");

		filter.Key.ShouldBe("price");
		filter.FilterType.ShouldBe(FilterTypes.GreaterThanOrEqual);
		filter.Value.ShouldBe("100");
	}

	/// <summary>Verifies that Parse correctly identifies a LessThan filter from the &lt; prefix.</summary>
	[Fact]
	public void Parse_LessThan_ParsesCorrectly()
	{
		var filter = Filter.Parse("price:<50");

		filter.Key.ShouldBe("price");
		filter.FilterType.ShouldBe(FilterTypes.LessThan);
		filter.Value.ShouldBe("50");
	}

	/// <summary>Verifies that Parse correctly identifies a LessThanOrEqual filter from the &lt;= prefix.</summary>
	[Fact]
	public void Parse_LessThanOrEqual_ParsesCorrectly()
	{
		var filter = Filter.Parse("price:<=50");

		filter.Key.ShouldBe("price");
		filter.FilterType.ShouldBe(FilterTypes.LessThanOrEqual);
		filter.Value.ShouldBe("50");
	}

	/// <summary>Verifies that Parse correctly identifies a Range filter, populating both Value and Value2.</summary>
	[Fact]
	public void Parse_Range_ParsesCorrectly()
	{
		var filter = Filter.Parse("price:>10|100<");

		filter.Key.ShouldBe("price");
		filter.FilterType.ShouldBe(FilterTypes.Range);
		filter.Value.ShouldBe("10");
		filter.Value2.ShouldBe("100");
	}

	/// <summary>Verifies that Parse correctly identifies an IsNull filter from the (null) syntax.</summary>
	[Fact]
	public void Parse_IsNull_ParsesCorrectly()
	{
		var filter = Filter.Parse("status:(null)");

		filter.Key.ShouldBe("status");
		filter.FilterType.ShouldBe(FilterTypes.IsNull);
		filter.Value.ShouldBe(string.Empty);
	}

	/// <summary>Verifies that Parse correctly identifies an IsNotNull filter from the !(null) syntax.</summary>
	[Fact]
	public void Parse_IsNotNull_ParsesCorrectly()
	{
		var filter = Filter.Parse("status:!(null)");

		filter.Key.ShouldBe("status");
		filter.FilterType.ShouldBe(FilterTypes.IsNotNull);
		filter.Value.ShouldBe(string.Empty);
	}

	/// <summary>Verifies that Parse correctly identifies an IsEmpty filter from the (empty) syntax.</summary>
	[Fact]
	public void Parse_IsEmpty_ParsesCorrectly()
	{
		var filter = Filter.Parse("status:(empty)");

		filter.Key.ShouldBe("status");
		filter.FilterType.ShouldBe(FilterTypes.IsEmpty);
		filter.Value.ShouldBe(string.Empty);
	}

	/// <summary>Verifies that Parse correctly identifies an IsNotEmpty filter from the !(empty) syntax.</summary>
	[Fact]
	public void Parse_IsNotEmpty_ParsesCorrectly()
	{
		var filter = Filter.Parse("status:!(empty)");

		filter.Key.ShouldBe("status");
		filter.FilterType.ShouldBe(FilterTypes.IsNotEmpty);
		filter.Value.ShouldBe(string.Empty);
	}
}
