namespace PanoramicData.Blazor.Interfaces;

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
	/// Event raised whenever the registered shortcuts are changed.
	/// </summary>
	event EventHandler<IEnumerable<ShortcutKey>>? ShortcutsChanged;

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

	/// <summary>
	/// Registers a shortcut key to listen for.
	/// </summary>
	/// <param name="shortcut">Details of the shortcut key combination.</param>
	void RegisterShortcutKey(ShortcutKey shortcut);

	/// <summary>
	/// Unregisters a shortcut key to listen for.
	/// </summary>
	/// <param name="shortcut">Details of the shortcut key combination.</param>
	void UnregisterShortcutKey(ShortcutKey shortcut);

	/// <summary>
	/// Gets the currently registered shortcut keys.
	/// </summary>
	IEnumerable<ShortcutKey> GetRegisteredShortcuts();
}
