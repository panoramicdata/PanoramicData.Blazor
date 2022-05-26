namespace PanoramicData.Blazor.Models;

public class ShortcutKey
{
	/// <summary>
	/// Gets or sets the string key code to match.
	/// </summary>
	public string Code { get; set; } = string.Empty;

	/// <summary>
	/// Gets or sets the key to match.
	/// </summary>
	public string Key { get; set; } = string.Empty;

	/// <summary>
	/// Gets or sets whether the alt key should be pressed.
	/// </summary>
	public bool AltKey { get; set; }

	/// <summary>
	/// Gets or sets whether the control key should be pressed.
	/// </summary>
	public bool CtrlKey { get; set; }

	/// <summary>
	/// Gets or sets whether the shift key should be pressed.
	/// </summary>
	public bool ShiftKey { get; set; }

	/// <summary>
	/// Returns the abbreviation of the shortcut key.
	/// </summary>
	/// <returns>A new string instance containing the shortcut abbreviation.</returns>
	public override string ToString()
	{
		if (string.IsNullOrWhiteSpace(Key) && string.IsNullOrWhiteSpace(Code))
		{
			return string.Empty;
		}
		var sb = new StringBuilder();
		if (CtrlKey)
		{
			sb.Append("Ctrl-");
		}
		if (ShiftKey)
		{
			sb.Append("Shift-");
		}
		if (AltKey)
		{
			sb.Append("Alt-");
		}
		if (Code.Length > 0 && Code.StartsWith("key", StringComparison.OrdinalIgnoreCase))
		{
			sb.Append(Code.Substring(3).ToUpper());
		}
		else if (Code.Length > 0 && Code.StartsWith("digit", StringComparison.OrdinalIgnoreCase))
		{
			sb.Append(Code.Substring(5).ToUpper());
		}
		else if (Code.Length > 0)
		{
			sb.Append(Code.ToUpper());
		}
		else
			sb.Append(Key.ToUpper());

		return sb.ToString();
	}

	/// <summary>
	/// Determines whether this shortcut key is a match with the given arguments.
	/// </summary>
	/// <param name="key">The key pressed.</param>
	/// <param name="code">String key code of the key pressed</param>
	/// <param name="altKey">Whether the alt key was also pressed.</param>
	/// <param name="ctrlKey">Whether the control key was also pressed.</param>
	/// <param name="shiftKey">Whether the shift key was also pressed.</param>
	/// <returns>true if it is a match, otherwise false.</returns>
	public bool IsMatch(string key, string code, bool altKey, bool ctrlKey, bool shiftKey)
	{
		return AltKey == altKey &&
			CtrlKey == ctrlKey &&
			ShiftKey == shiftKey &&
			(string.Equals(Key, key, StringComparison.OrdinalIgnoreCase) ||
			 string.Equals(Code, code, StringComparison.OrdinalIgnoreCase));
	}

	/// <summary>
	/// Determines whether this shortcut key is a match with the given shortcut key.
	/// </summary>
	/// <param name="shortcutKey">ShortcutKey instance to match with.</param>
	/// <returns>true if it is a match, otherwise false.</returns>
	public bool IsMatch(ShortcutKey shortcutKey)
	{
		return AltKey == shortcutKey.AltKey &&
			CtrlKey == shortcutKey.CtrlKey &&
			ShiftKey == shortcutKey.ShiftKey &&
			(string.Equals(Key, shortcutKey.Key, StringComparison.OrdinalIgnoreCase) ||
			 string.Equals(Code, shortcutKey.Code, StringComparison.OrdinalIgnoreCase));
	}

	/// <summary>
	/// Returns whether this instance represents a valid shortcut key.
	/// </summary>
	/// <returns>true if the shortcut key represents a valid value, otherwise false.</returns>
	public bool HasValue => !string.IsNullOrWhiteSpace(Key) || !string.IsNullOrWhiteSpace(Code);

	/// <summary>
	/// Creates a ShortcutKey instance from the given shortcut abbreviation.
	/// </summary>
	/// <param name="shortcutKey">Shortcut abbreviation.</param>
	/// <returns>A new ShortcutKey instance</returns>
	public static ShortcutKey Create(string shortcutKey)
	{
		if (string.IsNullOrWhiteSpace(shortcutKey))
		{
			return new ShortcutKey();
		}
		var codes = shortcutKey.Split(new[] { '-' }, StringSplitOptions.RemoveEmptyEntries);
		var lastCode = codes.LastOrDefault();
		return new ShortcutKey
		{
			AltKey = codes.Any(x => string.Equals(x, "alt", StringComparison.OrdinalIgnoreCase)),
			CtrlKey = codes.Any(x => string.Equals(x, "ctrl", StringComparison.OrdinalIgnoreCase)),
			ShiftKey = codes.Any(x => string.Equals(x, "shift", StringComparison.OrdinalIgnoreCase)),
			Code = lastCode?.Length > 1 ? lastCode : string.Empty,   // KeyA or Digit1 or Quote etc
			Key = lastCode?.Length == 1 ? lastCode : string.Empty       // a or b etc
		};
	}

	public static explicit operator string(ShortcutKey shortcut) => shortcut.ToString();
	public static explicit operator ShortcutKey(string text) => Create(text);
}
