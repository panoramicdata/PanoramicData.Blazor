using AwesomeAssertions;
using PanoramicData.Blazor.Models;

namespace PanoramicData.Blazor.Test.Models;

/// <summary>Tests for <see cref="SortCriteria"/>.</summary>
public class SortCriteriaTests
{
	/// <summary>The parameterless constructor has no key and no direction.</summary>
	[Fact]
	public void DefaultConstructor_IsUnsorted()
	{
		var criteria = new SortCriteria();

		criteria.Key.Should().BeEmpty();
		criteria.Direction.Should().Be(SortDirection.None);
	}

	/// <summary>The key-only constructor sorts ascending.</summary>
	[Fact]
	public void KeyConstructor_SortsAscending()
	{
		var criteria = new SortCriteria("Name");

		criteria.Key.Should().Be("Name");
		criteria.Direction.Should().Be(SortDirection.Ascending);
	}

	/// <summary>The key and direction constructor uses the given direction.</summary>
	[Fact]
	public void KeyAndDirectionConstructor_UsesDirection()
	{
		var criteria = new SortCriteria("Date", SortDirection.Descending);

		criteria.Key.Should().Be("Date");
		criteria.Direction.Should().Be(SortDirection.Descending);
	}
}
