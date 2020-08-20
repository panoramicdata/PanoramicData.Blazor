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
		///<param name="target">Details on where the drop occurred.</param>
		/// <param name="payload">Payload of the drop.</param>
		public DropEventArgs(object? target, object? payload)
		{
			Target = target;
			Payload = payload;
		}

		/// <summary>
		/// Gets detail son where the drop occurred.
		/// </summary>
		public object? Target { get; }

		/// <summary>
		/// Gets the payload associated with the drop.
		/// </summary>
		public object? Payload { get; }
	}
}
