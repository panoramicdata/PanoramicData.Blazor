using System;
using System.Drawing;

namespace PanoramicData.Blazor.Extensions;

public static class ColorExtensions
{
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
