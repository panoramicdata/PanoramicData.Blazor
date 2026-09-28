using AwesomeAssertions;
using Microsoft.AspNetCore.Components.Web;
using PanoramicData.Blazor.Models;

namespace PanoramicData.Blazor.Test.Models;

/// <summary>Tests for <see cref="ToolbarTextBox"/>.</summary>
public class ToolbarTextBoxTests
{
	/// <summary>A new text box is auto width, shows a clear button and has no handlers or debounce.</summary>
	[Fact]
	public void New_HasDocumentedDefaults()
	{
		var textBox = new ToolbarTextBox();

		textBox.CssClass.Should().BeEmpty();
		textBox.ItemCssClass.Should().BeEmpty();
		textBox.KeypressEvent.Should().BeFalse();
		textBox.Width.Should().Be("Auto");
		textBox.Value.Should().BeEmpty();
		textBox.ValueChanged.Should().BeNull();
		textBox.Keypress.Should().BeNull();
		textBox.ShowClearButton.Should().BeTrue();
		textBox.Cleared.Should().BeNull();
		textBox.Label.Should().BeEmpty();
		textBox.DebounceWait.Should().Be(0);
	}

	/// <summary>All members round-trip, and the handlers are invoked as supplied.</summary>
	[Fact]
	public void SettableMembers_RoundTrip()
	{
		string? changed = null;
		string? pressed = null;
		var cleared = false;

		var textBox = new ToolbarTextBox
		{
			CssClass = "search",
			ItemCssClass = "item",
			KeypressEvent = true,
			Width = "200px",
			Value = "abc",
			ValueChanged = v => changed = v,
			Keypress = e => pressed = e.Key,
			ShowClearButton = false,
			Cleared = () => cleared = true,
			Label = "Find",
			DebounceWait = 250
		};

		textBox.ValueChanged("xyz");
		textBox.Keypress(new KeyboardEventArgs { Key = "Enter" });
		textBox.Cleared();

		changed.Should().Be("xyz");
		pressed.Should().Be("Enter");
		cleared.Should().BeTrue();
		textBox.CssClass.Should().Be("search");
		textBox.ItemCssClass.Should().Be("item");
		textBox.KeypressEvent.Should().BeTrue();
		textBox.Width.Should().Be("200px");
		textBox.Value.Should().Be("abc");
		textBox.ShowClearButton.Should().BeFalse();
		textBox.Label.Should().Be("Find");
		textBox.DebounceWait.Should().Be(250);
	}
}
