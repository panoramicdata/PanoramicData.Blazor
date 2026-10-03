using AwesomeAssertions;
using PanoramicData.Blazor.Models.Monaco;
using System.Reflection;

namespace PanoramicData.Blazor.Test.Models.Monaco;

/// <summary>
/// Tests for adding methods to a <see cref="MethodCache"/>.
/// </summary>
public partial class MethodCacheTests
{
	/// <summary>Methods are cached per language and grouped into overloads by full name.</summary>
	[Fact]
	public void AddMethod_GroupsOverloadsByLanguage()
	{
		var cache = new MethodCache();
		cache.Contains(Lang).Should().BeFalse();

		cache.AddMethod(Lang, Method("Add", typeof(int)));
		cache.AddMethod(Lang, Method("Add", typeof(double)));
		cache.AddMethod("other", Method("Sub", typeof(int)));

		cache.Contains(Lang).Should().BeTrue();
		cache.FindMethod(Lang, "Demo.Funcs.Add").Should().HaveCount(2);
		cache.FindMethod(Lang, "Demo.Funcs.Sub").Should().BeEmpty();
		cache.FindMethod("missing", "Demo.Funcs.Add").Should().BeEmpty();

		cache.Clear();
		cache.Contains(Lang).Should().BeFalse();
	}

	/// <summary>Parameters added without positions are numbered after the first.</summary>
	[Fact]
	public void AddMethodParameters_NumbersPositions()
	{
		var method = Method("F", null);

		MethodCache.AddMethodParameters(method, [Param("a", null), Param("b", null), Param("c", null)]);

		method.Parameters.Select(p => p.Position).Should().Equal(0, 1, 2);
		method.ToString(new MethodCache.MethodCacheOptions { HideDataTypes = true }).Should().Be("F(a, b, c)");
	}

	/// <summary>Reflecting over a type caches each public method with its names, descriptions and parameters.</summary>
	[Fact]
	public void AddTypeMethods_ReflectsMethods()
	{
		var cache = new MethodCache();

		var count = cache.AddTypeMethods(Lang, typeof(Sample), BindingFlags.Public | BindingFlags.Static | BindingFlags.DeclaredOnly);

		count.Should().Be(2);
		var join = cache.FindMethod(Lang, $"{typeof(Sample).Namespace}.Sample.Joined").Should().ContainSingle().Subject;
		join.Description.Should().Be("Joins strings");
		join.ReturnType.Should().Be<string>();
		join.Parameters.Select(p => p.ToString()).Take(2).Should().Equal("string separator", "[int count]");
		join.Parameters[2].ToString().Should().StartWith("params ").And.EndWith(" parts");
		join.Parameters[0].Description.Should().Be("The separator");
		cache.FindMethod(Lang, $"{typeof(Sample).Namespace}.Sample.Nothing").Should().ContainSingle()
			.Which.Parameters.Should().BeEmpty();
	}

	/// <summary>Without binding flags every public method is reflected, including inherited ones.</summary>
	[Fact]
	public void AddTypeMethods_WithoutFlags_IncludesInherited()
	{
		new MethodCache().AddTypeMethods(Lang, typeof(Sample)).Should().BeGreaterThan(2);
	}

	/// <summary>The public static helper applies the description provider to the methods it adds.</summary>
	[Fact]
	public void AddPublicStaticTypeMethods_AppliesDescriptions()
	{
		var cache = new MethodCache();

		cache.AddPublicStaticTypeMethods("math", typeof(Math), new DefaultDescriptionProvider()).Should().BeGreaterThan(0);

		cache.FindMethod("math", "System.Math.Sqrt").Should().OnlyContain(m => m.Description == "Returns the square root of a specified number.");
	}

	/// <summary>The public static helper adds methods without descriptions when given no provider.</summary>
	[Fact]
	public void AddPublicStaticTypeMethods_WithoutProvider_AddsUndescribedMethods()
	{
		var cache = new MethodCache();

		cache.AddPublicStaticTypeMethods("math", typeof(Math)).Should().BeGreaterThan(0);

		cache.FindMethod("math", "System.Math.Sqrt").Should().NotBeEmpty().And.OnlyContain(m => string.IsNullOrEmpty(m.Description));
	}
}
