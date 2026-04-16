using PanoramicData.Blazor.Models;
using Shouldly;

namespace PanoramicData.Blazor.Test;

public class ShortcutKeyTests
{
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

    [Fact]
    public void WhenCreatingFromSingleCharThenKeyIsSet()
    {
        var sk = ShortcutKey.Create("ctrl-a");

        sk.CtrlKey.ShouldBeTrue();
        sk.Key.ShouldBe("a");
        sk.Code.ShouldBeEmpty();
    }

    [Fact]
    public void WhenCreatingFromCtrlShiftAltThenAllModifiersSet()
    {
        var sk = ShortcutKey.Create("ctrl-shift-alt-F5");

        sk.CtrlKey.ShouldBeTrue();
        sk.ShiftKey.ShouldBeTrue();
        sk.AltKey.ShouldBeTrue();
        sk.Code.ShouldBe("F5");
    }

    [Fact]
    public void WhenCreatingFromEmptyStringThenHasValueIsFalse()
    {
        var sk = ShortcutKey.Create("");

        sk.HasValue.ShouldBeFalse();
    }

    [Fact]
    public void WhenCreatingFromValidStringThenHasValueIsTrue()
    {
        var sk = ShortcutKey.Create("ctrl-s");

        sk.HasValue.ShouldBeTrue();
    }

    [Fact]
    public void WhenToStringWithCtrlAndKeyCodeThenFormatsCorrectly()
    {
        var sk = new ShortcutKey { CtrlKey = true, Code = "KeyS" };

        sk.ToString().ShouldBe("Ctrl-S");
    }

    [Fact]
    public void WhenToStringWithDigitCodeThenStripsPrefix()
    {
        var sk = new ShortcutKey { CtrlKey = true, Code = "Digit1" };

        sk.ToString().ShouldBe("Ctrl-1");
    }

    [Fact]
    public void WhenToStringWithNoKeyOrCodeThenReturnsEmpty()
    {
        var sk = new ShortcutKey();

        sk.ToString().ShouldBeEmpty();
    }

    [Fact]
    public void WhenIsMatchWithMatchingKeyThenReturnsTrue()
    {
        var sk = ShortcutKey.Create("ctrl-s");

        sk.IsMatch("s", "", altKey: false, ctrlKey: true, shiftKey: false).ShouldBeTrue();
    }

    [Fact]
    public void WhenIsMatchWithWrongModifierThenReturnsFalse()
    {
        var sk = ShortcutKey.Create("ctrl-s");

        sk.IsMatch("s", "", altKey: false, ctrlKey: false, shiftKey: false).ShouldBeFalse();
    }

    [Fact]
    public void WhenIsMatchWithShortcutKeyOverloadThenReturnsTrue()
    {
        var sk1 = ShortcutKey.Create("ctrl-enter");
        var sk2 = ShortcutKey.Create("ctrl-enter");

        sk1.IsMatch(sk2).ShouldBeTrue();
    }

    [Fact]
    public void WhenExplicitCastToStringThenReturnsToString()
    {
        var sk = ShortcutKey.Create("ctrl-s");

        var result = (string)sk;

        result.ShouldBe("Ctrl-S");
    }

    [Fact]
    public void WhenExplicitCastFromStringThenCreatesShortcutKey()
    {
        var sk = (ShortcutKey)"ctrl-shift-a";

        sk.CtrlKey.ShouldBeTrue();
        sk.ShiftKey.ShouldBeTrue();
        sk.Key.ShouldBe("a");
    }
}
