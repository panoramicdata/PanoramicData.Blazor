namespace PanoramicData.Blazor
{
	/// <summary>
	/// Enumeration of possible conflict resolutions.
	/// </summary>
	public enum ConflictResolutions
	{
		/// <summary>
		/// Prompt the user for decision.
		/// </summary>
		Prompt,
		/// <summary>
		/// Cancel the entire operation.
		/// </summary>
		Cancel,
		/// <summary>
		/// Copy / move all items except the conflicting ones.
		/// </summary>
		Skip,
		/// <summary>
		/// Copy / move all items, overwriting the conflicting items.
		/// </summary>
		Overwrite,
		/// <summary>
		/// Rename new items so as to avoid name conflicts.
		/// </summary>
		Rename
	}
}
