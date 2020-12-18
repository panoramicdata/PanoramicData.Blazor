namespace PanoramicData.Blazor
{
	/// <summary>
	/// The DropEventArgs class provides information for PDDragContext events.
	/// </summary>
	public class DropEventArgs
	{
		/// <summary>
		/// Initializes a new instance of the DropEventArgs class.
		/// </summary>
		/// <param name="target">Details on where the drop occurred.</param>
		/// <param name="payload">Payload of the drop.</param>
		/// <param name="ctrl">Was the control key pressed during the drop?</param>
		/// <param name="before">Is the drop operation before or after the selection item?</param>
		public DropEventArgs(object? target, object? payload, bool ctrl, bool? before = null)
		{
			Target = target;
			Payload = payload;
			Ctrl = ctrl;
			Before = before;
		}

		/// <summary>
		/// Gets or sets details on where the drop occurred.
		/// </summary>
		public object? Target { get; set; }

		/// <summary>
		/// Gets the payload associated with the drop.
		/// </summary>
		public object? Payload { get; }

		/// <summary>
		/// Gets whether the control key was held down on drop.
		/// </summary>
		public bool Ctrl { get; }

		/// <summary>
		/// Gets whether the drop operation is before (true), after (false) or not relevant (null) to the Node specified.
		/// </summary>
		public bool? Before { get; }
	}
}
