using AwesomeAssertions;
using Bunit;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;
using PanoramicData.Blazor.Arguments;
using PanoramicData.Blazor.Models;

namespace PanoramicData.Blazor.Test;

/// <summary>
/// Tests that <see cref="PDTagInput"/> adds, rejects and removes tags from keyboard, comma, blur and
/// suggestion input, reports each change, and keeps its suggestion list in step.
/// </summary>
public class PDTagInputTests : BunitContext
{
	private readonly List<List<string>> _emitted = [];
	private readonly List<string> _added = [];
	private readonly List<string> _removed = [];
	private readonly List<TagRejectedEventArgs> _rejected = [];

	/// <summary>Sets up the rendering context.</summary>
	public PDTagInputTests() => JSInterop.Mode = JSRuntimeMode.Loose;

	/// <summary>Verifies that the supplied values render as removable tags, with no placeholder.</summary>
	[Fact]
	public void Supplied_values_render_as_removable_tags()
	{
		var component = RenderInput(p => p
			.Add(x => x.Values, ["alpha", "beta"])
			.Add(x => x.Id, "tags")
			.Add(x => x.CssClass, "wide")
			.AddUnmatched("data-test", "yes"));

		Tags(component).Should().Equal("alpha", "beta");
		component.FindAll(".pd-taginput-remove").Select(b => b.GetAttribute("aria-label"))
			.Should().Equal("Remove alpha", "Remove beta");
		component.Find(".pd-taginput-input").GetAttribute("placeholder").Should().BeEmpty();
		var root = component.Find(".pd-taginput");
		root.Id.Should().Be("tags");
		root.ClassList.Should().Contain("wide");
		root.GetAttribute("data-test").Should().Be("yes");
	}

	/// <summary>Verifies that an empty input shows its placeholder.</summary>
	[Fact]
	public void An_empty_input_shows_the_placeholder()
	{
		var component = RenderInput(p => p.Add(x => x.Placeholder, "Type a tag"));

		component.Find(".pd-taginput-input").GetAttribute("placeholder").Should().Be("Type a tag");
	}

	/// <summary>Verifies that a disabled or read-only input shows tags but offers no way to change them.</summary>
	[Theory]
	[InlineData(false, false, "pd-taginput--disabled")]
	[InlineData(true, true, "pd-taginput--readonly")]
	public void Disabled_and_read_only_inputs_cannot_be_changed(bool isEnabled, bool isReadOnly, string stateClass)
	{
		var component = RenderInput(p => p
			.Add(x => x.Values, ["alpha"])
			.Add(x => x.IsEnabled, isEnabled)
			.Add(x => x.IsReadOnly, isReadOnly));

		Tags(component).Should().Equal("alpha");
		component.FindAll(".pd-taginput-remove").Should().BeEmpty();
		component.FindAll(".pd-taginput-input").Should().BeEmpty();
		component.Find(".pd-taginput-control").ClassList.Should().Contain(stateClass);
	}

	/// <summary>Verifies that a tag template renders each tag's content.</summary>
	[Fact]
	public void A_tag_template_renders_each_tag()
	{
		var component = RenderInput(p => p
			.Add(x => x.Values, ["alpha"])
			.Add(x => x.TagTemplate, tag => (RenderFragment)(b => b.AddMarkupContent(0, $"<b>#{tag}</b>"))));

		component.Find(".pd-taginput-tag b").TextContent.Should().Be("#alpha");
		component.FindAll(".pd-taginput-tag-label").Should().BeEmpty();
	}

	/// <summary>Verifies that Enter commits the trimmed typed text as a tag and reports it.</summary>
	[Fact]
	public void Enter_commits_the_typed_text()
	{
		var component = RenderInput();

		Type(component, "  gamma ");
		Press(component, "Enter");

		Tags(component).Should().Equal("gamma");
		_emitted.Should().ContainSingle().Which.Should().Equal("gamma");
		_added.Should().Equal("gamma");
		component.Find(".pd-taginput-input").GetAttribute("value").Should().BeEmpty();
	}

