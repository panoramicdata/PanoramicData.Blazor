namespace PanoramicData.Blazor.Enums;

/// <summary>
/// Represents the roll-up status of a component or system.
/// </summary>
/// <remarks>
/// The member order is declaration order only and carries no meaning; it is not a
/// severity order, and nothing should compare these values numerically. By severity,
/// worst first, the order is <see cref="Gray"/>, <see cref="Red"/>, <see cref="Amber"/>,
/// <see cref="Green"/>: an unknown status is always the worst, because a failure is at
/// least known, while an unknown could be anything, including a failure nobody can see.
/// The values are not renumbered, because consumers may persist them.
/// </remarks>
public enum RollUpStatus
{
	/// <summary>Status is unknown: it could not be assessed. The most severe status.</summary>
	Gray,

	/// <summary>All checks pass.</summary>
	Green,

	/// <summary>One or more checks have a warning.</summary>
	Amber,

	/// <summary>One or more checks are in a failed/error state.</summary>
	Red
}
