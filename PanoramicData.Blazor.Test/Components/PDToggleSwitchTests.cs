using AwesomeAssertions;
using Bunit;
using Microsoft.AspNetCore.Components.Web;
using PanoramicData.Blazor.Models;
using PanoramicData.Blazor.Options;

namespace PanoramicData.Blazor.Test;

/// <summary>
/// Tests that <see cref="PDToggleSwitch"/> sizes and positions its SVG parts from its parameters and
/// options, widens itself to fit measured on/off text, and toggles its value by click and keyboard.
/// </summary>
public class PDToggleSwitchTests : BunitContext
{
	private const string ModulePath = "./_content/PanoramicData.Blazor/PDToggleSwitch.razor.js";

	private readonly BunitJSModuleInterop _module;

	/// <summary>Sets up the rendering context and the text measurement module.</summary>
	public PDToggleSwitchTests()
	{
		JSInterop.Mode = JSRuntimeMode.Loose;
		_module = JSInterop.SetupModule(ModulePath);
	}

	/// <summary>
	/// Verifies the geometry of a default, medium, off switch with no text.
	/// </summary>
	[Fact]
	public void A_default_switch_has_medium_geometry_and_is_off()
	{
		var component = Render<PDToggleSwitch>();

		var root = component.Find(".pdtoggleswitch");
		root.ClassList.Should().Contain("md").And.NotContain("disabled").And.NotContain("rd");
		root.Id.Should().MatchRegex("^pd-toggleswitch-[0-9]+$");

		var svg = component.Find("svg");
		svg.GetAttribute("width").Should().Be("48");
		svg.GetAttribute("height").Should().Be("24");
		svg.GetAttribute("tabindex").Should().Be("0");

		AssertAttributes(component.Find("rect.switch"), ("class", "switch off"), ("height", "22"), ("width", "46"), ("x", "1"), ("y", "1"), ("rx", "0"));
		AssertAttributes(component.Find("rect.toggle"), ("class", "toggle off"), ("height", "18"), ("width", "20"), ("x", "3"), ("y", "3"));
		AssertAttributes(component.Find("text"), ("class", "text off"), ("text-anchor", "end"), ("x", "42"), ("y", "18"));
	}

	/// <summary>
	/// Verifies that an on switch moves the toggle to the right and anchors its text on the left.
	/// </summary>
	[Fact]
	public void An_on_switch_moves_the_toggle_right_and_shows_the_on_text()
	{
		var component = Render<PDToggleSwitch>(parameters => parameters
			.Add(p => p.Value, true)
			.Add(p => p.OnText, "Yes")
			.Add(p => p.OffText, "No"));

		AssertAttributes(component.Find("rect.toggle"), ("class", "toggle on"), ("x", "25"));
		AssertAttributes(component.Find("text"), ("class", "text on"), ("text-anchor", "start"), ("x", "6"));
		component.Find("text").TextContent.Should().Be("Yes");
		component.Find("rect.switch").GetAttribute("class").Should().Be("switch on");
	}

	/// <summary>
	/// Verifies the size-dependent class, height and text offset for the small and large sizes.
	/// </summary>
	[Theory]
	[InlineData(ButtonSizes.Small, "sm", "16", "32", "11")]
	[InlineData(ButtonSizes.Large, "lg", "32", "64", "25")]
	public void Sizes_set_the_class_height_width_and_text_offset(ButtonSizes size, string cssClass, string height, string width, string textY)
	{
		var component = Render<PDToggleSwitch>(parameters => parameters
			.Add(p => p.Size, size));

		component.Find(".pdtoggleswitch").ClassList.Should().Contain(cssClass);
		component.Find("svg").GetAttribute("height").Should().Be(height);
		component.Find("svg").GetAttribute("width").Should().Be(width);
		component.Find("text").GetAttribute("y").Should().Be(textY);
	}

