using System;

namespace PanoramicData.Blazor.Services
{
	/// <summary>
	/// The IGlobalEventService interface defines required properties, methods and events that must be
	/// implemented in order to provide global event related services.
	/// </summary>
	public interface IGlobalEventService
	{
		/// <summary>
		/// Event raised whenever the KeyDown event occurs.
		/// </summary>
		event EventHandler<KeyboardInfo>? KeyDownEvent;

		/// <summary>
		/// Event raised whenever the KeyUp event occurs.
		/// </summary>
		event EventHandler<KeyboardInfo>? KeyUpEvent;

		/// <summary>
		/// Fires the KeyDown event with the given parameters.
		/// </summary>
		/// <param name="keyboardInfo">Details about the key pressed, along with modifier key states.</param>
		void KeyDown(KeyboardInfo keyboardInfo);

		/// <summary>
		/// Fires the KeyUp event with the given parameters.
		/// </summary>
		/// <param name="keyboardInfo">Details about the key pressed, along with modifier key states.</param>
		void KeyUp(KeyboardInfo keyboardInfo);
	}
}
