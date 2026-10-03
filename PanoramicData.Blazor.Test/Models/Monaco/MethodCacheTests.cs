using AwesomeAssertions;
using BlazorMonaco.Languages;
using PanoramicData.Blazor.Models.Monaco;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.Reflection;

namespace PanoramicData.Blazor.Test.Models.Monaco;

/// <summary>Tests for <see cref="MethodCache"/> and its nested method, parameter and option types.</summary>
public class MethodCacheTests
{
	private const string Lang = "ncalc";

	private static MethodCache.Method Method(string name, Type? returnType, params MethodCache.Parameter[] parameters) => new()
	{
		Namespace = "Demo",
		TypeName = "Funcs",
		MethodName = name,
		ReturnType = returnType,
		Parameters = [.. parameters]
	};

	private static MethodCache.Parameter Param(string name, Type? type, int position = 0) => new() { Name = name, Type = type, Position = position };

	/// <summary>The full name joins namespace, type and method, dropping missing leading parts.</summary>
	[Fact]
	public void Method_Fullname_JoinsParts()
	{
		Method("Add", null).Fullname.Should().Be("Demo.Funcs.Add");
		new MethodCache.Method { MethodName = "Add" }.Fullname.Should().Be("Add");
	}

	/// <summary>A method matches its full name, its type-qualified name or its bare name, ignoring case.</summary>
	[Theory]
	[InlineData("demo.funcs.add", true)]
	[InlineData("Funcs.Add", true)]
	[InlineData("ADD", true)]
	[InlineData("Other.Add", false)]
	public void Method_IsMatch(string name, bool expected)
	{
		Method("Add", null).IsMatch(name).Should().Be(expected);
	}

	/// <summary>A signature shows the return type, name and typed parameters, or "void" when nothing is returned.</summary>
	[Fact]
	public void Method_ToString_ShowsSignature()
	{
		var method = Method("Add", typeof(int), Param("a", typeof(int)), Param("b", typeof(int), 1));

		method.ToString().Should().Be("int Add(int a, int b)");
		Method("Log", null, Param("text", typeof(string))).ToString().Should().Be("void Log(string text)");
	}

	/// <summary>Options can hide data types and prefix the declaring type name.</summary>
	[Fact]
	public void Method_ToString_HonoursOptions()
	{
		var method = Method("Add", typeof(int), Param("a", typeof(int)), Param("b", typeof(int), 1));

		method.ToString(new MethodCache.MethodCacheOptions { HideDataTypes = true }).Should().Be("Add(a, b)");
		method.ToString(new MethodCache.MethodCacheOptions { IncludeMethodTypeName = true, TypeNameFn = t => t.Name }).Should().Be("Int32 Funcs.Add(Int32 a, Int32 b)");
	}

	/// <summary>Optional parameters are bracketed and params arrays are marked; an untyped parameter shows only its name.</summary>
	[Fact]
	public void Parameter_ToString_MarksOptionalAndParams()
	{
		new MethodCache.Parameter { Name = "x", Type = typeof(int), IsOptional = true }.ToString().Should().Be("[int x]");
		new MethodCache.Parameter { Name = "rest", Type = typeof(object[]), IsParams = true }.ToString().Should().Be("params object[] rest");
		new MethodCache.Parameter { Name = "n" }.ToString().Should().Be("n");
		new MethodCache.Parameter { Name = "g", IsGeneric = true }.IsGeneric.Should().BeTrue();
	}

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

	/// <summary>Completion items list each method once, noting overloads and description.</summary>
	[Fact]
	public void GetCompletionItems_ListsMethods()
	{
		var cache = new MethodCache();
		cache.AddMethod(Lang, Method("Add", typeof(int), Param("a", typeof(int))));
		cache.AddMethod(Lang, Method("Add", typeof(double)));
		var described = Method("Sub", typeof(int));
		described.Description = "Subtracts";
		cache.AddMethod(Lang, described);

		var items = cache.GetCompletionItems(Lang, string.Empty).ToList();

		items.Select(i => i.LabelAsString).Should().Equal("Add", "Sub");
		items.Should().OnlyContain(i => i.Kind == CompletionItemKind.Function);
		items[0].DocumentationAsString.Should().StartWith("int Add(int a)").And.Contain("(+1 overloads)");
		items[1].DocumentationAsString.Should().Contain("Subtracts");
		items[1].InsertText.Should().Be("Sub");
	}

	/// <summary>Naming a function adds its first overload's parameters as property completions.</summary>
	[Fact]
	public void GetCompletionItems_ForFunction_AddsParameters()
	{
		var cache = new MethodCache();
		var add = Method("Add", typeof(int), Param("a", typeof(int)), Param("b", typeof(int), 1));
		add.Parameters[0].Description = "left";
		cache.AddMethod(Lang, add);

		var items = cache.GetCompletionItems(Lang, "Demo.Funcs.Add").ToList();

		items.Select(i => i.LabelAsString).Should().Equal("Add", "a", "b");
		items[1].Kind.Should().Be(CompletionItemKind.Property);
		items[1].DocumentationAsString.Should().Be("left");
		cache.GetCompletionItems("missing", string.Empty).Should().BeEmpty();
	}

	/// <summary>Signatures are returned for every overload matching the name, with parameter labels.</summary>
	[Fact]
	public void GetSignatures_ReturnsMatchingOverloads()
	{
		var cache = new MethodCache();
		cache.AddMethod(Lang, Method("Add", typeof(int), Param("a", typeof(int))));
		cache.AddMethod(Lang, Method("Add", typeof(double), Param("x", typeof(double))));
		cache.AddMethod(Lang, Method("Sub", typeof(int)));

		var signatures = cache.GetSignatures(Lang, "add").ToList();

		signatures.Select(s => s.Label).Should().Equal("int Add(int a)", "double Add(double x)");
		signatures[0].Parameters.Should().ContainSingle().Which.Label.Should().Be("int a");
		cache.GetSignatures(Lang, " ").Should().BeEmpty();
		cache.GetSignatures("missing", "Add").Should().BeEmpty();
	}

	/// <summary>The cache's default options show data types without type prefixes.</summary>
	[Fact]
	public void Options_HaveDefaults()
	{
		var options = new MethodCache().Options;

		options.HideDataTypes.Should().BeFalse();
		options.IncludeMethodTypeName.Should().BeFalse();
		options.TypeNameFn(typeof(bool)).Should().Be("bool");
	}

	private static class Sample
	{
		[Display(Name = "Joined")]
		[Description("Joins strings")]
		public static string Join([Description("The separator")] string separator, int count = 0, params string[] parts)
			=> string.Join(separator, parts.Take(count == 0 ? parts.Length : count));

		public static void Nothing()
		{
			// Deliberately empty: this method exists only to be reflected over, as the void-returning
			// counterpart to Join, and doing anything here would add nothing the tests look at.
		}
	}
}
