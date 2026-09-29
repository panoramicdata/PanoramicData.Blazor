using Bunit;
using Shouldly;
using Xunit;

namespace PanoramicData.Blazor.Test;

/// <summary>
/// Tests for <see cref="PDTextArea"/> with a debounce.
/// </summary>
/// <remarks>
/// MS-26860 (Magic Suite): with DebounceWait set, the text area only tells its parent about typing
/// once the user pauses, so for that interval the parent still holds the old value. Any parent render
/// in that interval passed the old value back in and the text area replaced what the user had typed
/// with it. In NCalc 101's variable dialog, typing "typed123" left "ed123": the first keystrokes were
/// wiped while the user watched.
/// </remarks>
public class PDTextAreaDebounceTests : BunitContext
{
	/// <summary>
	/// Sets up the rendering context.
	/// </summary>
	public PDTextAreaDebounceTests()
		// The text area imports JavaScript modules for the debounce and selection tracking. Loose mode
		// stubs them; the debounced report is driven directly through OnDebouncedInput instead.
		=> JSInterop.Mode = JSRuntimeMode.Loose;

	private IRenderedComponent<PDTextArea> Render(string value, int debounceWait, List<string>? reported = null)
		=> base.Render<PDTextArea>(parameters => parameters
			.Add(p => p.Value, value)
			.Add(p => p.DebounceWait, debounceWait)
			.Add(p => p.ValueChanged, v => reported?.Add(v)));

	private static string DisplayedValue(IRenderedComponent<PDTextArea> component)
		=> component.Find("textarea").GetAttribute("value") ?? string.Empty;

	/// <summary>
	/// The parent renders again before the debounce has reported, passing the value it already had.
	/// </summary>
	[Fact]
	public void ParentRendersWithItsUnchangedValue_WhileTyping_TheTypingIsKept()
	{
		var component = Render(string.Empty, debounceWait: 500);

		component.Find("textarea").Input("ty");
		component.Render(parameters => parameters.Add(p => p.Value, string.Empty));

		DisplayedValue(component).ShouldBe("ty");
	}

	/// <summary>
	/// The debounce reported "typ", the user kept typing, and then the parent echoed "typ" back.
	/// </summary>
	[Fact]
	public async Task ParentEchoesAnEarlierReportedValue_WhileTyping_TheNewerTypingIsKept()
	{
		var component = Render(string.Empty, debounceWait: 500);

		component.Find("textarea").Input("typ");
		await component.InvokeAsync(() => component.Instance.OnDebouncedInput("typ"));
		component.Find("textarea").Input("typed");
		component.Render(parameters => parameters.Add(p => p.Value, "typ"));

		DisplayedValue(component).ShouldBe("typed");
	}

	/// <summary>
	/// The guard only holds back values the text area has already seen: a parent that genuinely changes
	/// the value (loading another record, clearing a form) still wins.
	/// </summary>
	[Fact]
	public void ParentChangesTheValue_WhileTyping_TheParentsValueIsShown()
	{
		var component = Render("first record", debounceWait: 500);

		component.Find("textarea").Input("first record edited");
		component.Render(parameters => parameters.Add(p => p.Value, "second record"));

		DisplayedValue(component).ShouldBe("second record");
	}

	/// <summary>
	/// A report the parent has already taken up is not an echo any more: a later record whose value
	/// happens to be the same text must still be shown.
	/// </summary>
	[Fact]
	public async Task ParentLaterLoadsAValueEqualToAnOldReport_TheParentsValueIsShown()
	{
		var component = Render(string.Empty, debounceWait: 500);

		component.Find("textarea").Input("abc");
		await component.InvokeAsync(() => component.Instance.OnDebouncedInput("abc"));
		component.Render(parameters => parameters.Add(p => p.Value, "other record"));
		component.Render(parameters => parameters.Add(p => p.Value, "abc"));

		DisplayedValue(component).ShouldBe("abc");
	}

	/// <summary>
	/// With nothing typed, a new value from the parent is simply shown.
	/// </summary>
	[Fact]
	public void ParentChangesTheValue_WhenNotTyping_TheParentsValueIsShown()
	{
		var component = Render("first", debounceWait: 500);

		component.Render(parameters => parameters.Add(p => p.Value, "second"));

		DisplayedValue(component).ShouldBe("second");
	}

	/// <summary>
	/// The debounced value still reaches the parent.
	/// </summary>
	[Fact]
	public async Task DebounceReports_TheParentReceivesTheTypedValue()
	{
		var reported = new List<string>();
		var component = Render(string.Empty, debounceWait: 500, reported);

		component.Find("textarea").Input("typed");
		await component.InvokeAsync(() => component.Instance.OnDebouncedInput("typed"));

		reported.ShouldBe(["typed"]);
	}

	/// <summary>
	/// Without a debounce the parent is told on every keystroke, so it always holds the typed value and
	/// its value is applied as before.
	/// </summary>
	[Fact]
	public void NoDebounce_ParentsValueIsAppliedAsBefore()
	{
		var reported = new List<string>();
		var component = Render(string.Empty, debounceWait: 0, reported);

		component.Find("textarea").Input("ty");
		component.Render(parameters => parameters.Add(p => p.Value, string.Empty));

		reported.ShouldBe(["ty"]);
		DisplayedValue(component).ShouldBe(string.Empty);
	}
}
