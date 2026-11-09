namespace PanoramicData.Blazor.Models;

/// <summary>
/// Specifies how a reply's progress steps are shown while it is being composed.
/// </summary>
public enum ChatProgressDisplayMode
{
	/// <summary>Every step so far, one per line, oldest first.</summary>
	List = 0,

	/// <summary>The latest step alone on one line, rolling over to each new step, with the time taken so far and the
	/// full list on request.</summary>
	Ticker = 1
}
