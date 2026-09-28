using AwesomeAssertions;
using PanoramicData.Blazor.Models;
using PanoramicData.Blazor.Services;

namespace PanoramicData.Blazor.Test.Services;

/// <summary>Tests for <see cref="GlobalEventService"/>.</summary>
public class GlobalEventServiceTests
{
	private static ShortcutKey CtrlS() => new() { CtrlKey = true, Code = "KeyS", Key = "s" };

	/// <summary>Key down and key up are forwarded to their events with the service as sender.</summary>
	[Fact]
	public void KeyDownAndKeyUp_RaiseEvents()
	{
		var service = new GlobalEventService();
		var info = new KeyboardInfo { Key = "a" };
		var events = new List<string>();
		service.KeyDownEvent += (s, k) => { s.Should().BeSameAs(service); events.Add($"down {k.Key}"); };
		service.KeyUpEvent += (_, k) => events.Add($"up {k.Key}");

		service.KeyDown(info);
		service.KeyUp(info);

		events.Should().Equal("down a", "up a");
	}

	/// <summary>Key events with no subscribers do nothing.</summary>
	[Fact]
	public void KeyEvents_NoSubscribers_DoNotThrow()
	{
		var service = new GlobalEventService();

		var act = () =>
		{
			service.KeyDown(new KeyboardInfo());
			service.KeyUp(new KeyboardInfo());
			service.RegisterShortcutKey(CtrlS());
			service.UnregisterShortcutKey(CtrlS());
		};

		act.Should().NotThrow();
	}

	/// <summary>Registering a shortcut records it and announces the new set; registering it again changes nothing.</summary>
	[Fact]
	public void RegisterShortcutKey_AddsOnce()
	{
		var service = new GlobalEventService();
		var announcements = new List<int>();
		service.ShortcutsChanged += (_, shortcuts) => announcements.Add(shortcuts.Count());

		service.RegisterShortcutKey(CtrlS());
		service.RegisterShortcutKey(CtrlS());

		service.GetRegisteredShortcuts().Should().ContainSingle().Which.ToString().Should().Be("Ctrl-S");
		announcements.Should().Equal(1);
	}

	/// <summary>Unregistering removes a registered shortcut and announces the change; an unknown one is ignored.</summary>
	[Fact]
	public void UnregisterShortcutKey_RemovesRegistered()
	{
		var service = new GlobalEventService();
		service.RegisterShortcutKey(CtrlS());
		var announcements = new List<int>();
		service.ShortcutsChanged += (_, shortcuts) => announcements.Add(shortcuts.Count());

		service.UnregisterShortcutKey(new ShortcutKey { AltKey = true, Code = "KeyX" });
		service.UnregisterShortcutKey(CtrlS());

		service.GetRegisteredShortcuts().Should().BeEmpty();
		announcements.Should().Equal(0);
	}
}
