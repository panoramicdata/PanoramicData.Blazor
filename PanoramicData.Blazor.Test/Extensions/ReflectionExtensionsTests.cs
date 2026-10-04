using AwesomeAssertions;
using PanoramicData.Blazor.Extensions;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.Reflection;

namespace PanoramicData.Blazor.Test.Extensions;

/// <summary>Tests for <see cref="ReflectionExtensions"/>.</summary>
public partial class ReflectionExtensionsTests
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

	private static class Functions
	{
		[Display(Name = "Shown", Description = "Display description")]
		public static int WithDisplay([Display(Description = "First value")] int a, [Description("Second value")] int b) => a + b;

		[Description("Plain description")]
		[DisplayName("Named")]
		public static int WithDescription([Display(Name = "Sea")] int c) => c;

		public static int Bare(int value) => value;
	}
}
