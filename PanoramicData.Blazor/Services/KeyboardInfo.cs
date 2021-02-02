namespace PanoramicData.Blazor.Services
{
	/// <summary>
	/// The KeyboardInfo class provides information regarding a keyboard event.
	/// </summary>
	public class KeyboardInfo
	{
		/// <summary>
		/// Gets or sets the key that the event relates to.
		/// </summary>
		public string Key { get; set; } = string.Empty;

		/// <summary>
		/// Gets or sets the state of the Alt key when the event occurred.
		/// </summary>
		public bool AltKey { get; set; }

		/// <summary>
		/// Gets or sets the state of the Control key when the event occurred.
		/// </summary>
		public bool CtrlKey { get; set; }

		/// <summary>
		/// Gets or sets the state of the Shift key when the event occurred.
		/// </summary>
		public bool ShiftKey { get; set; }
	}
}