	/// <summary>
	/// Verifies that rounded switches get the rounded class and corner radii of half their height.
	/// </summary>
	[Fact]
	public void A_rounded_switch_has_rounded_corners()
	{
		var component = Render<PDToggleSwitch>(parameters => parameters
			.Add(p => p.Rounded, true));

		component.Find(".pdtoggleswitch").ClassList.Should().Contain("rd");
		AssertAttributes(component.Find("rect.switch"), ("rx", "12"), ("ry", "12"));
		AssertAttributes(component.Find("rect.toggle"), ("rx", "12"), ("ry", "12"));
	}

	/// <summary>
	/// Verifies that explicit dimensions, border width and text class override the defaults.
	/// </summary>
	[Fact]
	public void Explicit_dimensions_border_and_text_class_are_applied()
	{
		var component = Render<PDToggleSwitch>(parameters => parameters
			.Add(p => p.Width, 100)
			.Add(p => p.Height, 40)
			.Add(p => p.BorderWidth, 4)
			.Add(p => p.TextCssClass, "bold"));

		component.Find("svg").GetAttribute("width").Should().Be("100");
		component.Find("svg").GetAttribute("height").Should().Be("40");
		AssertAttributes(component.Find("rect.switch"), ("height", "36"), ("width", "96"), ("x", "2"), ("y", "2"));
		component.Find("text").GetAttribute("class").Should().Be("text off bold");
	}

	/// <summary>
	/// Verifies that options supply every setting the parameters leave unset.
	/// </summary>
	[Fact]
	public void Options_supply_unset_parameters()
	{
		var options = new PDToggleSwitchOptions
		{
			CssClass = "from-options",
			Size = ButtonSizes.Large,
			Rounded = true,
			LabelBefore = true,
			OffText = "Off",
			TextCssClass = "opt-text",
			Width = 90,
			Height = 30,
			BorderWidth = 0
		};

		var component = Render<PDToggleSwitch>(parameters => parameters
			.Add(p => p.Options, options)
			.Add(p => p.Label, "Power"));

		component.Find(".pdtoggleswitch").ClassList.Should().Contain(["from-options", "lg", "rd"]);
		component.Find("svg").GetAttribute("width").Should().Be("90");
		component.Find("svg").GetAttribute("height").Should().Be("30");
		component.Find("text").TextContent.Should().Be("Off");
		component.Find("text").GetAttribute("class").Should().Be("text off opt-text");
		component.Find("span.label").ClassList.Should().Contain("me-1");
	}

	/// <summary>
	/// Verifies that the label is placed after the switch by default and before it when asked.
	/// </summary>
	[Fact]
	public void The_label_is_placed_after_or_before_the_switch()
	{
		var after = Render<PDToggleSwitch>(parameters => parameters.Add(p => p.Label, "Power"));
		after.Find("span.label").ClassList.Should().Contain("ms-1");
		after.Find(".pdtoggleswitch").LastElementChild!.TagName.Should().Be("SPAN");

		var before = Render<PDToggleSwitch>(parameters => parameters
			.Add(p => p.Label, "Power")
			.Add(p => p.LabelBefore, true));
		before.Find("span.label").ClassList.Should().Contain("me-1");
		before.Find(".pdtoggleswitch").FirstElementChild!.TagName.Should().Be("SPAN");
	}

	/// <summary>
	/// Verifies that a blank label renders no label element.
	/// </summary>
	[Fact]
	public void A_blank_label_is_not_rendered()
	{
		var component = Render<PDToggleSwitch>(parameters => parameters.Add(p => p.Label, " "));

		component.FindAll("span.label").Should().BeEmpty();
	}

	/// <summary>
	/// Verifies that clicking toggles the value, raises ValueChanged, and redraws the switch.
	/// </summary>
	[Fact]
	public void Clicking_toggles_the_value()
	{
		var values = new List<bool>();
		var component = Render<PDToggleSwitch>(parameters => parameters
			.Add(p => p.ValueChanged, v => values.Add(v)));

		component.Find("svg").Click();
		component.Find("rect.toggle").GetAttribute("class").Should().Be("toggle on");

		component.Find("svg").Click();

		values.Should().Equal(true, false);
		component.Find("rect.toggle").GetAttribute("class").Should().Be("toggle off");
	}

