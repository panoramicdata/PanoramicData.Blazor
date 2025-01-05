namespace PanoramicData.Blazor.Arguments;

public class CustomMoveCopyArgs : MoveCopyArgs
{
	/// <summary>
	/// Gets or sets whether the default move/copy operation should be cancelled.
	/// </summary>
	public bool CancelDefault { get; set; }
}
