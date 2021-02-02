using System;

namespace PanoramicData.Blazor.Services
{
	/// <summary>
	/// The GlobalEventService provides a concrete implementation of the IGlobalEventService interface.
	/// </summary>
	public class GlobalEventService : IGlobalEventService
	{
		/// <summary>
		/// Event raised whenever the KeyDown event occurs.
		/// </summary>
		public event EventHandler<KeyboardInfo>? KeyDownEvent;

		/// <summary>
		/// Event raised whenever the KeyUp event occurs.
		/// </summary>
		public event EventHandler<KeyboardInfo>? KeyUpEvent;

		/// <summary>
		/// Fires the KeyDown event with the given parameters.
		/// </summary>
		/// <param name="keyboardInfo">Details about the key pressed, along with modifier key states.</param>
		public void KeyDown(KeyboardInfo keyboardInfo) => KeyDownEvent?.Invoke(this, keyboardInfo);

		/// <summary>
		/// Fires the KeyUp event with the given parameters.
		/// </summary>
		/// <param name="keyboardInfo">Details about the key pressed, along with modifier key states.</param>
		public void KeyUp(KeyboardInfo keyboardInfo) => KeyUpEvent?.Invoke(this, keyboardInfo);
	}
}
