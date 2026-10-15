namespace PanoramicData.Blazor.Models;

/// <summary>
/// What a <see cref="PDMediaPlayer"/> with a <see cref="PDMediaPlayer.PersistKey"/> keeps in local storage: the chosen
/// playback rate and the most recent positions, one per source.
/// </summary>
internal sealed class MediaPlayerPersistedState
{
	/// <summary>The number of sources whose position is remembered; the least recently saved is dropped first.</summary>
	internal const int MaxPositions = 50;

	/// <summary>Gets or sets the chosen playback rate, if one has been chosen.</summary>
	public double? Rate { get; set; }

	/// <summary>Gets or sets the remembered positions, most recently saved first.</summary>
	public List<MediaPlayerPersistedPosition> Positions { get; set; } = [];

	/// <summary>Gets the remembered position for a source, if there is one.</summary>
	/// <param name="src">The media source.</param>
	/// <returns>The position, or <c>null</c>.</returns>
	public TimeSpan? GetPosition(string? src)
	{
		var entry = Positions.Find(p => p.Src == src);
		return entry is null || !double.IsFinite(entry.Seconds) || entry.Seconds <= 0
			? null
			: TimeSpan.FromSeconds(entry.Seconds);
	}

	/// <summary>Remembers the position for a source, making it the most recent.</summary>
	/// <param name="src">The media source.</param>
	/// <param name="position">The position.</param>
	public void SetPosition(string src, TimeSpan position)
	{
		RemovePosition(src);
		Positions.Insert(0, new MediaPlayerPersistedPosition { Src = src, Seconds = Math.Round(position.TotalSeconds, 1) });
		if (Positions.Count > MaxPositions)
		{
			Positions.RemoveRange(MaxPositions, Positions.Count - MaxPositions);
		}
	}

	/// <summary>Forgets the position for a source.</summary>
	/// <param name="src">The media source.</param>
	/// <returns>Whether a position was forgotten.</returns>
	public bool RemovePosition(string? src) => Positions.RemoveAll(p => p.Src == src) > 0;
}

/// <summary>
/// A remembered position of one source.
/// </summary>
internal sealed class MediaPlayerPersistedPosition
{
	/// <summary>Gets or sets the media source.</summary>
	public string Src { get; set; } = string.Empty;

	/// <summary>Gets or sets the position, in seconds.</summary>
	public double Seconds { get; set; }
}
