namespace PanoramicData.Blazor.Models
{
	/// <summary>
	/// The OperationResponse class is used to return the results of an operation.
	/// </summary>
	public class OperationResponse
	{
		/// <summary>
		/// Gets or sets whether the operation was successful or not.
		/// </summary>
		public bool Success { get; set; }

		/// <summary>
		/// Gets or sets a short description of any error encountered.
		/// </summary>
		public string ErrorMessage { get; set; } = string.Empty;
	}
}
