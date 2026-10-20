namespace PanoramicData.Blazor.Enums;

/// <summary>
/// Specifies how a <see cref="PDMediaPlayer"/> moves its view when <see cref="MediaFollowMode.Margin"/> needs it to.
/// </summary>
public enum MediaFollowScroll
{
	/// <summary>The view moves continuously, so that the playhead holds exactly the margin once it gets there.</summary>
	Glide,

	/// <summary>The view jumps by most of a screen when the playhead reaches the margin, then holds still until the next time.</summary>
	PageTurn
}
