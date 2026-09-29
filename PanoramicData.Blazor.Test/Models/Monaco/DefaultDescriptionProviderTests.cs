using AwesomeAssertions;
using PanoramicData.Blazor.Models.Monaco;

namespace PanoramicData.Blazor.Test.Models.Monaco;

/// <summary>Tests for <see cref="DefaultDescriptionProvider"/>.</summary>
public class DefaultDescriptionProviderTests
{
	private static string DescribeMathMethod(string name)
	{
		var method = new MethodCache.Method { Namespace = "System", TypeName = "Math", MethodName = name, Description = "unchanged" };
		new DefaultDescriptionProvider().AddDescriptions(method);
		return method.Description;
	}

	/// <summary>Each described System.Math method gets its documented description.</summary>
	[Theory]
	[InlineData("Abs", "Returns the absolute value of a number.")]
	[InlineData("Acos", "Returns the angle whose cosine is the specified number.")]
	[InlineData("Acosh", "Returns the angle whose hyperbolic cosine is the specified number.")]
	[InlineData("Asin", "Returns the angle whose sine is the specified number.")]
	[InlineData("Asinh", "Returns the angle whose hyperbolic sine is the specified number.")]
	[InlineData("Atan", "Returns the angle whose tangent is the specified number.")]
	[InlineData("Atan2", "Returns the angle whose tangent is the quotient of two specified numbers.")]
	[InlineData("Atanh", "Returns the angle whose hyperbolic tangent is the specified number.")]
	[InlineData("BigMul", "Produces the full product of two 32-bit numbers.")]
	[InlineData("BitDecrement", "Returns the largest value that compares less than a specified value.")]
	[InlineData("BitIncrement", "Returns the smallest value that compares greater than a specified value.")]
	[InlineData("Cbrt", "Returns the cube root of a specified number.")]
	[InlineData("Ceiling", "Returns the smallest integral value that is greater than or equal to the specified decimal number.")]
	[InlineData("Clamp", "Returns value clamped to the inclusive range of min and max.")]
	[InlineData("CopySign", "Returns a value with the magnitude of x and the sign of y.")]
	[InlineData("Cos", "Returns the cosine of the specified angle.")]
	[InlineData("Cosh", "Returns the hyperbolic cosine of the specified angle.")]
	[InlineData("DivRem", "Produces the quotient and the remainder of two unsigned 8-bit numbers.")]
	[InlineData("Exp", "Returns e raised to the specified power.")]
	[InlineData("Floor", "Returns the largest integral value less than or equal to the specified decimal number.")]
	[InlineData("FusedMultiplyAdd", "Returns (x * y) + z, rounded as one ternary operation.")]
	[InlineData("IEEERemainder", "Returns the remainder resulting from the division of a specified number by another specified number.")]
	[InlineData("ILogB", "Returns the base 2 integer logarithm of a specified number.")]
	[InlineData("Log", "Returns the natural (base e) logarithm of a specified number.")]
	[InlineData("Log10", "Returns the base 10 logarithm of a specified number.")]
	[InlineData("Log2", "Returns the base 2 logarithm of a specified number.")]
	[InlineData("Max", "Returns the larger of two numbers.")]
	[InlineData("MaxMagnitude", "Returns the larger magnitude of two double-precision floating-point numbers.")]
	[InlineData("Min", "Returns the smaller of two numbers.")]
	[InlineData("MinMagnitude", "Returns the smaller magnitude of two double-precision floating-point numbers.")]
	[InlineData("Pow", "Returns a specified number raised to the specified power.")]
	[InlineData("ReciprocalEstimate", "Returns an estimate of the reciprocal of a specified number.")]
	[InlineData("ReciprocalSqrtEstimate", "Returns an estimate of the reciprocal square root of a specified number.")]
	[InlineData("Round", "Rounds a decimal value to the nearest integral value, and rounds midpoint values to the nearest even number.")]
	[InlineData("ScaleB", "Returns x * 2^n computed efficiently.")]
	[InlineData("Sign", "Returns an integer that indicates the sign of a number.")]
	[InlineData("Sin", "Returns the sine of the specified angle.")]
	[InlineData("SinCos", "Returns the sine and cosine of the specified angle.")]
	[InlineData("Sinh", "Returns the hyperbolic sine of the specified angle.")]
	[InlineData("Sqrt", "Returns the square root of a specified number.")]
	[InlineData("Tan", "Returns the tangent of the specified angle.")]
	[InlineData("Tanh", "Returns the hyperbolic tangent of the specified angle.")]
	public void AddDescriptions_DescribesMathMethods(string name, string expected)
	{
		DescribeMathMethod(name).Should().Be(expected);
	}

	/// <summary>A method the provider does not know keeps its existing description.</summary>
	[Fact]
	public void AddDescriptions_UnknownMethod_KeepsDescription()
	{
		DescribeMathMethod("Nonexistent").Should().Be("unchanged");
	}
}
