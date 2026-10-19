namespace PanoramicData.Blazor.Models;

/// <summary>
/// Describes a click on the visualisation (spectrogram) of a <see cref="PDMediaPlayer"/>.
/// </summary>
/// <param name="Time">The time in the media that the click was at.</param>
/// <param name="FrequencyHz">The frequency in hertz that the click was at.</param>
public sealed record PDMediaVisualisationClick(TimeSpan Time, double FrequencyHz);
