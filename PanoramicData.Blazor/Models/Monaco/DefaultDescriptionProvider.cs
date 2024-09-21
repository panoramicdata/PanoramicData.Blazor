namespace PanoramicData.Blazor.Models.Monaco;

public class DefaultDescriptionProvider : MethodCache.IDescriptionProvider
{
	public void AddDescriptions(MethodCache.Method method)
	{
		var methodDescription = method.Fullname switch
		{
			"System.Math.Abs" => "Returns the absolute value of a number.",
			"System.Math.Acos" => "Returns the angle whose cosine is the specified number.",
			"System.Math.Acosh" => "Returns the angle whose hyperbolic cosine is the specified number.",
			"System.Math.Asin" => "Returns the angle whose sine is the specified number.",
			"System.Math.Asinh" => "Returns the angle whose hyperbolic sine is the specified number.",
			"System.Math.Atan" => "Returns the angle whose tangent is the specified number.",
			"System.Math.Atan2" => "Returns the angle whose tangent is the quotient of two specified numbers.",
			"System.Math.Atanh" => "Returns the angle whose hyperbolic tangent is the specified number.",
			"System.Math.BigMul" => "Produces the full product of two 32-bit numbers.",
			"System.Math.BitDecrement" => "Returns the largest value that compares less than a specified value.",
			"System.Math.BitIncrement" => "Returns the smallest value that compares greater than a specified value.",
			"System.Math.Cbrt" => "Returns the cube root of a specified number.",
			"System.Math.Ceiling" => "Returns the smallest integral value that is greater than or equal to the specified decimal number.",
			"System.Math.Clamp" => "Returns value clamped to the inclusive range of min and max.",
			"System.Math.CopySign" => "Returns a value with the magnitude of x and the sign of y.",
			"System.Math.Cos" => "Returns the cosine of the specified angle.",
			"System.Math.Cosh" => "Returns the hyperbolic cosine of the specified angle.",
			"System.Math.DivRem" => "Produces the quotient and the remainder of two unsigned 8-bit numbers.",
			"System.Math.Exp" => "Returns e raised to the specified power.",
			"System.Math.Floor" => "Returns the largest integral value less than or equal to the specified decimal number.",
			"System.Math.FusedMultiplyAdd" => "Returns (x * y) + z, rounded as one ternary operation.",
			"System.Math.IEEERemainder" => "Returns the remainder resulting from the division of a specified number by another specified number.",
			"System.Math.ILogB" => "Returns the base 2 integer logarithm of a specified number.",
			"System.Math.Log" => "Returns the natural (base e) logarithm of a specified number.",
			"System.Math.Log10" => "Returns the base 10 logarithm of a specified number.",
			"System.Math.Log2" => "Returns the base 2 logarithm of a specified number.",
			"System.Math.Max" => "Returns the larger of two numbers.",
			"System.Math.MaxMagnitude" => "Returns the larger magnitude of two double-precision floating-point numbers.",
			"System.Math.Min" => "Returns the smaller of two numbers.",
			"System.Math.MinMagnitude" => "Returns the smaller magnitude of two double-precision floating-point numbers.",
			"System.Math.Pow" => "Returns a specified number raised to the specified power.",
			"System.Math.ReciprocalEstimate" => "Returns an estimate of the reciprocal of a specified number.",
			"System.Math.ReciprocalSqrtEstimate" => "Returns an estimate of the reciprocal square root of a specified number.",
			"System.Math.Round" => "Rounds a decimal value to the nearest integral value, and rounds midpoint values to the nearest even number.",
			"System.Math.ScaleB" => "Returns x * 2^n computed efficiently.",
			"System.Math.Sign" => "Returns an integer that indicates the sign of a number.",
			"System.Math.Sin" => "Returns the sine of the specified angle.",
			"System.Math.SinCos" => "Returns the sine and cosine of the specified angle.",
			"System.Math.Sinh" => "Returns the hyperbolic sine of the specified angle.",
			"System.Math.Sqrt" => "Returns the square root of a specified number.",
			"System.Math.Tan" => "Returns the tangent of the specified angle.",
			"System.Math.Tanh" => "Returns the hyperbolic tangent of the specified angle.",
			"System.Math.Truncate(" => "Calculates the integral part of a specified number.",
			_ => string.Empty
		};
		if (!string.IsNullOrEmpty(methodDescription))
		{
			method.Description = methodDescription;
		}
	}
}
