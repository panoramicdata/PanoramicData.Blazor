namespace PanoramicData.Blazor.Arguments;

/// <summary>
/// Provides arguments for a move or copy operation and allows the default action to be cancelled.
/// </summary>
public class CustomMoveCopyArgs : MoveCopyArgs
{
	/// <summary>
	/// Gets or sets whether the default move/copy operation should be cancelled.
	/// </summary>
	public bool CancelDefault { get; set; }
}
