namespace PanoramicData.Blazor.Enums;

/// <summary>
/// Controls what is shown in the dashboard header row when in display mode.
/// </summary>
public enum DisplayModeHeaderContent
{
	/// <summary>No header row in display mode.</summary>
	None,

	/// <summary>Show the dashboard name only.</summary>
	DashboardName,

	/// <summary>Show the current tab name only.</summary>
	TabName,

	/// <summary>Show both the dashboard name and the current tab name.</summary>
	Both
}
