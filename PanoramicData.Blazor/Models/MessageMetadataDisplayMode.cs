namespace PanoramicData.Blazor.Models;

/// <summary>
/// Specifies on which side of the chat view message metadata (sender name, timestamp, icon) is rendered.
/// </summary>
public enum MessageMetadataDisplayMode
{
	/// <summary>The current user's messages show metadata on the right; all other senders show it on the left.</summary>
	UserOnlyOnRightOthersOnLeft = 0,
	/// <summary>The current user's messages show metadata on the left; all other senders show it on the right.</summary>
	UserOnlyOnLeftOthersOnRight = 1,
	/// <summary>Metadata is always shown on the left side of the message.</summary>
	AlwaysOnLeft = 2,
	/// <summary>Metadata is always shown on the right side of the message.</summary>
	AlwaysOnRight = 3
}