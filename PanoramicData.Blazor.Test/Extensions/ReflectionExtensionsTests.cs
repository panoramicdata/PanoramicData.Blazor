using AwesomeAssertions;
using PanoramicData.Blazor.Extensions;
using System;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.Linq.Expressions;
using System.Reflection;

namespace PanoramicData.Blazor.Test.Extensions;

/// <summary>Tests for <see cref="ReflectionExtensions"/>.</summary>
public class ReflectionExtensionsTests
{
	private static MethodInfo Method(string name) => typeof(Functions).GetMethod(name)!;

	private static ParameterInfo Parameter(string method, int index) => Method(method).GetParameters()[index];

	/// <summary>A method description prefers the Display attribute, then the Description attribute, else empty.</summary>
	[Fact]
	public void GetDescription_Method_PrefersDisplayThenDescription()
	{
		Method(nameof(Functions.WithDisplay)).GetDescription().Should().Be("Display description");
		Method(nameof(Functions.WithDescription)).GetDescription().Should().Be("Plain description");
		Method(nameof(Functions.Bare)).GetDescription().Should().BeEmpty();
	}

	/// <summary>A parameter description prefers the Display attribute, then the Description attribute, else empty.</summary>
	[Fact]
	public void GetDescription_Parameter_PrefersDisplayThenDescription()
	{
		Parameter(nameof(Functions.WithDisplay), 0).GetDescription().Should().Be("First value");
		Parameter(nameof(Functions.WithDisplay), 1).GetDescription().Should().Be("Second value");
		Parameter(nameof(Functions.Bare), 0).GetDescription().Should().BeEmpty();
	}

	/// <summary>A method name prefers the Display attribute, then DisplayName, else the declared name.</summary>
	[Fact]
	public void GetName_Method_PrefersDisplayThenDisplayName()
	{
		Method(nameof(Functions.WithDisplay)).GetName().Should().Be("Shown");
		Method(nameof(Functions.WithDescription)).GetName().Should().Be("Named");
		Method(nameof(Functions.Bare)).GetName().Should().Be("Bare");
	}

	/// <summary>A parameter name comes from the Display attribute, else the declared name.</summary>
	[Fact]
	public void GetName_Parameter_PrefersDisplayThenDisplayName()
	{
		Parameter(nameof(Functions.WithDisplay), 0).GetName().Should().Be("a");
		Parameter(nameof(Functions.WithDisplay), 1).GetName().Should().Be("b");
		Parameter(nameof(Functions.WithDescription), 0).GetName().Should().Be("Sea");
		Parameter(nameof(Functions.Bare), 0).GetName().Should().Be("value");
	}

	/// <summary>The member info is found for plain, boxed and conditional member selectors.</summary>
	[Fact]
	public void GetPropertyMemberInfo_FindsMember()
	{
		Expression<Func<Item, object>> plain = x => x.Name;
		Expression<Func<Item, object>> boxed = x => x.Age;
		Expression<Func<Item, object>> whenTrue = x => x.Age > 0 ? x.Name : "none";
		Expression<Func<Item, object>> whenFalse = x => x.Age > 0 ? "none" : x.Name;

		plain.GetPropertyMemberInfo()!.Name.Should().Be("Name");
		boxed.GetPropertyMemberInfo()!.Name.Should().Be("Age");
		whenTrue.GetPropertyMemberInfo()!.Name.Should().Be("Name");
		whenFalse.GetPropertyMemberInfo()!.Name.Should().Be("Name");
		((Expression<Func<Item, object>>)null!).GetPropertyMemberInfo().Should().BeNull();
	}

	/// <summary>A boxed value that is not a member access yields no member.</summary>
	[Fact]
	public void GetPropertyMemberInfo_BoxedNonMember_IsNull()
	{
		Expression<Func<Item, object>> computed = x => x.Age + 1;

		computed.GetPropertyMemberInfo().Should().BeNull();
	}

	/// <summary>A selector that is neither a member access nor a boxed one yields no member rather than throwing (#171).</summary>
	[Fact]
	public void GetPropertyMemberInfo_UnboxedNonMember_IsNull()
	{
		Expression<Func<Item, object>> literal = x => "literal";
		Expression<Func<Item, object>> concatenated = x => x.Name + "!";
		Expression<Func<Item, object>> called = x => x.Name.Trim();
		Expression<Func<Item, object>> conditional = x => x.Age > 0 ? "a" : "b";

		literal.GetPropertyMemberInfo().Should().BeNull();
		concatenated.GetPropertyMemberInfo().Should().BeNull();
		called.GetPropertyMemberInfo().Should().BeNull();
		conditional.GetPropertyMemberInfo().Should().BeNull();
	}

	/// <summary>The underlying type is the type of a field, property or event.</summary>
	[Fact]
	public void GetMemberUnderlyingType_ForFieldPropertyAndEvent()
	{
		typeof(Item).GetField(nameof(Item.Counter))!.GetMemberUnderlyingType().Should().Be<long>();
		typeof(Item).GetProperty(nameof(Item.Name))!.GetMemberUnderlyingType().Should().Be<string>();
		typeof(Item).GetEvent(nameof(Item.Changed))!.GetMemberUnderlyingType().Should().Be<EventHandler>();
	}

	/// <summary>An event without a handler type has no underlying type.</summary>
	[Fact]
	public void GetMemberUnderlyingType_EventWithoutHandlerType_Throws()
	{
		var act = () => new HandlerlessEvent().GetMemberUnderlyingType();

		act.Should().Throw<ArgumentException>().WithParameterName("member").WithMessage("*EventHandlerType is null*");
	}

	/// <summary>Any other kind of member is rejected.</summary>
	[Fact]
	public void GetMemberUnderlyingType_Method_Throws()
	{
		var act = () => Method(nameof(Functions.Bare)).GetMemberUnderlyingType();

		act.Should().Throw<ArgumentException>().WithParameterName("member");
	}

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

	private static class Functions
	{
		[Display(Name = "Shown", Description = "Display description")]
		public static int WithDisplay([Display(Description = "First value")] int a, [Description("Second value")] int b) => a + b;

		[Description("Plain description")]
		[DisplayName("Named")]
		public static int WithDescription([Display(Name = "Sea")] int c) => c;

		public static int Bare(int value) => value;
	}

	private sealed class Item
	{
		public long Counter = 1;

		public string Name { get; set; } = string.Empty;

		public int Age { get; set; }

		public event EventHandler? Changed;

		public void RaiseChanged() => Changed?.Invoke(this, EventArgs.Empty);
	}

	/// <summary>An event description that, unlike any compiled event, has no handler type.</summary>
	private sealed class HandlerlessEvent : EventInfo
	{
		public override EventAttributes Attributes => EventAttributes.None;

		public override Type? DeclaringType => typeof(HandlerlessEvent);

		public override Type? EventHandlerType => null;

		public override string Name => "Handlerless";

		public override Type? ReflectedType => typeof(HandlerlessEvent);

		public override MethodInfo? GetAddMethod(bool nonPublic) => null;

		public override object[] GetCustomAttributes(bool inherit) => [];

		public override object[] GetCustomAttributes(Type attributeType, bool inherit) => [];

		public override MethodInfo? GetRaiseMethod(bool nonPublic) => null;

		public override MethodInfo? GetRemoveMethod(bool nonPublic) => null;

		public override bool IsDefined(Type attributeType, bool inherit) => false;
	}
}
