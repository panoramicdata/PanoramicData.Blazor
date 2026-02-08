using System.Globalization;

namespace PanoramicData.Blazor.Models.ColorPicker;

/// <summary>
/// Represents a color value with support for multiple color spaces.
/// </summary>
public class ColorValue : IEquatable<ColorValue>
{
	/// <summary>
	/// Red component (0-255).
	/// </summary>
	public byte R { get; set; }

	/// <summary>
	/// Green component (0-255).
	/// </summary>
	public byte G { get; set; }

	/// <summary>
	/// Blue component (0-255).
	/// </summary>
	public byte B { get; set; }

	/// <summary>
	/// Alpha/opacity component (0-1).
	/// </summary>
	public double A { get; set; } = 1.0;

	/// <summary>
	/// Gets or sets the hue (0-360 degrees).
	/// </summary>
	public double H { get; private set; }

	/// <summary>
	/// Gets or sets the saturation (0-1).
	/// </summary>
	public double S { get; private set; }

	/// <summary>
	/// Gets or sets the value/brightness (0-1) for HSV.
	/// </summary>
	public double V { get; private set; }

	/// <summary>
	/// Gets or sets the lightness (0-1) for HSL.
	/// </summary>
	public double L { get; private set; }

	/// <summary>
	/// Creates a new ColorValue with default values (black).
	/// </summary>
	public ColorValue()
	{
		UpdateHsvFromRgb();
		UpdateHslFromRgb();
	}

	/// <summary>
	/// Creates a new ColorValue from RGB values.
	/// </summary>
	public ColorValue(byte r, byte g, byte b, double a = 1.0)
	{
		R = r;
		G = g;
		B = b;
		A = Math.Clamp(a, 0, 1);
		UpdateHsvFromRgb();
		UpdateHslFromRgb();
	}

	/// <summary>
	/// Creates a ColorValue from a hex string.
	/// </summary>
	public static ColorValue FromHex(string hex)
	{
		var color = new ColorValue();
		color.SetFromHex(hex);
		return color;
	}

	/// <summary>
	/// Creates a ColorValue from HSV values.
	/// </summary>
	public static ColorValue FromHsv(double h, double s, double v, double a = 1.0)
	{
		var color = new ColorValue { A = Math.Clamp(a, 0, 1) };
		color.SetFromHsv(h, s, v);
		return color;
	}

	/// <summary>
	/// Creates a ColorValue from HSL values.
	/// </summary>
	public static ColorValue FromHsl(double h, double s, double l, double a = 1.0)
	{
		var color = new ColorValue { A = Math.Clamp(a, 0, 1) };
		color.SetFromHsl(h, s, l);
		return color;
	}

	/// <summary>
	/// Sets RGB values and updates HSV/HSL.
	/// </summary>
	public void SetRgb(byte r, byte g, byte b)
	{
		R = r;
		G = g;
		B = b;
		UpdateHsvFromRgb();
		UpdateHslFromRgb();
	}

