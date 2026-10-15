namespace PanoramicData.Blazor.Helpers;

/// <summary>
/// Validates and steps through the playback rates offered by a <see cref="PDMediaPlayer"/>.
/// </summary>
public static class MediaPlaybackRates
{
	/// <summary>The slowest rate allowed.</summary>
	public static double Minimum { get; } = 0.5;

	/// <summary>The fastest rate allowed.</summary>
	public static double Maximum { get; } = 4;

	/// <summary>The normal rate.</summary>
	public static double Normal { get; } = 1;

	/// <summary>
	/// Gets the rates offered when none are given: 0.75, 1, 1.25, 1.5, 1.75 and 2.
	/// </summary>
	public static IReadOnlyList<double> Default { get; } = [0.75, 1, 1.25, 1.5, 1.75, 2];

	/// <summary>
	/// Limits a rate to the allowed range, rounding it to two decimal places. A rate that is not a finite number
	/// becomes the normal rate.
	/// </summary>
	/// <param name="rate">The rate.</param>
	/// <returns>The rate, between <see cref="Minimum"/> and <see cref="Maximum"/>.</returns>
	public static double Clamp(double rate)
		=> double.IsFinite(rate) ? Math.Round(Math.Clamp(rate, Minimum, Maximum), 2) : Normal;

	/// <summary>
	/// Cleans a list of rates: drops values that are not finite or are outside the allowed range, rounds to two
	/// decimal places, removes duplicates and sorts. An empty or missing list gives <see cref="Default"/>.
	/// </summary>
	/// <param name="rates">The rates to clean.</param>
	/// <returns>The cleaned rates.</returns>
	public static IReadOnlyList<double> Normalize(IEnumerable<double>? rates)
	{
		if (rates is null)
		{
			return Default;
		}

		var cleaned = rates
			.Where(r => double.IsFinite(r) && r >= Minimum && r <= Maximum)
			.Select(r => Math.Round(r, 2))
			.Distinct()
			.Order()
			.ToList();
		return cleaned.Count == 0 ? Default : cleaned;
	}

	/// <summary>
	/// Finds the next slower or faster rate in a list.
	/// </summary>
	/// <param name="rates">The rates, sorted ascending.</param>
	/// <param name="current">The current rate, which need not be in the list.</param>
	/// <param name="faster">True for the next faster rate, false for the next slower one.</param>
	/// <returns>The rate, or <paramref name="current"/> when there is none in that direction.</returns>
	public static double Step(IReadOnlyList<double> rates, double current, bool faster)
	{
		ArgumentNullException.ThrowIfNull(rates);
		const double epsilon = 0.001;
		return faster
			? rates.Where(r => r > current + epsilon).DefaultIfEmpty(current).Min()
			: rates.Where(r => r < current - epsilon).DefaultIfEmpty(current).Max();
	}

	/// <summary>
	/// Formats a rate for display, for example <c>1.25×</c>.
	/// </summary>
	/// <param name="rate">The rate.</param>
	/// <returns>The label.</returns>
	public static string FormatLabel(double rate) => rate.ToString("0.##", CultureInfo.InvariantCulture) + "×";
}
