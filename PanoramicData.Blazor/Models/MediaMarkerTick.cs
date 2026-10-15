namespace PanoramicData.Blazor.Models;

/// <summary>
/// One mark drawn on the seek bar of a <see cref="PDMediaPlayer"/>. Markers of the same kind that would fall in the
/// same slot of the bar are merged into one tick, so the number of ticks drawn is bounded whatever the number of
/// markers.
/// </summary>
/// <param name="LeftPercent">The left edge, as a percentage of the bar's width.</param>
/// <param name="WidthPercent">The width, as a percentage of the bar's width (zero for a single point).</param>
/// <param name="Kind">The kind shared by the merged markers.</param>
/// <param name="Color">The colour to draw the tick in.</param>
/// <param name="CssClass">The CSS class of the first merged marker, if any.</param>
/// <param name="First">The earliest merged marker; selecting the tick selects this marker.</param>
/// <param name="Count">The number of merged markers.</param>
/// <param name="Start">The earliest start of the merged markers.</param>
/// <param name="End">The latest end of the merged markers.</param>
/// <param name="IsSpan">Whether any merged marker is a span.</param>
internal sealed record MediaMarkerTick(
	double LeftPercent,
	double WidthPercent,
	string Kind,
	string Color,
	string? CssClass,
	PDMediaMarker First,
	int Count,
	TimeSpan Start,
	TimeSpan End,
	bool IsSpan);
