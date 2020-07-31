namespace PanoramicData.Blazor
{
	/// <summary>
	/// The PDColumnConfig class allow the ability to change certain column settings at runtime.
	/// </summary>
	public class PDColumnConfig
    {
		/// <summary>
		///  Gets or sets the columns unique Id.
		/// </summary>
		/// <remarks>This must be provided in order to locate the column to be updated.</remarks>
		public string Id { get; set; } = string.Empty;

		/// <summary>
		/// Gets or sets an override for the columns default title.
		/// </summary>
		public string? Title { get; set; }

		/// <summary>
		/// Gets or sets whether this columns can be edited when the containing row is in edit mode.
		/// </summary>
		public bool? Editable { get; set; }
	}
}
