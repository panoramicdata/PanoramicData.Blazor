namespace PanoramicData.Blazor
{
	/// <summary>
	/// The FormFieldResult class provides the result of a form field operation.
	/// </summary>
	public class FormFieldResult
	{
		/// <summary>
		/// Gets or sets if the operation was canceled.
		/// </summary>
		public bool Canceled { get; set; }

		/// <summary>
		/// Gets or sets new field value.
		/// </summary>
		public object? NewValue { get; set; }
	}
}
