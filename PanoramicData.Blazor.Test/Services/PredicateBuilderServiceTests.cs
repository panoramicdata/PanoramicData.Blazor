using AwesomeAssertions;
using PanoramicData.Blazor.Services;
using System.Linq.Expressions;

namespace PanoramicData.Blazor.Test.Services;

/// <summary>Tests for <see cref="PredicateBuilderService"/>.</summary>
public class PredicateBuilderServiceTests
{
	private static readonly int[] _numbers = [1, 2, 3, 4, 5, 6];

	private static int[] Apply(Expression<Func<int, bool>> predicate) => [.. _numbers.AsQueryable().Where(predicate)];

	/// <summary>True matches everything and False matches nothing.</summary>
	[Fact]
	public void TrueAndFalse_MatchAllOrNothing()
	{
		Apply(PredicateBuilderService.True<int>()).Should().Equal(_numbers);
		Apply(PredicateBuilderService.False<int>()).Should().BeEmpty();
	}

	/// <summary>Create returns the predicate unchanged.</summary>
	[Fact]
	public void Create_ReturnsPredicate()
	{
		Expression<Func<int, bool>> even = x => x % 2 == 0;

		PredicateBuilderService.Create(even).Should().BeSameAs(even);
	}

	/// <summary>And requires both predicates, even when they use differently named parameters.</summary>
	[Fact]
	public void And_RequiresBoth()
	{
		Expression<Func<int, bool>> even = x => x % 2 == 0;
		Expression<Func<int, bool>> big = y => y > 3;

		Apply(even.And(big)).Should().Equal(4, 6);
	}

	/// <summary>Or requires either predicate.</summary>
	[Fact]
	public void Or_RequiresEither()
	{
		Expression<Func<int, bool>> one = x => x == 1;
		Expression<Func<int, bool>> six = y => y == 6;

		Apply(one.Or(six)).Should().Equal(1, 6);
	}

	/// <summary>Not negates the predicate.</summary>
	[Fact]
	public void Not_Negates()
	{
		Expression<Func<int, bool>> even = x => x % 2 == 0;

		Apply(even.Not()).Should().Equal(1, 3, 5);
	}

	/// <summary>Combined predicates compile to delegates that behave the same as the query.</summary>
	[Fact]
	public void Combined_CompilesToDelegate()
	{
		var predicate = PredicateBuilderService.True<int>().And(x => x > 2).Or(x => x == 1).Compile();

		predicate(1).Should().BeTrue();
		predicate(2).Should().BeFalse();
		predicate(3).Should().BeTrue();
	}
}
