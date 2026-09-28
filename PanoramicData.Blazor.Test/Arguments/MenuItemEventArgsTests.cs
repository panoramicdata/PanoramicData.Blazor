using AwesomeAssertions;
using PanoramicData.Blazor.Arguments;
using PanoramicData.Blazor.Models;

namespace PanoramicData.Blazor.Test.Arguments;

/// <summary>Tests for <see cref="MenuItemEventArgs"/>.</summary>
public class MenuItemEventArgsTests
{
	/// <summary>The constructor captures the sender and menu item, and the event is not cancelled.</summary>
	[Fact]
	public void Constructor_CapturesSenderAndItem()
	{
		var sender = new object();
		var item = new MenuItem("copy", "Copy", "fa-copy");

		var args = new MenuItemEventArgs(sender, item);

		args.Sender.Should().BeSameAs(sender);
		args.MenuItem.Should().BeSameAs(item);
		args.Cancel.Should().BeFalse();
	}

	/// <summary>A handler can cancel the click.</summary>
	[Fact]
	public void Cancel_IsSettable()
	{
		var args = new MenuItemEventArgs(this, new MenuItem()) { Cancel = true };

		args.Cancel.Should().BeTrue();
	}
}