	/// <summary>
	/// Sets the color from a hex string.
	/// </summary>
	public void SetFromHex(string hex)
	{
		if (string.IsNullOrWhiteSpace(hex))
		{
			return;
		}

		hex = hex.TrimStart('#');

		try
		{
			if (hex.Length == 3)
			{
			// Short form: #RGB
			R = byte.Parse($"{hex[0]}{hex[0]}", NumberStyles.HexNumber, CultureInfo.InvariantCulture);
			G = byte.Parse($"{hex[1]}{hex[1]}", NumberStyles.HexNumber, CultureInfo.InvariantCulture);
			B = byte.Parse($"{hex[2]}{hex[2]}", NumberStyles.HexNumber, CultureInfo.InvariantCulture);
			A = 1.0;
			}
			else if (hex.Length == 4)
			{
			// Short form with alpha: #RGBA
			R = byte.Parse($"{hex[0]}{hex[0]}", NumberStyles.HexNumber, CultureInfo.InvariantCulture);
			G = byte.Parse($"{hex[1]}{hex[1]}", NumberStyles.HexNumber, CultureInfo.InvariantCulture);
			B = byte.Parse($"{hex[2]}{hex[2]}", NumberStyles.HexNumber, CultureInfo.InvariantCulture);
			A = byte.Parse($"{hex[3]}{hex[3]}", NumberStyles.HexNumber, CultureInfo.InvariantCulture) / 255.0;
			}
			else if (hex.Length == 6)
			{
			// Standard form: #RRGGBB
			R = byte.Parse(hex[..2], NumberStyles.HexNumber, CultureInfo.InvariantCulture);
			G = byte.Parse(hex[2..4], NumberStyles.HexNumber, CultureInfo.InvariantCulture);
			B = byte.Parse(hex[4..6], NumberStyles.HexNumber, CultureInfo.InvariantCulture);
			A = 1.0;
			}
			else if (hex.Length == 8)
			{
			// With alpha: #RRGGBBAA
			R = byte.Parse(hex[..2], NumberStyles.HexNumber, CultureInfo.InvariantCulture);
			G = byte.Parse(hex[2..4], NumberStyles.HexNumber, CultureInfo.InvariantCulture);
			B = byte.Parse(hex[4..6], NumberStyles.HexNumber, CultureInfo.InvariantCulture);
			A = byte.Parse(hex[6..8], NumberStyles.HexNumber, CultureInfo.InvariantCulture) / 255.0;
			}

			UpdateHsvFromRgb();
			UpdateHslFromRgb();
		}
		catch
		{
			// Invalid hex, keep current values
		}
	}

	/// <summary>
	/// Sets the color from HSV values.
	/// </summary>
	public void SetFromHsv(double h, double s, double v)
	{
		H = h % 360;
		if (H < 0)
		{
			H += 360;
		}

		S = Math.Clamp(s, 0, 1);
		V = Math.Clamp(v, 0, 1);

		UpdateRgbFromHsv();
		UpdateHslFromRgb();
	}

	/// <summary>
	/// Sets the color from HSL values.
	/// </summary>
	public void SetFromHsl(double h, double s, double l)
	{
		H = h % 360;
		if (H < 0)
		{
			H += 360;
		}

		s = Math.Clamp(s, 0, 1);
		L = Math.Clamp(l, 0, 1);

		UpdateRgbFromHsl(s);
		UpdateHsvFromRgb();
	}

	/// <summary>
	/// Gets the hex representation without alpha.
	/// </summary>
	public string ToHex() => $"#{R:X2}{G:X2}{B:X2}";

	/// <summary>
	/// Gets the hex representation with alpha.
	/// </summary>
	public string ToHexWithAlpha() => $"#{R:X2}{G:X2}{B:X2}{(byte)(A * 255):X2}";

	/// <summary>
	/// Gets the RGB CSS representation.
	/// </summary>
	public string ToRgb() => $"rgb({R}, {G}, {B})";

	/// <summary>
	/// Gets the RGBA CSS representation.
	/// </summary>
	public string ToRgba() => $"rgba({R}, {G}, {B}, {A:F2})";

	/// <summary>
	/// Gets the HSL CSS representation.
	/// </summary>
	public string ToHsl() => $"hsl({H:F0}, {S * 100:F0}%, {L * 100:F0}%)";

	/// <summary>
	/// Gets the HSLA CSS representation.
	/// </summary>
	public string ToHsla() => $"hsla({H:F0}, {S * 100:F0}%, {L * 100:F0}%, {A:F2})";

	/// <summary>
	/// Gets a CSS-compatible color string based on alpha.
	/// </summary>
	public string ToCss() => A < 1.0 ? ToRgba() : ToHex();

	/// <summary>
	/// Creates a copy of this color.
	/// </summary>
	public ColorValue Clone() => new(R, G, B, A);