	/// <summary>Verifies that Enter with only whitespace typed adds nothing.</summary>
	[Fact]
	public void Enter_with_blank_text_adds_nothing()
	{
		var component = RenderInput();

		Type(component, "   ");
		Press(component, "Enter");

		Tags(component).Should().BeEmpty();
		_emitted.Should().BeEmpty();
	}

	/// <summary>Verifies that commas commit each complete part and leave the remainder being typed.</summary>
	[Fact]
	public void Commas_commit_each_complete_part()
	{
		var component = RenderInput();

		Type(component, "one, two,,thr");

		Tags(component).Should().Equal("one", "two");
		component.Find(".pd-taginput-input").GetAttribute("value").Should().Be("thr");
	}

	/// <summary>Verifies that a duplicate is rejected regardless of case, marking the input invalid until typing resumes.</summary>
	[Fact]
	public void A_duplicate_is_rejected_until_typing_resumes()
	{
		var component = RenderInput(p => p.Add(x => x.Values, ["Alpha"]));

		Type(component, "alpha");
		Press(component, "Enter");

		_rejected.Should().ContainSingle().Which.Reason.Should().Be(TagRejectionReason.Duplicate);
		_rejected[0].Tag.Should().Be("alpha");
		component.Find(".pd-taginput-control").ClassList.Should().Contain("pd-taginput--invalid");

		Type(component, "alph");
		component.Find(".pd-taginput-control").ClassList.Should().NotContain("pd-taginput--invalid");
	}

	/// <summary>Verifies that a case-sensitive input treats differently cased text as a different tag.</summary>
	[Fact]
	public void A_case_sensitive_input_allows_different_casing()
	{
		var component = RenderInput(p => p
			.Add(x => x.Values, ["Alpha"])
			.Add(x => x.CaseSensitive, true));

		Type(component, "alpha");
		Press(component, "Enter");

		Tags(component).Should().Equal("Alpha", "alpha");
		_rejected.Should().BeEmpty();
	}

	/// <summary>Verifies that the tag count and tag length limits reject what exceeds them.</summary>
	[Theory]
	[InlineData(1, 0, "second", TagRejectionReason.MaxTagsReached)]
	[InlineData(0, 3, "long", TagRejectionReason.TooLong)]
	public void Limits_reject_what_exceeds_them(int maxTags, int maxLength, string text, TagRejectionReason reason)
	{
		var component = RenderInput(p => p
			.Add(x => x.Values, ["one"])
			.Add(x => x.MaxTags, maxTags)
			.Add(x => x.MaxTagLength, maxLength));

		Type(component, text);
		Press(component, "Enter");

		_rejected.Should().ContainSingle().Which.Reason.Should().Be(reason);
		Tags(component).Should().Equal("one");
	}

	/// <summary>
	/// Verifies that without free text only suggestions are accepted, and a match takes the suggestion's casing.
	/// </summary>
	[Fact]
	public void Without_free_text_only_suggestions_are_accepted()
	{
		var component = RenderInput(p => p
			.Add(x => x.AllowFreeText, false)
			.Add(x => x.Suggestions, ["Red", "Green"]));

		Type(component, "blue");
		Press(component, "Escape");
		Press(component, "Enter");
		Type(component, "green");
		Press(component, "Escape");
		Press(component, "Enter");

		_rejected.Should().ContainSingle().Which.Reason.Should().Be(TagRejectionReason.NotInSuggestions);
		Tags(component).Should().Equal("Green");
	}

	/// <summary>Verifies that the remove button removes its tag and reports the removal.</summary>
	[Fact]
	public void The_remove_button_removes_its_tag()
	{
		var component = RenderInput(p => p.Add(x => x.Values, ["alpha", "beta"]));

		component.FindAll(".pd-taginput-remove")[0].Click();

		Tags(component).Should().Equal("beta");
		_removed.Should().Equal("alpha");
		_emitted.Should().ContainSingle().Which.Should().Equal("beta");
	}

	/// <summary>Verifies that Backspace in an empty input removes the last tag, but not while text is typed.</summary>
	[Fact]
	public void Backspace_removes_the_last_tag_only_when_nothing_is_typed()
	{
		var component = RenderInput(p => p.Add(x => x.Values, ["alpha", "beta"]));

		Type(component, "x");
		Press(component, "Backspace");
		Tags(component).Should().Equal("alpha", "beta");

		Type(component, string.Empty);
		Press(component, "Backspace");
		Tags(component).Should().Equal("alpha");
		_removed.Should().Equal("beta");
	}

