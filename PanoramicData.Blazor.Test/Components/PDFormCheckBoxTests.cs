using AwesomeAssertions;
using Bunit;
using Microsoft.AspNetCore.Components.Web;

namespace PanoramicData.Blazor.Test;

/// <summary>
/// Tests that <see cref="PDFormCheckBox"/> toggles by mouse and keyboard, reports each change, and ignores
/// input while disabled.
/// </summary>
public class PDFormCheckBoxTests : BunitContext
{
	/// <summary>Verifies that clicking an unchecked box checks it and reports the new value.</summary>
	[Fact]
	public void Clicking_toggles_the_value_and_raises_ValueChanged()
	{
		var reported = new List<bool>();
		var component = Render<PDFormCheckBox>(parameters => parameters
			.Add(p => p.ValueChanged, (bool value) => reported.Add(value)));

		component.Find("i.fa-square").Click();

		reported.Should().Equal(true);
		component.Find("i").ClassList.Should().Contain("fa-check-square");
	}

	/// <summary>Verifies that clicking a checked box unchecks it.</summary>
	[Fact]
	public void Clicking_a_checked_box_unchecks_it()
	{
		var reported = new List<bool>();
		var component = Render<PDFormCheckBox>(parameters => parameters
			.Add(p => p.Value, true)
			.Add(p => p.ValueChanged, (bool value) => reported.Add(value)));

		component.Find("i.fa-check-square").Click();

		reported.Should().Equal(false);
		component.Find("i").ClassList.Should().Contain("fa-square");
	}

	/// <summary>Verifies that Space and Enter both toggle the value, so the box is keyboard-operable.</summary>
	[Theory]
	[InlineData("Space")]
	[InlineData("Enter")]
	public void Space_and_Enter_toggle_the_value(string code)
	{
		var reported = new List<bool>();
		var component = Render<PDFormCheckBox>(parameters => parameters
			.Add(p => p.ValueChanged, (bool value) => reported.Add(value)));

		component.Find(".pdformcheckbox").KeyPress(new KeyboardEventArgs { Code = code });

		reported.Should().Equal(true);
	}

	/// <summary>Verifies that other keys do not toggle the value.</summary>
	[Fact]
	public void Other_keys_do_not_toggle_the_value()
	{
		var reported = new List<bool>();
		var component = Render<PDFormCheckBox>(parameters => parameters
			.Add(p => p.ValueChanged, (bool value) => reported.Add(value)));

		component.Find(".pdformcheckbox").KeyPress(new KeyboardEventArgs { Code = "KeyA" });

		reported.Should().BeEmpty();
		component.Find("i").ClassList.Should().Contain("fa-square");
	}

	/// <summary>
	/// Verifies that a disabled box ignores both click and keyboard, is marked disabled and is taken out of
	/// the tab order.
	/// </summary>
	[Fact]
	public void A_disabled_box_ignores_input_and_leaves_the_tab_order()
	{
		var reported = new List<bool>();
		var component = Render<PDFormCheckBox>(parameters => parameters
			.Add(p => p.Disabled, true)
			.Add(p => p.ValueChanged, (bool value) => reported.Add(value)));

		component.Find("i").Click();
		component.Find(".pdformcheckbox").KeyPress(new KeyboardEventArgs { Code = "Space" });

		reported.Should().BeEmpty();
		var root = component.Find(".pdformcheckbox");
		root.ClassList.Should().Contain("disabled");
		root.HasAttribute("tabindex").Should().BeFalse();
	}

	/// <summary>Verifies that an enabled box is focusable and carries any extra CSS class.</summary>
	[Fact]
	public void An_enabled_box_is_focusable_and_carries_the_css_class()
	{
		var component = Render<PDFormCheckBox>(parameters => parameters
			.Add(p => p.CssClass, "extra"));

		var root = component.Find(".pdformcheckbox");
		root.GetAttribute("tabindex").Should().Be("0");
		root.ClassList.Should().Contain("extra").And.NotContain("disabled");
	}

	/// <summary>Verifies that the label follows the box by default.</summary>
	[Fact]
	public void The_label_follows_the_box_by_default()
	{
		var component = Render<PDFormCheckBox>(parameters => parameters
			.Add(p => p.Label, "Accept"));

		var children = component.Find(".pdformcheckbox").Children;
		children.Should().HaveCount(2);
		children[0].TagName.Should().Be("I");
		children[1].TextContent.Should().Be("Accept");
		children[1].ClassList.Should().Contain("ms-1");
	}

	/// <summary>Verifies that LabelBefore puts the label ahead of the box.</summary>
	[Fact]
	public void LabelBefore_puts_the_label_ahead_of_the_box()
	{
		var component = Render<PDFormCheckBox>(parameters => parameters
			.Add(p => p.Label, "Accept")
			.Add(p => p.LabelBefore, true));

		var children = component.Find(".pdformcheckbox").Children;
		children.Should().HaveCount(2);
		children[0].TextContent.Should().Be("Accept");
		children[0].ClassList.Should().Contain("me-1");
		children[1].TagName.Should().Be("I");
	}

	/// <summary>Verifies that a blank label renders no label element at all.</summary>
	[Fact]
	public void A_blank_label_renders_no_label()
	{
		var component = Render<PDFormCheckBox>(parameters => parameters
			.Add(p => p.Label, "  "));

		component.FindAll("span.label").Should().BeEmpty();
	}
}
