namespace PanoramicData.Blazor
{
	/// <summary>
	/// An enumeration of possible Form modes.
	/// </summary>
	public enum FormModes
	{
		/// <summary>
		/// No form fields are displayed.
		/// </summary>
		Hidden,
		/// <summary>
		/// Displayed but empty.
		/// </summary>
		Empty,
		/// <summary>
		/// The form is being used to define a new entity.
		/// </summary>
		Create,
		/// <summary>
		/// The form is being used to edit an existing entity.
		/// </summary>
		Edit,
		/// <summary>
		/// The form is being used confirm deletion of an existing entity.
		/// </summary>
		Delete,
		/// <summary>
		/// The form has changes and the user is being prompted for confirmation to cancel.
		/// </summary>
		Cancel,
		/// <summary>
		/// The form is displayed but all control are in a read only state.
		/// </summary>
		ReadOnly
	}
}
