namespace PanoramicData.Blazor.Models;

public record RangeOptions
{
	public TrackOptions Track { get; init; } = new();
}

public record TrackOptions
{
	public double Height { get; init; } = 0.5;
}
