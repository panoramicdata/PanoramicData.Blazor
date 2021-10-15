namespace PanoramicData.Blazor.Arguments
{
	/// <summary>
	/// The BeforeNavigateEventArgs class holds details about an impending navigation.
	/// </summary>
	public class BeforeNavigateEventArgs : CancelEventArgs
	{
		/// <summary>
		/// Gets or sets the target navigation path.
		/// </summary>
		public string Target { get; set; } = string.Empty;
	}
}
