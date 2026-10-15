namespace PanoramicData.Blazor.Models;

/// <summary>
/// One bar drawn in a segment lane of a <see cref="PDMediaPlayer"/>. Very short segments of the same kind that fall
/// in the same slot of a lane are merged into one bar, so the number of bars drawn is bounded.
/// </summary>
/// <param name="Lane">The lane, counting from zero.</param>
/// <param name="LeftPercent">The left edge, as a percentage of the lane's width.</param>
/// <param name="WidthPercent">The width, as a percentage of the lane's width.</param>
/// <param name="Color">The colour to draw the bar in.</param>
/// <param name="First">The earliest merged segment; selecting the bar selects this segment.</param>
/// <param name="Count">The number of merged segments.</param>
/// <param name="Start">The earliest start of the merged segments.</param>
/// <param name="End">The latest end of the merged segments.</param>
internal sealed record MediaSegmentBar(
	int Lane,
	double LeftPercent,
	double WidthPercent,
	string Color,
	PDMediaSegment First,
	int Count,
	TimeSpan Start,
	TimeSpan End);
