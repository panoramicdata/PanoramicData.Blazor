using System.Drawing;

namespace PanoramicData.Blazor.Extensions;

/// <summary>
/// Extension methods for working with CSS colour strings.
/// </summary>
public static class ColorExtensions
{
	/// <summary>
	/// Linearly interpolates between two HTML colour strings and returns the resulting colour as a hex string.
	/// </summary>
	/// <param name="color1">The start colour as an HTML colour string (e.g. <c>#000000</c>). Used when <paramref name="ratio"/> is 0.</param>
	/// <param name="color2">The end colour as an HTML colour string (e.g. <c>#ffffff</c>). Used when <paramref name="ratio"/> is 1.</param>
	/// <param name="ratio">A value between 0.0 and 1.0 that controls the mix of the two colours.</param>
	/// <returns>A hex colour string (e.g. <c>#7f7f7f</c>), or <paramref name="color1"/> if either colour string cannot be parsed.</returns>
	public static string Interpolate(string color1, string color2, double ratio)
	{
		try
		{
			var c1 = ColorTranslator.FromHtml(color1);
			var c2 = ColorTranslator.FromHtml(color2);

			var r = (int)(c1.R * (1 - ratio) + c2.R * ratio);
			var g = (int)(c1.G * (1 - ratio) + c2.G * ratio);
			var b = (int)(c1.B * (1 - ratio) + c2.B * ratio);

			return $"#{r:X2}{g:X2}{b:X2}";
		}
		catch
		{
			return color1;
		}
	}
}
