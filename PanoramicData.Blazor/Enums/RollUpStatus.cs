namespace PanoramicData.Blazor.Enums;

/// <summary>
/// Represents the roll-up status of a component or system, ordered from least to most severe.
/// </summary>
public enum RollUpStatus
{
	/// <summary>Status is unknown or not applicable.</summary>
	Gray,

	/// <summary>All checks pass.</summary>
	Green,

	/// <summary>One or more checks have a warning.</summary>
	Amber,

	/// <summary>One or more checks are in a failed/error state.</summary>
	Red
}
