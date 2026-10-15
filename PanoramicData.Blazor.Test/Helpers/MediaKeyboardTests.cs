using AwesomeAssertions;
using Microsoft.AspNetCore.Components.Web;
using PanoramicData.Blazor.Helpers;

namespace PanoramicData.Blazor.Test.Helpers;

/// <summary>Tests for <see cref="MediaKeyboard"/>.</summary>
public class MediaKeyboardTests
{
	/// <summary>The documented shortcut keys map to their actions; modified, repeated and other keys do nothing.</summary>
	[Theory]
	[InlineData("k", MediaShortcutCase.TogglePlay)]
	[InlineData("K", MediaShortcutCase.TogglePlay)]
	[InlineData("j", MediaShortcutCase.PreviousMarker)]
	[InlineData("J", MediaShortcutCase.PreviousMarker)]
	[InlineData("[", MediaShortcutCase.PreviousMarker)]
	[InlineData("l", MediaShortcutCase.NextMarker)]
	[InlineData("L", MediaShortcutCase.NextMarker)]
	[InlineData("]", MediaShortcutCase.NextMarker)]
	[InlineData("m", MediaShortcutCase.ToggleMute)]
	[InlineData("M", MediaShortcutCase.ToggleMute)]
	[InlineData("<", MediaShortcutCase.Slower)]
	[InlineData(">", MediaShortcutCase.Faster)]
	[InlineData("x", MediaShortcutCase.None)]
	[InlineData(" ", MediaShortcutCase.None)]
	public void Keyboard_MapsTheShortcutKeys(string key, MediaShortcutCase expected)
		=> ((int)MediaKeyboard.Resolve(new KeyboardEventArgs { Key = key })).Should().Be((int)expected);

	/// <summary>Keys pressed with a modifier, or repeated by holding them down, are not shortcuts.</summary>
	[Fact]
	public void Keyboard_IgnoresModifiedAndRepeatedKeys()
	{
		MediaKeyboard.Resolve(new KeyboardEventArgs { Key = "k", CtrlKey = true }).Should().Be(MediaShortcut.None);
		MediaKeyboard.Resolve(new KeyboardEventArgs { Key = "k", AltKey = true }).Should().Be(MediaShortcut.None);
		MediaKeyboard.Resolve(new KeyboardEventArgs { Key = "k", MetaKey = true }).Should().Be(MediaShortcut.None);
		MediaKeyboard.Resolve(new KeyboardEventArgs { Key = "k", Repeat = true }).Should().Be(MediaShortcut.None);
		MediaKeyboard.Resolve(new KeyboardEventArgs { Key = ">", ShiftKey = true }).Should().Be(MediaShortcut.Faster);
		new Action(() => MediaKeyboard.Resolve(null!)).Should().Throw<ArgumentNullException>();
	}

	/// <summary>A public mirror of the internal shortcut enum, so it can appear in theory data.</summary>
	public enum MediaShortcutCase
	{
		/// <summary>See <see cref="MediaShortcut.None"/>.</summary>
		None,

		/// <summary>See <see cref="MediaShortcut.TogglePlay"/>.</summary>
		TogglePlay,

		/// <summary>See <see cref="MediaShortcut.PreviousMarker"/>.</summary>
		PreviousMarker,

		/// <summary>See <see cref="MediaShortcut.NextMarker"/>.</summary>
		NextMarker,

		/// <summary>See <see cref="MediaShortcut.ToggleMute"/>.</summary>
		ToggleMute,

		/// <summary>See <see cref="MediaShortcut.Slower"/>.</summary>
		Slower,

		/// <summary>See <see cref="MediaShortcut.Faster"/>.</summary>
		Faster
	}
}
