namespace PanoramicData.Blazor.Demo.Data;

public enum TodoProgress
{
	/// <summary>
	/// The todo item is being defined, no progress has been made yet.
	/// </summary>
	BeingDefined,

	/// <summary>
	/// The todo item is ready to be worked on, but no work has started yet.
	/// </summary>
	ReadyForProgress,

	/// <summary>
	/// The todo item is currently being worked on, some progress has been made.
	/// </summary>
	InProgress,

	/// <summary>
	/// The todo item is ready for testing, indicating that the work is complete and needs verification.
	/// </summary>
	ReadyForTest,

	/// <summary>
	/// The todo item has been tested and is ready for deployment or final review.
	/// </summary>
	Done
}