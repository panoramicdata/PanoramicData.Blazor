using System;
using System.Collections.Generic;
using System.Linq;

namespace PanoramicData.Blazor.Services
{
	/// <summary>
	/// The GlobalEventService provides a concrete implementation of the IGlobalEventService interface.
	/// </summary>
	public class GlobalEventService : IGlobalEventService
	{
		private readonly Dictionary<string, ShortcutKey> _registeredShortcuts = new Dictionary<string, ShortcutKey>();

		/// <summary>
		/// Event raised whenever the KeyDown event occurs.
		/// </summary>
		public event EventHandler<KeyboardInfo>? KeyDownEvent;

		/// <summary>
		/// Event raised whenever the KeyUp event occurs.
		/// </summary>
		public event EventHandler<KeyboardInfo>? KeyUpEvent;

		/// <summary>
		/// Event raised whenever the registered shortcuts are changed.
		/// </summary>
		public event EventHandler<IEnumerable<ShortcutKey>>? ShortcutsChanged;

		/// <summary>
		/// Fires the KeyDown event with the given parameters.
		/// </summary>
		/// <param name="keyboardInfo">Details about the key pressed, along with modifier key states.</param>
		public void KeyDown(KeyboardInfo keyboardInfo)
		{
			KeyDownEvent?.Invoke(this, keyboardInfo);
		}

		/// <summary>
		/// Fires the KeyUp event with the given parameters.
		/// </summary>
		/// <param name="keyboardInfo">Details about the key pressed, along with modifier key states.</param>
		public void KeyUp(KeyboardInfo keyboardInfo)
		{
			KeyUpEvent?.Invoke(this, keyboardInfo);
		}

		/// <summary>
		/// Registers a shortcut key to listen for.
		/// </summary>
		/// <param name="shortcut">Details of the shortcut key combination.</param>
		public void RegisterShortcutKey(ShortcutKey shortcut)
		{
			if (!_registeredShortcuts.ContainsKey(shortcut.ToString()))
			{
				_registeredShortcuts.Add(shortcut.ToString(), shortcut);
				ShortcutsChanged?.Invoke(this, _registeredShortcuts.Values.ToArray());
			}
		}

		/// <summary>
		/// Unregisters a shortcut key to listen for.
		/// </summary>
		/// <param name="shortcut">Details of the shortcut key combination.</param>
		public void UnregisterShortcutKey(ShortcutKey shortcut)
		{
			if (_registeredShortcuts.ContainsKey(shortcut.ToString()))
			{
				_registeredShortcuts.Remove(shortcut.ToString());
				ShortcutsChanged?.Invoke(this, _registeredShortcuts.Values.ToArray());
			}
		}
	}
}