	/// <summary>
	/// Verifies that Space and Enter toggle the value while other keys do not.
	/// </summary>
	[Fact]
	public void Space_and_enter_toggle_the_value_and_other_keys_do_not()
	{
		var values = new List<bool>();
		var component = Render<PDToggleSwitch>(parameters => parameters
			.Add(p => p.ValueChanged, v => values.Add(v)));

		component.Find("svg").KeyPress(new KeyboardEventArgs { Code = "Space" });
		component.Find("svg").KeyPress(new KeyboardEventArgs { Code = "KeyA" });
		component.Find("svg").KeyPress(new KeyboardEventArgs { Code = "Enter" });

		values.Should().Equal(true, false);
	}

	/// <summary>
	/// Verifies that a disabled switch is not focusable and ignores clicks and keys.
	/// </summary>
	[Fact]
	public void A_disabled_switch_ignores_input()
	{
		var values = new List<bool>();
		var component = Render<PDToggleSwitch>(parameters => parameters
			.Add(p => p.IsEnabled, false)
			.Add(p => p.ValueChanged, v => values.Add(v)));

		component.Find(".pdtoggleswitch").ClassList.Should().Contain("disabled");
		component.Find("svg").HasAttribute("tabindex").Should().BeFalse();

		component.Find("svg").Click();
		component.Find("svg").KeyPress(new KeyboardEventArgs { Code = "Space" });

		values.Should().BeEmpty();
	}

	/// <summary>
	/// Verifies that the switch widens to fit the longer of its measured on and off texts.
	/// </summary>
	[Fact]
	public void The_switch_widens_to_fit_the_measured_text()
	{
		_module.Setup<double>("measureText", "Enabled", "1rem").SetResult(40);
		_module.Setup<double>("measureText", "Off", "1rem").SetResult(20);

		var component = Render<PDToggleSwitch>(parameters => parameters
			.Add(p => p.OnText, "Enabled")
			.Add(p => p.OffText, "Off"));

		// 48 for a medium switch, plus the text width beyond the first 16px.
		component.WaitForAssertion(() => component.Find("svg").GetAttribute("width").Should().Be("72"));
	}

	/// <summary>
	/// Verifies that text is measured at the font size of the switch size, and that empty text is not
	/// measured at all.
	/// </summary>
	[Theory]
	[InlineData(ButtonSizes.Small, "0.5rem", 14, "38")]
	[InlineData(ButtonSizes.Large, "1.5rem", 30, "70")]
	public void Text_is_measured_at_the_size_font(ButtonSizes size, string fontSize, double measured, string width)
	{
		_module.Setup<double>("measureText", "On", fontSize).SetResult(measured);

		var component = Render<PDToggleSwitch>(parameters => parameters
			.Add(p => p.Size, size)
			.Add(p => p.OnText, "On"));

		component.WaitForAssertion(() => component.Find("svg").GetAttribute("width").Should().Be(width));
		_module.Invocations["measureText"].Should().ContainSingle()
			.Which.Arguments.Should().Equal("On", fontSize);
	}

	/// <summary>
	/// Verifies that new, longer text supplied after first render widens the switch again.
	/// </summary>
	[Fact]
	public void Longer_text_supplied_later_widens_the_switch()
	{
		_module.Setup<double>("measureText", "On", "1rem").SetResult(20);
		_module.Setup<double>("measureText", "Switched on", "1rem").SetResult(60);
		var component = Render<PDToggleSwitch>(parameters => parameters.Add(p => p.OnText, "On"));
		component.WaitForAssertion(() => component.Find("svg").GetAttribute("width").Should().Be("52"));

		component.Render(parameters => parameters.Add(p => p.OnText, "Switched on"));

		component.WaitForAssertion(() => component.Find("svg").GetAttribute("width").Should().Be("92"));
	}

	private static void AssertAttributes(AngleSharp.Dom.IElement element, params (string Name, string Value)[] expected)
	{
		foreach (var (name, value) in expected)
		{
			element.GetAttribute(name).Should().Be(value, "attribute {0} of <{1}>", name, element.TagName);
		}
	}
}
