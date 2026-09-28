using AwesomeAssertions;
using PanoramicData.Blazor.Arguments;
using PanoramicData.Blazor.Models;

namespace PanoramicData.Blazor.Test.Arguments;

/// <summary>Tests for <see cref="MenuItemsEventArgs"/>.</summary>
public class MenuItemsEventArgsTests
{
	/// <summary>The constructor captures the sender and items; context and source element start empty.</summary>
	[Fact]
	public void Constructor_CapturesSenderAndItems()
	{
		var sender = new object();
		List<MenuItem> items = [new MenuItem { Key = "a" }];

		var args = new MenuItemsEventArgs(sender, items);

		args.Sender.Should().BeSameAs(sender);
		args.MenuItems.Should().BeSameAs(items);
		args.Context.Should().BeNull();
		args.SourceElement.Should().BeNull();
		args.Cancel.Should().BeFalse();
	}

	/// <summary>A handler can attach context and the element the menu was opened from, and alter the items.</summary>
	[Fact]
	public void SettableMembers_RoundTrip()
	{
		var element = new ElementInfo { Tag = "TD" };
		var args = new MenuItemsEventArgs(this, [])
		{
			Context = 42,
			SourceElement = element,
			Cancel = true
		};

		args.MenuItems.Add(new MenuItem { Key = "added" });

		args.Context.Should().Be(42);
		args.SourceElement.Should().BeSameAs(element);
		args.Cancel.Should().BeTrue();
		args.MenuItems.Should().ContainSingle().Which.Key.Should().Be("added");
	}
}