	/// <summary>Verifies that focusing offers every unused suggestion, with the first active.</summary>
	[Fact]
	public void Focusing_offers_the_unused_suggestions()
	{
		var component = RenderInput(p => p
			.Add(x => x.Values, ["Red"])
			.Add(x => x.Suggestions, ["Red", "Green", "Blue"]));

		component.Find(".pd-taginput-input").Focus();

		Suggestions(component).Should().Equal("Green", "Blue");
		ActiveSuggestion(component).Should().Be("Green");
	}

	/// <summary>Verifies that typing filters the suggestions to those containing the text.</summary>
	[Fact]
	public void Typing_filters_the_suggestions()
	{
		var component = RenderInput(p => p.Add(x => x.Suggestions, ["Green", "Grey", "Blue"]));

		Type(component, "gr");

		Suggestions(component).Should().Equal("Green", "Grey");
	}

	/// <summary>Verifies that typing text matching no suggestion hides the list.</summary>
	[Fact]
	public void Typing_text_matching_nothing_hides_the_suggestions()
	{
		var component = RenderInput(p => p.Add(x => x.Suggestions, ["Green"]));

		Type(component, "zzz");

		component.FindAll(".pd-taginput-dropdown").Should().BeEmpty();
	}

	/// <summary>Verifies that the arrow keys move the active suggestion, wrapping at both ends, and Enter adds it.</summary>
	[Fact]
	public void Arrow_keys_move_the_active_suggestion_and_Enter_adds_it()
	{
		var component = RenderInput(p => p.Add(x => x.Suggestions, ["Red", "Green", "Blue"]));
		component.Find(".pd-taginput-input").Focus();

		Press(component, "ArrowUp");
		ActiveSuggestion(component).Should().Be("Blue");
		Press(component, "ArrowDown");
		ActiveSuggestion(component).Should().Be("Red");
		Press(component, "ArrowDown");
		Press(component, "Enter");

		Tags(component).Should().Equal("Green");
		Suggestions(component).Should().Equal("Red", "Blue");
	}

	/// <summary>Verifies that Escape hides the suggestions and ArrowDown brings them back.</summary>
	[Fact]
	public void Escape_hides_and_ArrowDown_reopens_the_suggestions()
	{
		var component = RenderInput(p => p.Add(x => x.Suggestions, ["Red"]));
		component.Find(".pd-taginput-input").Focus();

		Press(component, "Escape");
		component.FindAll(".pd-taginput-dropdown").Should().BeEmpty();

		Press(component, "ArrowDown");
		Suggestions(component).Should().Equal("Red");
	}

	/// <summary>Verifies that adding the last remaining suggestion closes the list.</summary>
	[Fact]
	public void Adding_the_last_suggestion_closes_the_list()
	{
		var component = RenderInput(p => p.Add(x => x.Suggestions, ["Red"]));
		component.Find(".pd-taginput-input").Focus();

		Press(component, "Enter");

		Tags(component).Should().Equal("Red");
		component.FindAll(".pd-taginput-dropdown").Should().BeEmpty();
	}

	/// <summary>Verifies that hovering a suggestion makes it active, and clicking it adds it and refocuses.</summary>
	[Fact]
	public void Hovering_and_clicking_a_suggestion_adds_it()
	{
		var component = RenderInput(p => p.Add(x => x.Suggestions, ["Red", "Green"]));
		component.Find(".pd-taginput-input").Focus();

		component.FindAll(".pd-taginput-dropdown li")[1].MouseOver();
		ActiveSuggestion(component).Should().Be("Green");
		component.FindAll(".pd-taginput-dropdown li")[1].Click();

		Tags(component).Should().Equal("Green");
		JSInterop.Invocations.Should().Contain(i => i.Identifier.EndsWith("focus", StringComparison.Ordinal));
	}

