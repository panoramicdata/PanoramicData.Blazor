using PanoramicData.Blazor.Models;
using Shouldly;

namespace PanoramicData.Blazor.Test;

/// <summary>Tests for the ShortcutKey class.</summary>
public class ShortcutKeyTests
{
    /// <summary>Verifies that creating a shortcut from "ctrl-enter" sets CtrlKey to true, Code to "enter", and clears other modifiers.</summary>
    [Fact]
    public void WhenCreatingFromCtrlEnterThenPropertiesAreSet()
    {
        var sk = ShortcutKey.Create("ctrl-enter");

        sk.CtrlKey.ShouldBeTrue();
        sk.AltKey.ShouldBeFalse();
        sk.ShiftKey.ShouldBeFalse();
        sk.Code.ShouldBe("enter");
        sk.Key.ShouldBeEmpty();
    }

    /// <summary>Verifies that creating a shortcut from a single character sets the Key property.</summary>
    [Fact]
    public void WhenCreatingFromSingleCharThenKeyIsSet()
    {
        var sk = ShortcutKey.Create("ctrl-a");

        sk.CtrlKey.ShouldBeTrue();
        sk.Key.ShouldBe("a");
        sk.Code.ShouldBeEmpty();
    }

    /// <summary>Verifies that creating a shortcut from "ctrl-shift-alt-F5" sets all three modifier keys and Code to "F5".</summary>
    [Fact]
    public void WhenCreatingFromCtrlShiftAltThenAllModifiersSet()
    {
        var sk = ShortcutKey.Create("ctrl-shift-alt-F5");

        sk.CtrlKey.ShouldBeTrue();
        sk.ShiftKey.ShouldBeTrue();
        sk.AltKey.ShouldBeTrue();
        sk.Code.ShouldBe("F5");
    }

    /// <summary>Verifies that creating a shortcut from an empty string produces a shortcut with HasValue false.</summary>
    [Fact]
    public void WhenCreatingFromEmptyStringThenHasValueIsFalse()
    {
        var sk = ShortcutKey.Create("");

        sk.HasValue.ShouldBeFalse();
    }

    /// <summary>Verifies that creating a shortcut from a valid string produces a shortcut with HasValue true.</summary>
    [Fact]
    public void WhenCreatingFromValidStringThenHasValueIsTrue()
    {
        var sk = ShortcutKey.Create("ctrl-s");

        sk.HasValue.ShouldBeTrue();
    }

    /// <summary>Verifies that ToString formats a Ctrl+KeyS shortcut as "Ctrl-S".</summary>
    [Fact]
    public void WhenToStringWithCtrlAndKeyCodeThenFormatsCorrectly()
    {
        var sk = new ShortcutKey { CtrlKey = true, Code = "KeyS" };

        sk.ToString().ShouldBe("Ctrl-S");
    }

    /// <summary>Verifies that ToString strips the "Digit" prefix from digit key codes.</summary>
    [Fact]
    public void WhenToStringWithDigitCodeThenStripsPrefix()
    {
        var sk = new ShortcutKey { CtrlKey = true, Code = "Digit1" };

        sk.ToString().ShouldBe("Ctrl-1");
    }

    /// <summary>Verifies that ToString returns an empty string when no key or code is set.</summary>
    [Fact]
    public void WhenToStringWithNoKeyOrCodeThenReturnsEmpty()
    {
        var sk = new ShortcutKey();

        sk.ToString().ShouldBeEmpty();
    }

    /// <summary>Verifies that IsMatch returns true when the key and modifiers match the shortcut.</summary>
    [Fact]
    public void WhenIsMatchWithMatchingKeyThenReturnsTrue()
    {
        var sk = ShortcutKey.Create("ctrl-s");

        sk.IsMatch("s", "", altKey: false, ctrlKey: true, shiftKey: false).ShouldBeTrue();
    }

    /// <summary>Verifies that IsMatch returns false when the modifier keys do not match the shortcut.</summary>
    [Fact]
    public void WhenIsMatchWithWrongModifierThenReturnsFalse()
    {
        var sk = ShortcutKey.Create("ctrl-s");

        sk.IsMatch("s", "", altKey: false, ctrlKey: false, shiftKey: false).ShouldBeFalse();
    }

    /// <summary>Verifies that the ShortcutKey overload of IsMatch returns true for two identical shortcut keys.</summary>
    [Fact]
    public void WhenIsMatchWithShortcutKeyOverloadThenReturnsTrue()
    {
        var sk1 = ShortcutKey.Create("ctrl-enter");
        var sk2 = ShortcutKey.Create("ctrl-enter");

        sk1.IsMatch(sk2).ShouldBeTrue();
    }

    /// <summary>Verifies that explicitly casting a ShortcutKey to string returns the same result as ToString.</summary>
    [Fact]
    public void WhenExplicitCastToStringThenReturnsToString()
    {
        var sk = ShortcutKey.Create("ctrl-s");

        var result = (string)sk;

        result.ShouldBe("Ctrl-S");
    }

    /// <summary>Verifies that explicitly casting a string to ShortcutKey creates a correctly configured shortcut.</summary>
    [Fact]
    public void WhenExplicitCastFromStringThenCreatesShortcutKey()
    {
        var sk = (ShortcutKey)"ctrl-shift-a";

        sk.CtrlKey.ShouldBeTrue();
        sk.ShiftKey.ShouldBeTrue();
        sk.Key.ShouldBe("a");
    }
}
