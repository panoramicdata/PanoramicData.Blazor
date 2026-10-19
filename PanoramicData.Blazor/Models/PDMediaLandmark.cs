namespace PanoramicData.Blazor.Models;

/// <summary>
/// A line between two points on the spectrogram of a <see cref="PDMediaPlayer"/>, such as an audio-fingerprint landmark that
/// pairs one spectral peak with a later one. The player draws these when <see cref="PDMediaPlayer.ShowLandmarks"/> is set.
/// </summary>
public sealed class PDMediaLandmark
{
	/// <summary>
	/// Gets the offset of the first point from the start of the media.
	/// </summary>
	public TimeSpan FromTime { get; init; }

	/// <summary>
	/// Gets the frequency of the first point in hertz.
	/// </summary>
	public double FromFrequencyHz { get; init; }

	/// <summary>
	/// Gets the offset of the second point from the start of the media.
	/// </summary>
	public TimeSpan ToTime { get; init; }

	/// <summary>
	/// Gets the frequency of the second point in hertz.
	/// </summary>
	public double ToFrequencyHz { get; init; }

	/// <summary>
	/// Gets an optional CSS colour for this landmark, overriding the default.
	/// </summary>
	public string? Color { get; init; }
}