	/// <summary>Verifies that clicking the control focuses the input only when it can be edited.</summary>
	[Theory]
	[InlineData(true, true)]
	[InlineData(false, false)]
	public void Clicking_the_control_focuses_an_editable_input(bool isEnabled, bool expectFocus)
	{
		var component = RenderInput(p => p.Add(x => x.IsEnabled, isEnabled));

		component.Find(".pd-taginput-control").Click();

		JSInterop.Invocations.Any(i => i.Identifier.EndsWith("focus", StringComparison.Ordinal)).Should().Be(expectFocus);
	}

	/// <summary>Verifies that leaving the input commits the pending text once the blur delay has passed.</summary>
	[Fact]
	public void Leaving_the_input_commits_the_pending_text()
	{
		var component = RenderInput();

		Type(component, "pending");
		component.Find(".pd-taginput-input").Blur();

		component.WaitForAssertion(() => Tags(component).Should().Equal("pending"));
	}

	/// <summary>Verifies that leaving the input hides the suggestion list without adding, when AddOnBlur is off.</summary>
	[Fact]
	public void Leaving_without_AddOnBlur_only_hides_the_suggestions()
	{
		var component = RenderInput(p => p
			.Add(x => x.AddOnBlur, false)
			.Add(x => x.Suggestions, ["pending tag"]));

		Type(component, "pending");
		component.Find(".pd-taginput-dropdown").Should().NotBeNull();
		component.Find(".pd-taginput-input").Blur();

		component.WaitForAssertion(() => component.FindAll(".pd-taginput-dropdown").Should().BeEmpty());
		Tags(component).Should().BeEmpty();
	}

	/// <summary>Verifies that refocusing before the blur delay passes keeps the text uncommitted.</summary>
	[Fact]
	public async Task Refocusing_before_the_blur_delay_keeps_the_text()
	{
		var component = RenderInput(p => p.Add(x => x.Suggestions, ["pending tag"]));

		Type(component, "pending");
		var blur = component.Find(".pd-taginput-input").BlurAsync(new FocusEventArgs());
		component.Find(".pd-taginput-input").Focus();

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

	/// <summary>
	/// Verifies that the parent echoing back the list just emitted does not reset the input, and that the same
	/// list instance supplied again is not re-read either.
	/// </summary>
	[Fact]
	public void An_echoed_or_repeated_list_is_not_re_read()
	{
		var supplied = new List<string> { "alpha" };
		var component = RenderInput(p => p.Add(x => x.Values, supplied));
		Type(component, "beta");
		Press(component, "Enter");

		component.Render(p => p.Add(x => x.Values, _emitted[^1]));
		component.Render(p => p.Add(x => x.Values, _emitted[^1]));

		Tags(component).Should().Equal("alpha", "beta");
		supplied.Should().Equal("alpha");
	}

	private IRenderedComponent<PDTagInput> RenderInput(Action<ComponentParameterCollectionBuilder<PDTagInput>>? configure = null)
		=> Render<PDTagInput>(parameters =>
		{
			parameters
				.Add(p => p.ValuesChanged, (List<string> values) => _emitted.Add(values))
				.Add(p => p.TagAdded, (string tag) => _added.Add(tag))
				.Add(p => p.TagRemoved, (string tag) => _removed.Add(tag))
				.Add(p => p.TagRejected, (TagRejectedEventArgs args) => _rejected.Add(args));
			configure?.Invoke(parameters);
		});

	private static void Type(IRenderedComponent<PDTagInput> component, string text)
		=> component.Find(".pd-taginput-input").Input(new ChangeEventArgs { Value = text });

	private static void Press(IRenderedComponent<PDTagInput> component, string key)
		=> component.Find(".pd-taginput-input").KeyDown(new KeyboardEventArgs { Key = key });

	private static List<string> Tags(IRenderedComponent<PDTagInput> component)
		=> [.. component.FindAll(".pd-taginput-tag").Select(t => t.GetAttribute("title")!)];

	private static List<string> Suggestions(IRenderedComponent<PDTagInput> component)
		=> [.. component.FindAll(".pd-taginput-dropdown li").Select(li => li.TextContent.Trim())];

	private static string ActiveSuggestion(IRenderedComponent<PDTagInput> component)
		=> component.Find(".pd-taginput-dropdown li.active").TextContent.Trim();
}
