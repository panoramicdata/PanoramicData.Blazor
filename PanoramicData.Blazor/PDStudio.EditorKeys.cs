namespace PanoramicData.Blazor;

/// <summary>
/// Keys the host asks the code editor to leave alone, so that a page shortcut works from inside the editor.
/// </summary>
public partial class PDStudio
{
	private readonly List<(int KeyCode, bool CtrlKey, bool AltKey, bool ShiftKey)> _editorKeysLeftAlone = [];

	/// <summary>
	/// Stops the code editor acting on a key, while the key still reaches the page's shortcuts, as PDStudio does for its own
	/// Ctrl+Enter. Use it for a page shortcut the editor would otherwise take, such as Ctrl+Alt+O, which on a keyboard
	/// layout with AltGr types a character. Asked before the editor exists, it is applied once it does.
	/// </summary>
	/// <param name="keyCode">The key's code, such as 79 for O.</param>
	/// <param name="ctrlKey">Whether Ctrl is held.</param>
	/// <param name="altKey">Whether Alt is held.</param>
	/// <param name="shiftKey">Whether Shift is held.</param>
	public Task DisableEditorKeyAsync(int keyCode, bool ctrlKey, bool altKey, bool shiftKey)
	{
		_editorKeysLeftAlone.Add((keyCode, ctrlKey, altKey, shiftKey));
		return RunOnEditorAsync(e => e.DisableKeyBindingAsync(keyCode, ctrlKey, altKey, shiftKey), "leave a key to the page");
	}

	/// <summary>
	/// Stops the code editor acting on a shortcut's key, while the key still reaches the page's shortcuts. A shortcut whose
	/// key is not a letter, a digit or a function key is left as it is.
	/// </summary>
	/// <param name="shortcutKey">The shortcut, such as a menu item's.</param>
	public Task DisableEditorKeyAsync(ShortcutKey shortcutKey)
	{
		ArgumentNullException.ThrowIfNull(shortcutKey);
		return EditorKeyCode(shortcutKey) is { } keyCode
			? DisableEditorKeyAsync(keyCode, shortcutKey.CtrlKey, shortcutKey.AltKey, shortcutKey.ShiftKey)
			: Task.CompletedTask;
	}

	// Asked before the editor existed, they had nothing to go to
	private async Task LeaveEditorKeysAloneAsync()
	{
		foreach (var (keyCode, ctrlKey, altKey, shiftKey) in _editorKeysLeftAlone.ToList())
		{
			await RunOnEditorAsync(e => e.DisableKeyBindingAsync(keyCode, ctrlKey, altKey, shiftKey), "leave a key to the page");
		}
	}

	// The editor's key codes: A to Z are 65 to 90, 0 to 9 are 48 to 57, F1 to F12 are 112 to 123
	internal static int? EditorKeyCode(ShortcutKey shortcutKey)
	{
		var name = shortcutKey.Code.Length > 0 ? shortcutKey.Code : shortcutKey.Key;
		if (name.Length == 1 && char.IsAsciiLetterOrDigit(name[0]))
		{
			return char.ToUpperInvariant(name[0]);
		}

		if (name.Length == 4 && name.StartsWith("Key", StringComparison.OrdinalIgnoreCase) && char.IsAsciiLetter(name[3]))
		{
			return char.ToUpperInvariant(name[3]);
		}

		if (name.Length == 6 && name.StartsWith("Digit", StringComparison.OrdinalIgnoreCase) && char.IsAsciiDigit(name[5]))
		{
			return name[5];
		}

		return name.Length is 2 or 3 && (name[0] is 'F' or 'f') && int.TryParse(name[1..], NumberStyles.None, CultureInfo.InvariantCulture, out var function) && function is >= 1 and <= 12
			? 111 + function
			: null;
	}
}
