using AwesomeAssertions;
using Bunit;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;

namespace PanoramicData.Blazor.Test;

/// <summary>
/// Focus, blur and parameter binding tests for <see cref="PDTagInput"/>.
/// </summary>
public partial class PDTagInputTests
{
	/// <summary>Verifies that clicking the control focuses the input only when it can be edited.</summary>
	[Theory]
	[InlineData(true, true)]
	[InlineData(false, false)]
	public async Task Clicking_the_control_focuses_an_editable_input(bool isEnabled, bool expectFocus)
	{
		var component = RenderInput(p => p.Add(x => x.IsEnabled, isEnabled));

		await component.Find(".pd-taginput-control").ClickAsync(new MouseEventArgs());

		JSInterop.Invocations.Any(i => i.Identifier.EndsWith("focus", StringComparison.Ordinal)).Should().Be(expectFocus);
	}

	/// <summary>Verifies that leaving the input commits the pending text once the blur delay has passed.</summary>
	[Fact]
	public async Task Leaving_the_input_commits_the_pending_text()
	{
		var component = RenderInput();

		await TypeAsync(component, "pending");
		await component.Find(".pd-taginput-input").BlurAsync(new FocusEventArgs());

		component.WaitForAssertion(() => Tags(component).Should().Equal("pending"), Patience);
	}

	/// <summary>Verifies that leaving the input hides the suggestion list without adding, when AddOnBlur is off.</summary>
	[Fact]
	public async Task Leaving_without_AddOnBlur_only_hides_the_suggestions()
	{
		var component = RenderInput(p => p
			.Add(x => x.AddOnBlur, false)
			.Add(x => x.Suggestions, ["pending tag"]));

		await TypeAsync(component, "pending");
		component.Find(".pd-taginput-dropdown").Should().NotBeNull();
		await component.Find(".pd-taginput-input").BlurAsync(new FocusEventArgs());

		component.WaitForAssertion(() => component.FindAll(".pd-taginput-dropdown").Should().BeEmpty(), Patience);
		Tags(component).Should().BeEmpty();
	}

	/// <summary>Verifies that refocusing before the blur delay passes keeps the text uncommitted.</summary>
	[Fact]
	public async Task Refocusing_before_the_blur_delay_keeps_the_text()
	{
		var component = RenderInput(p => p.Add(x => x.Suggestions, ["pending tag"]));

		await TypeAsync(component, "pending");
		var blur = component.Find(".pd-taginput-input").BlurAsync(new FocusEventArgs());
		await component.Find(".pd-taginput-input").FocusAsync(new FocusEventArgs());

		await blur;
		Tags(component).Should().BeEmpty();
		component.Find(".pd-taginput-dropdown").Should().NotBeNull();
	}

	/// <summary>Verifies that a new list supplied by the parent replaces the tags shown.</summary>
	[Fact]
	public void A_new_list_from_the_parent_replaces_the_tags()
	{
		var component = RenderInput(p => p.Add(x => x.Values, ["alpha"]));

		component.Render(p => p.Add(x => x.Values, ["beta", "gamma"]));

		Tags(component).Should().Equal("beta", "gamma");
	}

	/// <summary>Verifies that a null list from the parent, before anything was emitted, clears the tags (#192).</summary>
	[Fact]
	public void A_null_list_from_the_parent_clears_the_tags()
	{
		var component = RenderInput(p => p.Add(x => x.Values, ["alpha"]));

		component.Render(p => p.Add(x => x.Values, null!));

		Tags(component).Should().BeEmpty();
	}

	/// <summary>Verifies that a null list from the parent clears the tags after the user has added one too.</summary>
	[Fact]
	public async Task A_null_list_from_the_parent_clears_the_tags_after_an_edit()
	{
		var component = RenderInput(p => p.Add(x => x.Values, ["alpha"]));
		await TypeAsync(component, "beta");
		await PressAsync(component, "Enter");

		component.Render(p => p.Add(x => x.Values, null!));

		Tags(component).Should().BeEmpty();
	}

	/// <summary>
	/// Verifies that the parent echoing back the list just emitted does not reset the input, and that the same
	/// list instance supplied again is not re-read either.
	/// </summary>
	[Fact]
	public async Task An_echoed_or_repeated_list_is_not_re_read()
	{
		var supplied = new List<string> { "alpha" };
		var component = RenderInput(p => p.Add(x => x.Values, supplied));
		await TypeAsync(component, "beta");
		await PressAsync(component, "Enter");

		component.Render(p => p.Add(x => x.Values, _emitted[^1]));
		component.Render(p => p.Add(x => x.Values, _emitted[^1]));

		Tags(component).Should().Equal("alpha", "beta");
		supplied.Should().Equal("alpha");
	}
}
