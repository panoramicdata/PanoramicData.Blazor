namespace PanoramicData.Blazor.Models;

/// <summary>
/// A notable point in time and frequency on the spectrogram of a <see cref="PDMediaPlayer"/>, such as a spectral peak.
/// The player draws these as dots when <see cref="PDMediaPlayer.ShowKeyPoints"/> is set; it does not interpret them.
/// </summary>
public sealed class PDMediaKeyPoint
{
	/// <summary>
	/// Gets the offset of the point from the start of the media.
	/// </summary>
	public TimeSpan Time { get; init; }

	/// <summary>
	/// Gets the frequency of the point in hertz.
	/// </summary>
	public double FrequencyHz { get; init; }

	/// <summary>
	/// Gets the relative importance of the point, from 0 to 1. Heavier points are drawn larger. The default is 0.5.
	/// </summary>
	public double Weight { get; init; } = 0.5;
}
