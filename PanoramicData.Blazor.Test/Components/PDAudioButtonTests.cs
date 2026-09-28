using AwesomeAssertions;
using Bunit;
using PanoramicData.Blazor.Enums;

namespace PanoramicData.Blazor.Test;

/// <summary>
/// Tests that <see cref="PDAudioButton"/> toggles between on and off and shows the state it is in.
/// </summary>
public class PDAudioButtonTests : BunitContext
{
	/// <summary>Sets up the rendering context.</summary>
	public PDAudioButtonTests() => JSInterop.Mode = JSRuntimeMode.Loose;

	/// <summary>
	/// Verifies that a value above one half renders as pressed in the active colour, and one at or below
	/// renders as unpressed in the inactive colour.
	/// </summary>
	[Theory]
	[InlineData(1.0, "pressed", "#0f0")]
	[InlineData(0.5, "unpressed", "#111")]
	[InlineData(0.0, "unpressed", "#111")]
	public void Value_SelectsThePressedStateAndColour(double value, string expectedClass, string expectedColour)
	{
		var component = Render<PDAudioButton>(parameters => parameters
			.Add(p => p.Value, value)
			.Add(p => p.ActiveColor, "#0f0")
			.Add(p => p.InactiveColor, "#111"));

		component.Find(".button-container").ClassList.Should().Contain(expectedClass);
		component.Find(".button-inner").GetAttribute("style").Should().Contain($"background-color: {expectedColour}");
	}

	/// <summary>
	/// Verifies that clicking an off button turns it on, and clicking again turns it off, raising
	/// <see cref="PDAudioControl.ValueChanged"/> with the new value each time.
	/// </summary>
	[Fact]
	public void Click_TogglesTheValue_AndRaisesValueChanged()
	{
		var raised = new List<double>();
		var component = Render<PDAudioButton>(parameters => parameters
			.Add(p => p.Value, 0.0)
			.Add(p => p.ValueChanged, v => raised.Add(v)));

		component.Find(".button-container").Click();
		component.Find(".button-container").ClassList.Should().Contain("pressed");

		component.Find(".button-container").Click();
		component.Find(".button-container").ClassList.Should().Contain("unpressed");

		raised.Should().Equal(1.0, 0.0);
	}

	/// <summary>
	/// Verifies that a disabled button ignores clicks, raises nothing, and is styled as disabled.
	/// </summary>
	[Fact]
	public void Click_WhenDisabled_DoesNothing()
	{
		var raised = new List<double>();
		var component = Render<PDAudioButton>(parameters => parameters
			.Add(p => p.Value, 0.0)
			.Add(p => p.IsEnabled, false)
			.Add(p => p.ValueChanged, v => raised.Add(v)));

		component.Find(".button-container").Click();

		component.Find(".pd-audio-button").ClassList.Should().Contain("disabled");
		component.Find(".button-container").ClassList.Should().Contain("unpressed");
		raised.Should().BeEmpty();
	}

	/// <summary>
	/// Verifies that the label is rendered before the button when positioned above, and after it when below.
	/// </summary>
	[Theory]
	[InlineData(PDLabelPosition.Above, true)]
	[InlineData(PDLabelPosition.Below, false)]
	public void Label_IsRenderedOnTheRequestedSide(PDLabelPosition position, bool labelFirst)
	{
		var component = Render<PDAudioButton>(parameters => parameters
			.Add(p => p.Label, "Mute")
			.Add(p => p.CssClass, "custom")
			.Add(p => p.LabelPosition, position));

		var root = component.Find(".pd-audio-button");
		root.ClassList.Should().Contain("custom");
		var children = root.Children.Select(c => c.ClassName ?? string.Empty).ToList();
		children.Should().HaveCount(2);
		var labelIndex = children.FindIndex(c => c.Contains("pd-audio-label", StringComparison.Ordinal));
		labelIndex.Should().Be(labelFirst ? 0 : 1);
		component.Find(".pd-audio-label").TextContent.Should().Be("Mute");
	}
}
