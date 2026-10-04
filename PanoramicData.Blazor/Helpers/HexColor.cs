namespace PanoramicData.Blazor.Helpers;

/// <summary>
/// Parses and formats the hex colours used by <see cref="PDTiles"/>.
/// </summary>
internal static class HexColor
{
	/// <summary>
	/// Parses a hex colour in the #RRGGBB or short #RGB form (the leading # is optional).
	/// </summary>
	/// <exception cref="ArgumentException">The value is not a hex colour in either form.</exception>
	internal static (int r, int g, int b) Parse(string hex)
	{
		var digits = (hex ?? string.Empty).TrimStart('#');
		if (digits.Length == 3)
		{
			digits = string.Concat(digits.Select(c => new string(c, 2)));
		}

		if (digits.Length != 6 || !digits.All(Uri.IsHexDigit))
		{
			throw new ArgumentException($"'{hex}' is not a valid colour: PDTiles accepts hex colours in the form #RGB or #RRGGBB.", nameof(hex));
		}

		return (
			Convert.ToInt32(digits[..2], 16),
			Convert.ToInt32(digits.Substring(2, 2), 16),
			Convert.ToInt32(digits.Substring(4, 2), 16)
		);
	}

	/// <summary>
	/// Formats colour components (each clamped to 0-255) as #RRGGBB.
	/// </summary>
	internal static string Format(int r, int g, int b) => $"#{Math.Clamp(r, 0, 255):X2}{Math.Clamp(g, 0, 255):X2}{Math.Clamp(b, 0, 255):X2}";
}
