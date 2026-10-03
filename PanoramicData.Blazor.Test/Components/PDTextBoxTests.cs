using AwesomeAssertions;
using Bunit;
using PanoramicData.Blazor.Models;

namespace PanoramicData.Blazor.Test.Components;

/// <summary>
/// Tests that <see cref="PDTextBox"/> renders its input, raises value changes on the right event, clears
/// itself and drives the speech recognition and debounce JS modules.
/// </summary>
public partial class PDTextBoxTests : BunitContext
{
	private const string SpeechModulePath = "./_content/PanoramicData.Blazor/PDTextBox.razor.js";
	private readonly List<string> _changes = [];

	/// <summary>Sets up the rendering context.</summary>
	public PDTextBoxTests() => JSInterop.Mode = JSRuntimeMode.Loose;

	/// <summary>The input carries the value, placeholder, autocomplete and state attributes it is given.</summary>
	[Fact]
	public void Parameters_AreRenderedOntoTheInput()
	{
		var component = Render<PDTextBox>(parameters => parameters
			.Add(p => p.Value, "hello")
			.Add(p => p.Placeholder, "Type here")
			.Add(p => p.AutoComplete, "off")
			.Add(p => p.CssClass, "extra")
			.Add(p => p.IsReadOnly, true)
			.Add(p => p.IsEnabled, false));

		var input = component.Find("input");
		input.GetAttribute("value").Should().Be("hello");
		input.GetAttribute("placeholder").Should().Be("Type here");
		input.GetAttribute("autocomplete").Should().Be("off");
		input.GetAttribute("type").Should().Be("text");
		input.HasAttribute("readonly").Should().BeTrue();
		input.HasAttribute("disabled").Should().BeTrue();
		input.ClassList.Should().Contain(["form-control", "extra"]);
		input.Id.Should().StartWith("pd-textbox-");
	}

	/// <summary>The input type is rendered in lower case, with DateTimeLocal hyphenated.</summary>
	[Theory]
	[InlineData(PDInputType.Password, "password")]
	[InlineData(PDInputType.Email, "email")]
	[InlineData(PDInputType.DateTimeLocal, "datetime-local")]
	public void Type_IsRenderedAsTheHtmlInputType(PDInputType type, string expected)
	{
		var component = Render<PDTextBox>(parameters => parameters.Add(p => p.Type, type));

		component.Find("input").GetAttribute("type").Should().Be(expected);
	}

	/// <summary>The size maps onto both the input and button size classes.</summary>
	[Theory]
	[InlineData(ButtonSizes.Small, "form-control-sm", "btn-sm")]
	[InlineData(ButtonSizes.Large, "form-control-lg", "btn-lg")]
	public void Size_SetsInputAndButtonSizeClasses(ButtonSizes size, string inputClass, string buttonClass)
	{
		var component = Render<PDTextBox>(parameters => parameters
			.Add(p => p.Size, size)
			.Add(p => p.ShowSpeechButton, true));

		component.Find("input").ClassList.Should().Contain(inputClass);
		component.FindAll("button").Should().AllSatisfy(b => b.ClassList.Should().Contain(buttonClass));
	}

	/// <summary>A medium size adds no size classes.</summary>
	[Fact]
	public void MediumSize_AddsNoSizeClasses()
	{
		var component = Render<PDTextBox>(parameters => parameters.Add(p => p.Size, ButtonSizes.Medium));

		component.Find("input").ClassList.Should().NotContain(["form-control-sm", "form-control-lg"]);
		component.Find("button").ClassList.Should().NotContain(["btn-sm", "btn-lg"]);
	}

	/// <summary>A hidden text box is given d-none and shows no buttons.</summary>
	[Fact]
	public void Hidden_HasDNone_AndNoButtons()
	{
		var component = Render<PDTextBox>(parameters => parameters
			.Add(p => p.IsVisible, false)
			.Add(p => p.ShowSpeechButton, true));

		component.Find("input").ClassList.Should().Contain("d-none");
		component.FindAll("button").Should().BeEmpty();
	}

	/// <summary>The clear button is disabled while the value is empty.</summary>
	[Fact]
	public void ClearButton_IsDisabledWhenEmpty()
	{
		var empty = Render<PDTextBox>();
		var filled = Render<PDTextBox>(parameters => parameters.Add(p => p.Value, "x"));
		var disabled = Render<PDTextBox>(parameters => parameters.Add(p => p.Value, "x").Add(p => p.IsEnabled, false));

		empty.Find("button").HasAttribute("disabled").Should().BeTrue();
		filled.Find("button").HasAttribute("disabled").Should().BeFalse();
		disabled.Find("button").HasAttribute("disabled").Should().BeTrue();
	}

	/// <summary>Clicking clear empties the JS input and the value, then raises ValueChanged and Cleared.</summary>
	[Fact]
	public void ClearButton_ClearsTheValue_AndRaisesCleared()
	{
		var common = JSInterop.SetupModule(JSInteropVersionHelper.CommonJsUrl);
		common.Mode = JSRuntimeMode.Loose;
		var cleared = 0;
		var component = Render<PDTextBox>(parameters => parameters
			.Add(p => p.Value, "something")
			.Add(p => p.ValueChanged, (string v) => _changes.Add(v))
			.Add(p => p.Cleared, () => cleared++));

		component.Find("button").Click();

		common.VerifyInvoke("setValue").Arguments.Should().Equal(component.Instance.Id, string.Empty);
		component.Instance.Value.Should().BeEmpty();
		_changes.Should().Equal(string.Empty);
		cleared.Should().Be(1);
	}

	/// <summary>Clearing still works when the common JS module could not be loaded.</summary>
	[Fact]
	public void ClearButton_WithoutTheModule_StillClears()
	{
		JSInterop.Mode = JSRuntimeMode.Strict;
		var component = Render<PDTextBox>(parameters => parameters
			.Add(p => p.Value, "something")
			.Add(p => p.ValueChanged, (string v) => _changes.Add(v)));

		component.Find("button").Click();

		_changes.Should().Equal(string.Empty);
	}

	/// <summary>With ShowClearButton false there is no clear button.</summary>
	[Fact]
	public void ShowClearButtonFalse_HidesTheClearButton()
	{
		var component = Render<PDTextBox>(parameters => parameters.Add(p => p.ShowClearButton, false));

		component.FindAll("button").Should().BeEmpty();
	}

	/// <summary>Leaving the input raises Blur.</summary>
	[Theory]
	[InlineData(false)]
	[InlineData(true)]
	public void Blur_RaisesBlur(bool keypressEvent)
	{
		var blurs = 0;
		var component = Render<PDTextBox>(parameters => parameters
			.Add(p => p.KeypressEvent, keypressEvent)
			.Add(p => p.Blur, () => blurs++));

		component.Find("input").Blur();

		blurs.Should().Be(1);
	}
}
