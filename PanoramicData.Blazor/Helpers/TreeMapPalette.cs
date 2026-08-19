namespace PanoramicData.Blazor.Helpers;

/// <summary>
/// Supplies the colours used by a tree map when the caller has not provided them.
/// </summary>
/// <remarks>
/// Colours are emitted as HSL strings rather than named CSS variables because each rectangle needs
/// an individual value; the surrounding chrome is themed through CSS custom properties as usual.
/// The categorical hues are spaced around the wheel and kept at a lightness that holds sufficient
/// contrast against the white label text in both light and dark themes.
/// </remarks>
public static class TreeMapPalette
{
	private static readonly int[] _categoryHues = [210, 25, 145, 280, 45, 190, 330, 95, 255, 15];

	/// <summary>
	/// Gets the number of distinct hues in the categorical palette.
	/// </summary>
	public static int CategoryCount => _categoryHues.Length;

	/// <summary>
	/// Returns a stable categorical colour for the given key. The same key always yields the same colour.
	/// </summary>
	/// <param name="key">The grouping key. A null or empty key is treated as a single unnamed group.</param>
	/// <returns>A CSS colour string.</returns>
	public static string ForCategory(string? key)
	{
		var index = StableIndex(key, _categoryHues.Length);

		return Hsl(_categoryHues[index], 55, 45);
	}

	/// <summary>
	/// Returns a colour representing the given depth, shading progressively lighter as depth increases.
	/// </summary>
	/// <param name="depth">The zero-based depth of the node.</param>
	/// <param name="maximumDepth">The greatest depth being rendered, used to spread the available range.</param>
	/// <returns>A CSS colour string.</returns>
	public static string ForDepth(int depth, int maximumDepth)
	{
		var span = Math.Max(1, maximumDepth);
		var fraction = Math.Clamp((double)depth / span, 0, 1);
		var lightness = 32 + (fraction * 38);

		return Hsl(210, 48, lightness);
	}

	/// <summary>
	/// Returns a colour from a sequential scale running from cool to warm.
	/// </summary>
	/// <param name="value">The value to map.</param>
	/// <param name="minimum">The value mapped to the cool end of the scale.</param>
	/// <param name="maximum">The value mapped to the warm end of the scale.</param>
	/// <returns>A CSS colour string.</returns>
	public static string ForHeat(double value, double minimum, double maximum)
	{
		var fraction = 0.0;

		if (maximum > minimum && !double.IsNaN(value) && !double.IsInfinity(value))
		{
			fraction = Math.Clamp((value - minimum) / (maximum - minimum), 0, 1);
		}

		// 210 (cool blue) through to 0 (warm red).
		var hue = 210 - (fraction * 210);

		return Hsl(hue, 62, 45);
	}

	/// <summary>
	/// Returns the fallback colour used when no other mode applies.
	/// </summary>
	/// <returns>A CSS colour string.</returns>
	public static string Fallback() => Hsl(210, 30, 45);

	private static string Hsl(double hue, double saturation, double lightness)
		=> string.Create(
			CultureInfo.InvariantCulture,
			$"hsl({hue:0.#} {saturation:0.#}% {lightness:0.#}%)");

	private static int StableIndex(string? key, int buckets)
	{
		if (string.IsNullOrEmpty(key))
		{
			return 0;
		}

		// A small deterministic hash. String.GetHashCode is randomised per process, which would make
		// colours change between runs and between server and client.
		var hash = 2166136261;

		foreach (var character in key)
		{
			hash ^= character;
			hash *= 16777619;
		}

		return (int)(hash % (uint)buckets);
	}
}
