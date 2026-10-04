using AwesomeAssertions;
using PanoramicData.Blazor.Extensions;
using System;
using System.Linq.Expressions;
using System.Reflection;

namespace PanoramicData.Blazor.Test.Extensions;

/// <summary>
/// Tests for the member-information extensions in <see cref="ReflectionExtensions"/>.
/// </summary>
public partial class ReflectionExtensionsTests
{
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
