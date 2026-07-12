namespace PanoramicData.Blazor.Models;

/// <summary>
/// Configuration options for the <see cref="PanoramicData.Blazor.PDRange"/> slider component.
/// </summary>
public record RangeOptions
{
	/// <summary>Gets or sets the track appearance options.</summary>
	public TrackOptions Track { get; init; } = new();
}

/// <summary>
/// Configures the appearance of the slider track in a <see cref="PanoramicData.Blazor.PDRange"/> component.
/// </summary>
public record TrackOptions
{
	/// <summary>Gets or sets the height of the slider track relative to the component height. Value is a fraction between 0.0 and 1.0. Defaults to 0.5.</summary>
	public double Height { get; init; } = 0.5;
}
