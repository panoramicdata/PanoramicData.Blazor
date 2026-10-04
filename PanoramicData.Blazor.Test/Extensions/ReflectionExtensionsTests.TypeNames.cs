using AwesomeAssertions;
using PanoramicData.Blazor.Extensions;
using System;

namespace PanoramicData.Blazor.Test.Extensions;

/// <summary>
/// Tests for the type-name extensions in <see cref="ReflectionExtensions"/>.
/// </summary>
public partial class ReflectionExtensionsTests
{
	/// <summary>Nullable types unwrap to their underlying type; other types are returned unchanged.</summary>
	[Fact]
	public void GetNonNullableType_UnwrapsNullable()
	{
		typeof(int?).GetNonNullableType().Should().Be<int>();
		typeof(string).GetNonNullableType().Should().Be<string>();
	}

	/// <summary>Primitive types map to their C# keywords.</summary>
	[Theory]
	[InlineData(typeof(int), "int")]
	[InlineData(typeof(short), "short")]
	[InlineData(typeof(long), "long")]
	[InlineData(typeof(uint), "uint")]
	[InlineData(typeof(ushort), "ushort")]
	[InlineData(typeof(ulong), "ulong")]
	[InlineData(typeof(bool), "bool")]
	[InlineData(typeof(string), "string")]
	[InlineData(typeof(decimal), "decimal")]
	[InlineData(typeof(float), "float")]
	[InlineData(typeof(double), "double")]
	[InlineData(typeof(byte), "byte")]
	[InlineData(typeof(sbyte), "sbyte")]
	[InlineData(typeof(object), "object")]
	[InlineData(typeof(object[]), "object[]")]
	[InlineData(typeof(DateTime), "DateTime")]
	public void GetFriendlyTypeName_MapsKeywords(Type type, string expected)
	{
		type.GetFriendlyTypeName().Should().Be(expected);
	}

	/// <summary>Generic types are named with their friendly type arguments, recursively.</summary>
	[Fact]
	public void GetFriendlyTypeName_FormatsGenerics()
	{
		typeof(List<int>).GetFriendlyTypeName().Should().Be("List<int>");
		typeof(Dictionary<string, List<double?>>).GetFriendlyTypeName().Should().Be("Dictionary<string, List<Nullable<double>>>");
	}

	/// <summary>Arrays are named from their friendly element type, with their rank (#199).</summary>
	[Fact]
	public void GetFriendlyTypeName_FormatsArrays()
	{
		typeof(string[]).GetFriendlyTypeName().Should().Be("string[]");
		typeof(int[,]).GetFriendlyTypeName().Should().Be("int[,]");
		typeof(double[][]).GetFriendlyTypeName().Should().Be("double[][]");
		typeof(List<int>[]).GetFriendlyTypeName().Should().Be("List<int>[]");
		typeof(DateTime[]).GetFriendlyTypeName().Should().Be("DateTime[]");
	}
}
