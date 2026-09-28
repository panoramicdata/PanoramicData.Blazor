using AwesomeAssertions;
using PanoramicData.Blazor.Extensions;
using System.Linq.Expressions;

namespace PanoramicData.Blazor.Test.Extensions;

/// <summary>Tests for <see cref="ExpressionExtensions"/>.</summary>
public class ExpressionExtensionsTests
{
	/// <summary>A simple property selector yields the property name.</summary>
	[Fact]
	public void GetPropertyName_SimpleMember()
	{
		Expression<Func<Item, object>> expr = x => x.Name;

		expr.GetPropertyName().Should().Be("Name");
	}

	/// <summary>A nested property selector yields the dotted path below the parameter.</summary>
	[Fact]
	public void GetPropertyName_NestedMember()
	{
		Expression<Func<Item, object>> expr = x => x.Child!.Name;

		expr.GetPropertyName().Should().Be("Child.Name");
	}

	/// <summary>A value type selector, which is boxed by a conversion, still yields the property name.</summary>
	[Fact]
	public void GetPropertyName_BoxedValueType()
	{
		Expression<Func<Item, object>> expr = x => x.Age;
		Expression<Func<Item, object>> nested = x => x.Child!.Age;

		expr.GetPropertyName().Should().Be("Age");
		nested.GetPropertyName().Should().Be("Child.Age");
	}

	/// <summary>A conditional selector yields the member on whichever branch is a member access.</summary>
	[Fact]
	public void GetPropertyName_Conditional()
	{
		Expression<Func<Item, object>> whenTrue = x => x.Child == null ? x.Name : "none";
		Expression<Func<Item, object>> whenFalse = x => x.Child == null ? "none" : x.Child.Name;

		whenTrue.GetPropertyName().Should().Be("Name");
		whenFalse.GetPropertyName().Should().Be("Child.Name");
	}

	/// <summary>A selector that is not a member access yields an empty name, as does a null expression.</summary>
	[Fact]
	public void GetPropertyName_NonMember_IsEmpty()
	{
		Expression<Func<Item, object>> constant = x => "literal";

		constant.GetPropertyName().Should().BeEmpty();
		((Expression<Func<Item, object>>)null!).GetPropertyName().Should().BeEmpty();
	}

	/// <summary>Member clauses list each member access from the outermost parameter inwards.</summary>
	[Fact]
	public void MemberClauses_ListsEachLevel()
	{
		Expression<Func<Item, string>> expr = x => x.Child!.Name;

		expr.Body.MemberClauses().Select(m => m.Member.Name).Should().Equal("Child", "Name");
	}

	/// <summary>An expression that is not a member access has no member clauses.</summary>
	[Fact]
	public void MemberClauses_NonMember_IsEmpty()
	{
		Expression.Constant(1).MemberClauses().Should().BeEmpty();
		((Expression?)null).MemberClauses().Should().BeEmpty();
	}

	private sealed class Item
	{
		public string Name { get; set; } = string.Empty;

		public int Age { get; set; }

		public Item? Child { get; set; }
	}
}
