namespace PanoramicData.Blazor.Models;

/// <summary>
/// A stretch of the seek bar of a <see cref="PDMediaPlayer"/>, such as a buffered range or the shaded range.
/// </summary>
/// <param name="LeftPercent">The left edge, as a percentage of the bar's width.</param>
/// <param name="WidthPercent">The width, as a percentage of the bar's width.</param>
internal readonly record struct MediaBarSegment(double LeftPercent, double WidthPercent);
