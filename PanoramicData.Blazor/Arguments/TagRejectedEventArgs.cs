namespace PanoramicData.Blazor.Arguments;

/// <summary>
/// Event arguments describing a tag that was rejected by a PDTagInput and why.
/// </summary>
public class TagRejectedEventArgs(string tag, TagRejectionReason reason) : EventArgs
{
	/// <summary>
	/// Gets the trimmed tag text that was rejected.
	/// </summary>
	public string Tag { get; } = tag;

	/// <summary>
	/// Gets the reason the tag was rejected.
	/// </summary>
	public TagRejectionReason Reason { get; } = reason;
}
