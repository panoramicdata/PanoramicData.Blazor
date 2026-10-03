namespace PanoramicData.Blazor.Models;

/// <summary>
/// Describes a keyboard shortcut consisting of an optional modifier combination and a key or code.
/// </summary>
public class ShortcutKey
{
	// The prefixes of key codes that are dropped when displaying them, e.g. "KeyA" displays as "A"
	private static readonly string[] _displayedCodePrefixes = ["key", "digit"];

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
		if (!HasValue)
		{
			return string.Empty;
		}

		var modifiers = new (bool IsPressed, string Text)[] { (CtrlKey, "Ctrl-"), (ShiftKey, "Shift-"), (AltKey, "Alt-") }
			.Where(modifier => modifier.IsPressed)
			.Select(modifier => modifier.Text);
		return string.Concat(modifiers) + GetKeyText();
	}

	private string GetKeyText()
	{
		if (Code.Length == 0)
		{
			return Key.ToUpperInvariant();
		}

		var prefix = _displayedCodePrefixes.FirstOrDefault(p => Code.StartsWith(p, StringComparison.OrdinalIgnoreCase)) ?? string.Empty;
		return Code[prefix.Length..].ToUpperInvariant();
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
		=> (AltKey, CtrlKey, ShiftKey) == (altKey, ctrlKey, shiftKey) && IsKeyOrCodeMatch(key, code);

	/// <summary>
	/// Determines whether this shortcut key is a match with the given shortcut key.
	/// </summary>
	/// <param name="shortcutKey">ShortcutKey instance to match with.</param>
	/// <returns>true if it is a match, otherwise false.</returns>
	public bool IsMatch(ShortcutKey shortcutKey)
		=> IsMatch(shortcutKey.Key, shortcutKey.Code, shortcutKey.AltKey, shortcutKey.CtrlKey, shortcutKey.ShiftKey);

	/// <summary>
	/// Matches on Key or on Code, but only on a value this shortcut actually has: an empty Key or Code is "not
	/// part of this shortcut", not a value to compare, so it must not match an event whose Key or Code is also
	/// empty (#170).
	/// </summary>
	private bool IsKeyOrCodeMatch(string key, string code) =>
		(!string.IsNullOrEmpty(Key) && string.Equals(Key, key, StringComparison.OrdinalIgnoreCase)) ||
		(!string.IsNullOrEmpty(Code) && string.Equals(Code, code, StringComparison.OrdinalIgnoreCase));

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

		var codes = shortcutKey.Split(['-'], StringSplitOptions.RemoveEmptyEntries);
		var lastCode = codes.LastOrDefault() ?? string.Empty;
		return new ShortcutKey
		{
			AltKey = codes.Any(x => string.Equals(x, "alt", StringComparison.OrdinalIgnoreCase)),
			CtrlKey = codes.Any(x => string.Equals(x, "ctrl", StringComparison.OrdinalIgnoreCase)),
			ShiftKey = codes.Any(x => string.Equals(x, "shift", StringComparison.OrdinalIgnoreCase)),
			Code = lastCode.Length > 1 ? lastCode : string.Empty,   // KeyA or Digit1 or Quote etc
			Key = lastCode.Length == 1 ? lastCode : string.Empty       // a or b etc
		};
	}

	/// <summary>Explicitly converts a <see cref="ShortcutKey"/> to its string representation by calling <see cref="ToString"/>.</summary>
	public static explicit operator string(ShortcutKey shortcut) => shortcut.ToString();
	/// <summary>Explicitly creates a <see cref="ShortcutKey"/> from a shortcut abbreviation string by calling <see cref="Create"/>.</summary>
	public static explicit operator ShortcutKey(string text) => Create(text);
}
