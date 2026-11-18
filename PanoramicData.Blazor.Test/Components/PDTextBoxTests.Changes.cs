using AwesomeAssertions;
using Bunit;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;

namespace PanoramicData.Blazor.Test.Components;

/// <summary>
/// Value change, debounce and key press tests for <see cref="PDTextBox"/>.
/// </summary>
public partial class PDTextBoxTests
{
	/// <summary>Without a debounce, a change event updates the value and raises ValueChanged.</summary>
	[Fact]
	public void Change_WithoutDebounce_RaisesValueChanged()
	{
		var component = Render<PDTextBox>(parameters => parameters
			.Add(p => p.ValueChanged, (string v) => _changes.Add(v)));

		component.Find("input").Change("typed");
		component.Find("input").Change((object?)null);

		_changes.Should().Equal("typed", string.Empty);
	}

	/// <summary>With a debounce, a change event (leaving the box or pressing Enter) reports the value without waiting for the debounce.</summary>
	[Fact]
	public async Task Change_WithDebounce_ReportsTheValueAtOnce()
	{
		var component = RenderDebounced();

		await component.Find("input").ChangeAsync(new ChangeEventArgs { Value = "typed" });

		_changes.Should().Equal("typed");
		component.Instance.Value.Should().Be("typed");
	}

	/// <summary>The debounced report of typing a change event has already reported is skipped, but later typing is reported.</summary>
	[Fact]
	public async Task DebouncedInput_OfTypingAChangeReported_IsSkipped()
	{
		var component = RenderDebounced();

		await component.Find("input").ChangeAsync(new ChangeEventArgs { Value = "typed" });
		await component.InvokeAsync(() => component.Instance.OnDebouncedInput("typed"));
		await component.InvokeAsync(() => component.Instance.OnDebouncedInput("typed more"));

		_changes.Should().Equal("typed", "typed more");
	}

	/// <summary>A change to the value the debounce has already reported is not reported again.</summary>
	[Fact]
	public async Task Change_ToTheValueTheDebounceReported_IsNotReportedAgain()
	{
		var component = RenderDebounced();

		await component.InvokeAsync(() => component.Instance.OnDebouncedInput("typed"));
		await component.Find("input").ChangeAsync(new ChangeEventArgs { Value = "typed" });

		_changes.Should().Equal("typed");
	}

	/// <summary>Once the parent has replaced the value, a change back to what the debounce reported is reported again.</summary>
	[Fact]
	public async Task Change_AfterTheParentReplacedTheValue_IsReported()
	{
		var component = RenderDebounced();

		await component.InvokeAsync(() => component.Instance.OnDebouncedInput("typed"));
		component.Render(parameters => parameters.Add(p => p.Value, "replaced"));
		await component.Find("input").ChangeAsync(new ChangeEventArgs { Value = "typed" });

		_changes.Should().Equal("typed", "typed");
	}

	private IRenderedComponent<PDTextBox> RenderDebounced() => Render<PDTextBox>(parameters => parameters
		.Add(p => p.DebounceWait, 300)
		.Add(p => p.ValueChanged, (string v) => _changes.Add(v)));

	/// <summary>A debounce registers the input with the common JS module, which then reports the value.</summary>
	[Fact]
	public async Task Debounce_RegistersWithJs_AndReceivesTheDebouncedValue()
	{
		var common = JSInterop.SetupModule(JSInteropVersionHelper.CommonJsUrl);
		common.Mode = JSRuntimeMode.Loose;
		var component = Render<PDTextBox>(parameters => parameters
			.Add(p => p.DebounceWait, 300)
			.Add(p => p.ValueChanged, (string v) => _changes.Add(v)));

		var call = common.VerifyInvoke("debounceInput");
		call.Arguments[0].Should().Be(component.Instance.Id);
		call.Arguments[1].Should().Be(300);

		await component.InvokeAsync(() => component.Instance.OnDebouncedInput("debounced"));

		_changes.Should().Equal("debounced");
		component.Instance.Value.Should().Be("debounced");
	}

	/// <summary>Without a debounce nothing is registered with JS.</summary>
	[Fact]
	public void NoDebounce_RegistersNothingWithJs()
	{
		var common = JSInterop.SetupModule(JSInteropVersionHelper.CommonJsUrl);
		common.Mode = JSRuntimeMode.Loose;

		Render<PDTextBox>();

		common.Invocations["debounceInput"].Should().BeEmpty();
	}

	/// <summary>With keypress events, typing updates the value and each key up raises ValueChanged then Keypress.</summary>
	[Fact]
	public void KeypressEvent_RaisesValueChangedAndKeypressOnKeyUp()
	{
		var keys = new List<string>();
		var component = Render<PDTextBox>(parameters => parameters
			.Add(p => p.KeypressEvent, true)
			.Add(p => p.ValueChanged, (string v) => _changes.Add(v))
			.Add(p => p.Keypress, (KeyboardEventArgs e) => keys.Add(e.Key)));

		component.Find("input").Input("ab");
		component.Find("input").KeyUp(new KeyboardEventArgs { Key = "b" });

		component.Instance.Value.Should().Be("ab");
		_changes.Should().Equal("ab");
		keys.Should().Equal("b");
	}
}