	private void UpdateHsvFromRgb()
	{
		var r = R / 255.0;
		var g = G / 255.0;
		var b = B / 255.0;

		var max = Math.Max(r, Math.Max(g, b));
		var min = Math.Min(r, Math.Min(g, b));
		var delta = max - min;

		// Value
		V = max;

		// Saturation
		S = max == 0 ? 0 : delta / max;

		// Hue
		if (delta == 0)
		{
			H = 0;
		}
		else if (max == r)
		{
			H = 60 * (((g - b) / delta) % 6);
		}
		else if (max == g)
		{
			H = 60 * (((b - r) / delta) + 2);
		}
		else
		{
			H = 60 * (((r - g) / delta) + 4);
		}

		if (H < 0)
		{
			H += 360;
		}
	}

	private void UpdateHslFromRgb()
	{
		var r = R / 255.0;
		var g = G / 255.0;
		var b = B / 255.0;

		var max = Math.Max(r, Math.Max(g, b));
		var min = Math.Min(r, Math.Min(g, b));

		// Lightness
		L = (max + min) / 2;

		// Note: HSL saturation is different from HSV saturation.
		// We keep HSV saturation in S property (set by UpdateHsvFromRgb).
		// HSL saturation would be: delta / (1 - |2L - 1|) where delta = max - min
	}

	private void UpdateRgbFromHsv()
	{
		var c = V * S;
		var x = c * (1 - Math.Abs((H / 60) % 2 - 1));
		var m = V - c;

		double r, g, b;

		if (H < 60)
		{
			r = c;
			g = x;
			b = 0;
		}
		else if (H < 120)
		{
			r = x;
			g = c;
			b = 0;
		}
		else if (H < 180)
		{
			r = 0;
			g = c;
			b = x;
		}
		else if (H < 240)
		{
			r = 0;
			g = x;
			b = c;
		}
		else if (H < 300)
		{
			r = x;
			g = 0;
			b = c;
		}
		else
		{
			r = c;
			g = 0;
			b = x;
		}

		R = (byte)Math.Round((r + m) * 255);
		G = (byte)Math.Round((g + m) * 255);
		B = (byte)Math.Round((b + m) * 255);
	}

	private void UpdateRgbFromHsl(double saturation)
	{
		if (saturation == 0)
		{
			var gray = (byte)Math.Round(L * 255);
			R = G = B = gray;
			return;
		}

		var q = L < 0.5 ? L * (1 + saturation) : L + saturation - L * saturation;
		var p = 2 * L - q;

		R = (byte)Math.Round(HueToRgb(p, q, H / 360 + 1.0 / 3) * 255);
		G = (byte)Math.Round(HueToRgb(p, q, H / 360) * 255);
		B = (byte)Math.Round(HueToRgb(p, q, H / 360 - 1.0 / 3) * 255);
	}

	private static double HueToRgb(double p, double q, double t)
	{
		if (t < 0)
		{
			t += 1;
		}

		if (t > 1)
		{
			t -= 1;
		}

		if (t < 1.0 / 6)
		{
			return p + (q - p) * 6 * t;
		}

		if (t < 1.0 / 2)
		{
			return q;
		}

		if (t < 2.0 / 3)
		{
			return p + (q - p) * (2.0 / 3 - t) * 6;
		}

		return p;
	}

	public bool Equals(ColorValue? other)
	{
		if (other is null)
		{
			return false;
		}

		return R == other.R && G == other.G && B == other.B && Math.Abs(A - other.A) < 0.001;
	}

	public override bool Equals(object? obj) => Equals(obj as ColorValue);

	public override int GetHashCode() => HashCode.Combine(R, G, B, A);

	public static bool operator ==(ColorValue? left, ColorValue? right)
	{
		if (left is null)
		{
			return right is null;
		}

		return left.Equals(right);
	}

	public static bool operator !=(ColorValue? left, ColorValue? right) => !(left == right);
}
