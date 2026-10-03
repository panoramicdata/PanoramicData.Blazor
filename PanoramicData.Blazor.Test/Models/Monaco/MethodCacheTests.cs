using AwesomeAssertions;
using PanoramicData.Blazor.Models.Monaco;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;

namespace PanoramicData.Blazor.Test.Models.Monaco;

/// <summary>Tests for <see cref="MethodCache"/> and its nested method, parameter and option types.</summary>
public partial class MethodCacheTests
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
